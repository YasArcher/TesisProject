using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    /// <summary>
    /// Genera un Excel con la misma lógica de columnas de la matriz
    /// (base + dinámicas de categorías + dinámicas de objetivos),
    /// pero permitiendo seleccionar columnas y encabezados mediante ExportRequestDTO.
    /// Usa EPPlus igual que MatrixExcelExportService.
    /// </summary>
    public class MatrixTemplateExcelExportService : IMatrixTemplateExcelExportService
    {
        private readonly IProjectFlatReportService _flatService;
        private readonly IResearchCategoryService _categoryService;
        private readonly ILogger<MatrixTemplateExcelExportService> _logger;

        public MatrixTemplateExcelExportService(
            IProjectFlatReportService flatService,
            IResearchCategoryService categoryService,
            ILogger<MatrixTemplateExcelExportService> logger)
        {
            _flatService = flatService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportRequestDTO request,
            CancellationToken ct = default)
        {
            if (request == null)
                return ServiceResult<byte[]>.Fail(
                    "La solicitud de exportación es nula.",
                    ErrorType.Validation);

            if (request.Columns is null || request.Columns.Count == 0)
                return ServiceResult<byte[]>.Fail(
                    "No se han definido columnas para la exportación.",
                    ErrorType.Validation);

            // 1) Cargar dataset plano (igual que MatrixExcelExportService)
            var flatResult = await _flatService.GetFlatReportAsync(request.ProjectIds, ct);
            if (!flatResult.Success || flatResult.Data is null || flatResult.Data.Count == 0)
                return ServiceResult<byte[]>.Fail(flatResult.Message ?? "No hay datos para exportar.");

            var projects = flatResult.Data.ToList();

            // 2) Cargar árbol de categorías
            var catResult = await _categoryService.GetTreeAsync(onlyActives: true, ct);
            var categoryTree = catResult.Success && catResult.Data is not null
                ? catResult.Data.ToList()
                : new List<ResearchCategoryTreeItemDTO>();

            // 3) Lookups de categorías
            var categoryById = BuildCategoryLookups(categoryTree);

            try
            {
                var bytes = GenerateExcelInternal(request, projects, categoryById);
                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al generar el Excel de matriz basado en plantilla dinámica.");

                return ServiceResult<byte[]>.Fail(
                    "Error al generar el archivo Excel.",
                    ErrorType.Unexpected);
            }
        }

        // =========================================================
        //              LÓGICA INTERNA DE EXCEL (EPPlus)
        // =========================================================

        private sealed class ColumnDef
        {
            /// <summary>Clave lógica del campo para cruzar con ExportColumnDTO.FieldKey.</summary>
            public string FieldKey { get; init; } = string.Empty;

            /// <summary>Encabezado por defecto (igual que en MatrixExcelExportService).</summary>
            public string Header { get; init; } = string.Empty;

            /// <summary>Orden base (como Order en MatrixExcelExportService).</summary>
            public int Order { get; init; }

            /// <summary>Selector de valor desde el DTO plano.</summary>
            public Func<ProjectFlatReportDTO, object?> Selector { get; init; } = _ => null;
        }

        private byte[] GenerateExcelInternal(
            ExportRequestDTO request,
            List<ProjectFlatReportDTO> projects,
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            // 1) Construir TODAS las columnas (base + categorías + objetivos)
            var allColumns = new List<ColumnDef>();
            allColumns.AddRange(GetBaseColumns());
            allColumns.AddRange(BuildCategoryColumnsMetadata(categoryById));
            allColumns.AddRange(BuildObjectiveColumnsMetadata(projects));

            // 2) Resolver columnas a partir del DTO (filtrado + casos especiales)
            var orderedRequestedColumns = request.Columns
                .OrderBy(c => c.OrderIndex)
                .ToList();

            var selectedColumns = new List<ColumnDef>();

            foreach (var dtoCol in orderedRequestedColumns)
            {
                if (string.IsNullOrWhiteSpace(dtoCol.FieldKey))
                    continue;

                var key = dtoCol.FieldKey.ToUpperInvariant();

                // ==============================
                // CASO ESPECIAL: TODAS CATEGORÍAS DINÁMICAS
                // ==============================
                if (key == "MATRIX_DYNAMIC_CATEGORIES")
                {
                    var dynamicCategoryColumns = allColumns
                        .Where(c => c.FieldKey.StartsWith("CATEGORY_TYPE_",
                            StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.Order)
                        .ToList();

                    foreach (var def in dynamicCategoryColumns)
                    {
                        selectedColumns.Add(new ColumnDef
                        {
                            FieldKey = def.FieldKey,
                            Order = def.Order,
                            Selector = def.Selector,
                            Header = def.Header // usamos header por defecto
                        });
                    }

                    continue; // siguiente dtoCol
                }

                // ==============================
                // CASO ESPECIAL: TODOS OBJETIVOS DINÁMICOS
                // ==============================
                if (key == "MATRIX_DYNAMIC_OBJECTIVES")
                {
                    var dynamicObjectiveColumns = allColumns
                        .Where(c => c.FieldKey.StartsWith("OBJECTIVE_",
                            StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.Order)
                        .ToList();

                    foreach (var def in dynamicObjectiveColumns)
                    {
                        selectedColumns.Add(new ColumnDef
                        {
                            FieldKey = def.FieldKey,
                            Order = def.Order,
                            Selector = def.Selector,
                            Header = def.Header
                        });
                    }

                    continue;
                }

                // ==============================
                // CASO NORMAL: 1 fieldKey → 1 columna
                // ==============================
                var defNormal = allColumns.FirstOrDefault(c =>
                    string.Equals(c.FieldKey, dtoCol.FieldKey,
                        StringComparison.OrdinalIgnoreCase));

                if (defNormal is null)
                {
                    _logger.LogDebug("FieldKey {FieldKey} no encontrado en columnas de matriz.", dtoCol.FieldKey);
                    continue;
                }

                selectedColumns.Add(new ColumnDef
                {
                    FieldKey = defNormal.FieldKey,
                    Order = defNormal.Order,
                    Selector = defNormal.Selector,
                    Header = string.IsNullOrWhiteSpace(dtoCol.Header)
                        ? defNormal.Header
                        : dtoCol.Header!
                });
            }

            if (selectedColumns.Count == 0)
                throw new InvalidOperationException(
                    "Ninguna de las columnas solicitadas coincide con las columnas disponibles de la matriz.");

            // 3) Generar Excel con EPPlus (igual estilo que MatrixExcelExportService)
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(
                string.IsNullOrWhiteSpace(request.Name) ? "Matriz proyectos" : request.Name);

            var row = 1;
            var col = 1;

            // 3.1) Encabezados (en orden del DTO expandido)
            foreach (var c in selectedColumns)
            {
                ws.Cells[row, col].Value = c.Header;
                col++;
            }

            // 3.2) Filas de datos
            row = 2;
            foreach (var p in projects)
            {
                col = 1;
                foreach (var c in selectedColumns)
                {
                    ws.Cells[row, col].Value = c.Selector(p);
                    col++;
                }
                row++;
            }

            // Ajustar ancho
            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        // =========================================================
        //          Metadata de columnas (como en MatrixExcelExportService)
        //          + FieldKey para filtrado por DTO
        // =========================================================

        private static IEnumerable<ColumnDef> GetBaseColumns()
        {
            var list = new List<ColumnDef>();
            var i = 1;

            // 1  PROJECT_CODE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_CODE",
                Header = "Código de proyecto",
                Selector = p => p.ProjectCode ?? "-"
            });

            // 2  PROJECT_NAME
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_NAME",
                Header = "Nombre del proyecto",
                Selector = p => p.ProjectName ?? "-"
            });

            // 3  PROJECT_NUMBER
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_NUMBER",
                Header = "Número de proyecto",
                Selector = p => p.ProjectNumber
            });

            // 4  PROJECT_TYPE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_TYPE",
                Header = "Tipo de proyecto",
                Selector = p => p.ProjectTypeName ?? "-"
            });

            // 5  PROJECT_STATE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_STATE",
                Header = "Estado del proyecto",
                Selector = p => p.ProjectStateName ?? "-"
            });

            // 6  CONVOCATION_NAME
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CONVOCATION_NAME",
                Header = "Convocatoria",
                Selector = p => p.ConvocationName ?? "-"
            });

            // 7  APPROVAL_DATE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "APPROVAL_DATE",
                Header = "Fecha de aprobación",
                Selector = p => FormatDate(p.ApprovalDate)
            });

            // 8  START_DATE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "START_DATE",
                Header = "Fecha de inicio",
                Selector = p => FormatDate(p.StartDate)
            });

            // 9  DURATION_MONTHS
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "DURATION_MONTHS",
                Header = "Duración (meses)",
                Selector = p => p.DurationInMonths
            });

            // 10 TENTATIVE_END_DATE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "TENTATIVE_END_DATE",
                Header = "Fecha tentativa de fin",
                Selector = p => FormatDate(p.TentativeEndDate)
            });

            // 11 REAL_END_DATE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "REAL_END_DATE",
                Header = "Fecha real de fin",
                Selector = p => FormatDate(p.RealEndDate)
            });

            // 12 EXECUTION_PERCENTAGE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXECUTION_PERCENTAGE",
                Header = "% de ejecución",
                Selector = p => p.ExecutionPercentage?.ToString("N2") ?? "-"
            });

            // 13 FACULTY_NAME
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "FACULTY_NAME",
                Header = "Facultad",
                Selector = p => p.FacultyName ?? "-"
            });

            // 14 COORDINATOR_NAME
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_NAME",
                Header = "Coordinador del proyecto",
                Selector = p => p.CoordinatorName ?? "-"
            });

            // 15 COORDINATOR_EMAIL
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_EMAIL",
                Header = "Correo del coordinador",
                Selector = p => p.CoordinatorEmail ?? "-"
            });

            // 16 COORDINATOR_PHONE
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_PHONE",
                Header = "Teléfono del coordinador",
                Selector = p => p.CoordinatorPhone ?? "-"
            });

            // 17 SENESCYT_MEMBERS
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "SENESCYT_MEMBERS",
                Header = "Investigadores SENESCYT",
                Selector = p => GetSenescytNamesSummary(p)
            });

            // 18 EXTERNAL_RESEARCHER_NAMES
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXTERNAL_RESEARCHER_NAMES",
                Header = "Investigadores externos",
                Selector = p => GetExternalNamesSummary(p)
            });

            // 19 EXTERNAL_INSTITUTIONS
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXTERNAL_INSTITUTIONS",
                Header = "Instituciones de investigadores externos",
                Selector = p => GetExternalInstitutionsSummary(p)
            });

            // 20 BUDGET_INITIAL_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_INITIAL_SUMMARY",
                Header = "Presupuesto inicial (resumen)",
                Selector = p => GetBudgetInitialSummary(p)
            });

            // 21 BUDGET_FUNDINGTYPES_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_FUNDINGTYPES_SUMMARY",
                Header = "Tipos de financiamiento",
                Selector = p => GetBudgetFundingTypesSummary(p)
            });

            // 22 BUDGET_CERTIFIED_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_CERTIFIED_SUMMARY",
                Header = "Presupuesto certificado (resumen)",
                Selector = p => GetBudgetCertifiedSummary(p)
            });

            // 23 BUDGET_EXECUTED_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_EXECUTED_SUMMARY",
                Header = "Presupuesto ejecutado (resumen)",
                Selector = p => GetBudgetExecutedSummary(p)
            });

            // 24 PRODUCT_TITLES_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PRODUCT_TITLES_SUMMARY",
                Header = "Productos (títulos)",
                Selector = p => GetProductTitlesSummary(p)
            });

            // 25 PRODUCT_TYPES_SUMMARY
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PRODUCT_TYPES_SUMMARY",
                Header = "Productos (tipos)",
                Selector = p => GetProductTypesSummary(p)
            });

            // 26 CASES_OBJETIVOS (bundle)
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CASES_OBJETIVOS",
                Header = "Objetivos del proyecto (bundle)",
                Selector = p => ResolveObjetivosBundle(p)
            });

            // 27 CASES_RESEARCH_CATEGORIES (bundle)
            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CASES_RESEARCH_CATEGORIES",
                Header = "Categorías de investigación (bundle)",
                Selector = p => ResolveResearchCategoriesBundle(p)
            });

            return list;
        }


        private static string ResolveObjetivosBundle(ProjectFlatReportDTO p)
        {
            if (p.Objectives is null || p.Objectives.Count == 0)
                return "-";

            // Objetivo general (typeId == 1)
            var general = p.Objectives
                .Where(o => o.ObjectiveTypeId == 1)
                .OrderBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .FirstOrDefault();

            var especificos = p.Objectives
                .Where(o => o.ObjectiveTypeId != 1)
                .OrderBy(o => o.ObjectiveTypeId)
                .ThenBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var sb = new List<string>();

            if (!string.IsNullOrWhiteSpace(general))
            {
                sb.Add("GENERAL: " + general);
            }

            if (especificos.Count > 0)
            {
                sb.Add("ESPECÍFICOS:");
                sb.AddRange(especificos.Select(e => "- " + e));
            }

            return sb.Count == 0 ? "-" : string.Join(Environment.NewLine, sb);
        }

        private static string ResolveResearchCategoriesBundle(ProjectFlatReportDTO p)
        {
            if (p.ResearchCategories is null || p.ResearchCategories.Count == 0)
                return "-";

            var names = p.ResearchCategories
                .Select(c =>
                    string.IsNullOrWhiteSpace(c.CategoryName)
                        ? c.ResearchCategoryId.ToString()
                        : c.CategoryName)
                .Distinct()
                .ToList();

            return names.Count == 0 ? "-" : string.Join(", ", names);
        }

        // ------------------ CATEGORÍAS DINÁMICAS ------------------

        private static Dictionary<int, ResearchCategoryTreeItemDTO> BuildCategoryLookups(
            IReadOnlyList<ResearchCategoryTreeItemDTO> categoryTree)
        {
            var dict = new Dictionary<int, ResearchCategoryTreeItemDTO>();

            void Recurse(ResearchCategoryTreeItemDTO node)
            {
                dict[node.Id] = node;
                if (node.Children is null) return;
                foreach (var c in node.Children)
                    Recurse(c);
            }

            foreach (var root in categoryTree)
                Recurse(root);

            return dict;
        }

        private IEnumerable<ColumnDef> BuildCategoryColumnsMetadata(
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            if (categoryById.Count == 0)
                return Array.Empty<ColumnDef>();

            var list = new List<ColumnDef>();

            var typeInfos = categoryById.Values
                .GroupBy(n => n.ResearchCategoryTypeId)
                .Select(g => new
                {
                    TypeId = g.Key,
                    TypeName = g.First().ResearchCategoryTypeName
                })
                .OrderBy(x => x.TypeId)
                .ToList();

            var baseOrder = 1000; // después de base
            var offset = 0;

            foreach (var type in typeInfos)
            {
                var localTypeId = type.TypeId;
                var header = string.IsNullOrWhiteSpace(type.TypeName)
                    ? $"Tipo {localTypeId}"
                    : type.TypeName;

                list.Add(new ColumnDef
                {
                    Order = baseOrder + (offset++),
                    FieldKey = $"CATEGORY_TYPE_{localTypeId}", // clave para DTO
                    Header = header,
                    Selector = p => GetCategoriesForTypeSummary(p, localTypeId, categoryById)
                });
            }

            return list;
        }

        private static string GetCategoriesForTypeSummary(
            ProjectFlatReportDTO project,
            int typeId,
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            if (project.ResearchCategories is null || project.ResearchCategories.Count == 0)
                return "-";

            var categoryIds = project.ResearchCategories
                .Select(rc => rc.ResearchCategoryId)
                .Distinct()
                .ToList();

            if (categoryIds.Count == 0)
                return "-";

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in categoryIds)
            {
                if (!categoryById.TryGetValue(id, out var leaf))
                    continue;

                foreach (var node in GetCategoryPathToRoot(leaf, categoryById))
                {
                    if (node.ResearchCategoryTypeId == typeId &&
                        !string.IsNullOrWhiteSpace(node.Name))
                    {
                        names.Add(node.Name);
                    }
                }
            }

            return names.Count == 0 ? "-" : string.Join(", ", names);
        }

        private static IEnumerable<ResearchCategoryTreeItemDTO> GetCategoryPathToRoot(
            ResearchCategoryTreeItemDTO leaf,
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            var current = leaf;
            while (current is not null)
            {
                yield return current;

                if (current.ParentCategoryId is null)
                    break;

                if (!categoryById.TryGetValue(current.ParentCategoryId.Value, out var parent))
                    break;

                current = parent;
            }
        }

        // ------------------ OBJETIVOS DINÁMICOS ------------------

        private IEnumerable<ColumnDef> BuildObjectiveColumnsMetadata(
            IReadOnlyList<ProjectFlatReportDTO> projects)
        {
            if (projects is null || projects.Count == 0)
                return Array.Empty<ColumnDef>();

            var list = new List<ColumnDef>();

            var objectiveTypes = projects
                .Where(p => p.Objectives is not null && p.Objectives.Count > 0)
                .SelectMany(p => p.Objectives!)
                .GroupBy(o => new { o.ObjectiveTypeId, o.ObjectiveTypeName })
                .Select(g =>
                {
                    var typeId = g.Key.ObjectiveTypeId;

                    var maxCountForType = projects
                        .Where(p => p.Objectives is not null)
                        .Select(p => p.Objectives!.Count(o => o.ObjectiveTypeId == typeId))
                        .DefaultIfEmpty(0)
                        .Max();

                    return new
                    {
                        TypeId = typeId,
                        TypeName = g.Key.ObjectiveTypeName,
                        MaxCount = maxCountForType
                    };
                })
                .Where(x => x.MaxCount > 0)
                .OrderBy(x => x.TypeId)
                .ToList();

            var baseOrder = 2000; // después de categorías
            var offset = 0;

            foreach (var ot in objectiveTypes)
            {
                var baseLabel = GetObjectiveTypeLabel(ot.TypeId, ot.TypeName);

                for (var idx = 1; idx <= ot.MaxCount; idx++)
                {
                    var header = $"{baseLabel} {idx}";
                    var localTypeId = ot.TypeId;
                    var localIndex = idx;

                    list.Add(new ColumnDef
                    {
                        Order = baseOrder + (offset++),
                        FieldKey = $"OBJECTIVE_{localTypeId}_{localIndex}", // clave para DTO
                        Header = header,
                        Selector = p => GetObjectiveText(p, localTypeId, localIndex)
                    });
                }
            }

            return list;
        }

        private static string GetObjectiveTypeLabel(int typeId, string? defaultName)
            => typeId switch
            {
                1 => "Objetivo general",
                2 => "Objetivo específico",
                _ => string.IsNullOrWhiteSpace(defaultName)
                        ? $"Tipo {typeId}"
                        : defaultName!
            };

        private static string GetObjectiveText(
            ProjectFlatReportDTO project,
            int typeId,
            int index)
        {
            if (project.Objectives is null || project.Objectives.Count == 0)
                return "-";

            var objectives = project.Objectives
                .Where(o => o.ObjectiveTypeId == typeId)
                .OrderBy(o => o.ObjectiveId)
                .ToList();

            if (index <= 0 || index > objectives.Count)
                return "-";

            var obj = objectives[index - 1];
            return string.IsNullOrWhiteSpace(obj.Objective) ? "-" : obj.Objective;
        }

        // =========================================================
        //   Helpers de resumen (idénticos al MatrixExcelExportService)
        // =========================================================

        private static string FormatDate(DateTime? date)
            => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "-";

        private static string GetBudgetInitialSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return "-";

            var values = project.Budgets.Select(b => b.InitialAmount).ToList();

            return values.Count == 0
                ? "-"
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetFundingTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return "-";

            var names = project.Budgets
                .Select(b => b.FundingTypeName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? "-" : string.Join(" | ", names);
        }

        private static string GetBudgetCertifiedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return "-";

            var values = project.Budgets.Select(b => b.CertifiedAmount).ToList();

            return values.Count == 0
                ? "-"
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetExecutedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return "-";

            var values = project.Budgets.Select(b => b.ExecutedAmount).ToList();

            return values.Count == 0
                ? "-"
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        private static string GetProductTitlesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return "-";

            var titles = project.Products
                .Select(p => p.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return titles.Count == 0 ? "-" : string.Join(" | ", titles);
        }

        private static string GetProductTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return "-";

            var types = project.Products
                .Select(p => p.ProductTypeName ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            return types.Count == 0 ? "-" : string.Join(" | ", types);
        }

        private static string GetExternalNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return "-";

            var names = project.ExternalResearchers
                .Select(r => r.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            return names.Count == 0 ? "-" : string.Join(" | ", names);
        }

        private static string GetExternalInstitutionsSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return "-";

            var insts = project.ExternalResearchers
                .Select(r => r.InstitutionName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return insts.Count == 0 ? "-" : string.Join(" | ", insts);
        }

        private static string GetSenescytNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.SenescytMembers is null || project.SenescytMembers.Count == 0) return "-";

            var names = project.SenescytMembers
                .Select(m => m.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? "-" : string.Join(" | ", names);
        }
    }
}
