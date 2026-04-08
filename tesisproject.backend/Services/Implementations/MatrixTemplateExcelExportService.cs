using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    /// <summary>
    /// Genera un Excel de matriz a partir de una plantilla persistida en BD.
    /// La plantilla define headers, orden y campos base; la UI solo envía:
    /// - TemplateId
    /// - ProjectIds
    /// - IncludedTemplateColumnIds
    /// - NameOverride
    /// </summary>
    public class MatrixTemplateExcelExportService : IMatrixTemplateExcelExportService
    {
        private const string EmptyPlaceholder = "-";
        private const string PipeSeparator = " | ";
        private const string DefaultWorksheetName = "Matriz proyectos";
        private const string NullRequestMessage = "La solicitud de exportación es nula.";
        private const string InvalidTemplateMessage = "Debe especificar una plantilla válida.";
        private const string AtLeastOneColumnMessage = "Debe seleccionar al menos una columna para exportar.";
        private const string TemplateNotRecoveredMessage = "No se pudo recuperar la plantilla.";
        private const string TemplateInactiveMessage = "La plantilla seleccionada está inactiva.";
        private const string TemplateWithoutColumnsMessage = "La plantilla no tiene columnas configuradas.";
        private const string IncludedColumnsNotInTemplateMessage = "La selección contiene columnas que no pertenecen a la plantilla.";
        private const string MissingRequiredColumnsMessage = "Faltan columnas obligatorias requeridas por la plantilla.";
        private const string NoValidSelectedColumnsMessage = "No hay columnas válidas seleccionadas para exportar.";
        private const string NoDataToExportMessage = "No hay datos para exportar.";
        private const string ErrorGeneratingExcelMessage = "Error al generar el archivo Excel.";

        private const string DynamicCategoriesKey = "MATRIX_DYNAMIC_CATEGORIES";
        private const string DynamicObjectivesKey = "MATRIX_DYNAMIC_OBJECTIVES";

        private const string CategoryTypePrefix = "CATEGORY_TYPE_";
        private const string ObjectivePrefix = "OBJECTIVE_";

        private const int CategoryColumnsBaseOrder = 1000;
        private const int ObjectiveColumnsBaseOrder = 2000;

        private readonly IProjectFlatReportService _flatService;
        private readonly IResearchCategoryService _categoryService;
        private readonly IExportTemplateService _templateService;
        private readonly ILogger<MatrixTemplateExcelExportService> _logger;

        public MatrixTemplateExcelExportService(
            IProjectFlatReportService flatService,
            IResearchCategoryService categoryService,
            IExportTemplateService templateService,
            ILogger<MatrixTemplateExcelExportService> logger)
        {
            _flatService = flatService;
            _categoryService = categoryService;
            _templateService = templateService;
            _logger = logger;
        }

        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportByTemplateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<byte[]>.Fail(
                    NullRequestMessage,
                    ErrorType.Validation);

            if (request.TemplateId <= 0)
                return ServiceResult<byte[]>.Fail(
                    InvalidTemplateMessage,
                    ErrorType.Validation);

            var includedColumnIds = request.IncludedTemplateColumnIds?
                .Distinct()
                .ToList() ?? new List<int>();

            if (includedColumnIds.Count == 0)
                return ServiceResult<byte[]>.Fail(
                    AtLeastOneColumnMessage,
                    ErrorType.Validation);

            // 1) Cargar plantilla desde BD
            var templateResult = await _templateService.GetTemplateAsync(request.TemplateId, ct);
            if (!templateResult.Success || templateResult.Data is null)
                return ServiceResult<byte[]>.Fail(
                    templateResult.Message ?? TemplateNotRecoveredMessage,
                    ErrorType.Validation);

            var template = templateResult.Data;

            if (!template.IsActive)
                return ServiceResult<byte[]>.Fail(
                    TemplateInactiveMessage,
                    ErrorType.Validation);

            if (template.Columns is null || template.Columns.Count == 0)
                return ServiceResult<byte[]>.Fail(
                    TemplateWithoutColumnsMessage,
                    ErrorType.Validation);

            var templateColumns = template.Columns
                .OrderBy(c => c.OrderIndex)
                .ToList();

            var templateColumnIds = templateColumns
                .Select(c => c.Id)
                .ToHashSet();

            var invalidIncludedIds = includedColumnIds
                .Where(id => !templateColumnIds.Contains(id))
                .ToList();

            if (invalidIncludedIds.Count > 0)
                return ServiceResult<byte[]>.Fail(
                    IncludedColumnsNotInTemplateMessage,
                    ErrorType.Validation);

            var requiredNotIncluded = templateColumns
                .Where(c => c.IsRequired && !includedColumnIds.Contains(c.Id))
                .Select(c => c.TargetHeader)
                .ToList();

            if (requiredNotIncluded.Count > 0)
                return ServiceResult<byte[]>.Fail(
                    MissingRequiredColumnsMessage,
                    ErrorType.Validation);

            var selectedTemplateColumns = templateColumns
                .Where(c => includedColumnIds.Contains(c.Id))
                .OrderBy(c => c.OrderIndex)
                .ToList();

            if (selectedTemplateColumns.Count == 0)
                return ServiceResult<byte[]>.Fail(
                    NoValidSelectedColumnsMessage,
                    ErrorType.Validation);

            // 2) Cargar dataset plano
            var flatResult = await _flatService.GetFlatReportAsync(request.ProjectIds, ct);
            if (!flatResult.Success || flatResult.Data is null || flatResult.Data.Count == 0)
                return ServiceResult<byte[]>.Fail(
                    flatResult.Message ?? NoDataToExportMessage);

            var projects = flatResult.Data.ToList();

            // 3) Cargar árbol de categorías
            var catResult = await _categoryService.GetTreeAsync(onlyActives: true, ct);
            var categoryTree = catResult.Success && catResult.Data is not null
                ? catResult.Data.ToList()
                : new List<ResearchCategoryTreeItemDTO>();

            var categoryById = BuildCategoryLookups(categoryTree);

            try
            {
                var bytes = GenerateExcelInternal(
                    request,
                    selectedTemplateColumns,
                    projects,
                    categoryById);

                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al generar el Excel de matriz basado en plantilla persistida. TemplateId={TemplateId}",
                    request.TemplateId);

                return ServiceResult<byte[]>.Fail(
                    ErrorGeneratingExcelMessage,
                    ErrorType.Unexpected);
            }
        }

        // =========================================================
        //              LÓGICA INTERNA DE EXCEL (EPPlus)
        // =========================================================

        private sealed class ColumnDef
        {
            public string FieldKey { get; init; } = string.Empty;
            public string Header { get; init; } = string.Empty;
            public int Order { get; init; }
            public Func<ProjectFlatReportDTO, object?> Selector { get; init; } = _ => null;
        }

        private static ColumnDef CloneColumn(ColumnDef source, string header)
            => new ColumnDef
            {
                FieldKey = source.FieldKey,
                Order = source.Order,
                Selector = source.Selector,
                Header = header
            };

        private byte[] GenerateExcelInternal(
            ExportByTemplateRequestDTO request,
            List<ExportTemplateColumnDTO> selectedTemplateColumns,
            List<ProjectFlatReportDTO> projects,
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            // 1) Construir TODAS las columnas disponibles del motor
            var allColumns = new List<ColumnDef>();
            allColumns.AddRange(GetBaseColumns());
            allColumns.AddRange(BuildCategoryColumnsMetadata(categoryById));
            allColumns.AddRange(BuildObjectiveColumnsMetadata(projects));

            // 2) Resolver columnas seleccionadas desde la plantilla
            var selectedColumns = new List<ColumnDef>();

            foreach (var templateCol in selectedTemplateColumns)
            {
                var resolvedFieldKey = NormalizeTemplateFieldKey(templateCol.FieldKey);

                if (string.IsNullOrWhiteSpace(resolvedFieldKey))
                    continue;

                // ==============================
                // CASO ESPECIAL: TODAS CATEGORÍAS DINÁMICAS
                // ==============================
                if (resolvedFieldKey == DynamicCategoriesKey)
                {
                    var dynamicCategoryColumns = allColumns
                        .Where(c => c.FieldKey.StartsWith(CategoryTypePrefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.Order)
                        .ToList();

                    foreach (var def in dynamicCategoryColumns)
                        selectedColumns.Add(CloneColumn(def, def.Header));

                    continue;
                }

                // ==============================
                // CASO ESPECIAL: TODOS OBJETIVOS DINÁMICOS
                // ==============================
                if (resolvedFieldKey == DynamicObjectivesKey)
                {
                    var dynamicObjectiveColumns = allColumns
                        .Where(c => c.FieldKey.StartsWith(ObjectivePrefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.Order)
                        .ToList();

                    foreach (var def in dynamicObjectiveColumns)
                        selectedColumns.Add(CloneColumn(def, def.Header));

                    continue;
                }

                // ==============================
                // CASO NORMAL: 1 FieldKey -> 1 columna
                // ==============================
                var defNormal = allColumns.FirstOrDefault(c =>
                    string.Equals(c.FieldKey, resolvedFieldKey, StringComparison.OrdinalIgnoreCase));

                if (defNormal is null)
                {
                    _logger.LogDebug(
                        "FieldKey {FieldKey} no encontrado en columnas disponibles de matriz.",
                        templateCol.FieldKey);
                    continue;
                }

                var header = string.IsNullOrWhiteSpace(templateCol.TargetHeader)
                    ? defNormal.Header
                    : templateCol.TargetHeader.Trim();

                selectedColumns.Add(CloneColumn(defNormal, header));
            }

            if (selectedColumns.Count == 0)
                throw new InvalidOperationException(
                    "Ninguna de las columnas seleccionadas coincide con las columnas disponibles de la matriz.");

            // 3) Generar Excel
            using var package = new ExcelPackage();

            var worksheetName = string.IsNullOrWhiteSpace(request.NameOverride)
                ? DefaultWorksheetName
                : request.NameOverride;

            var worksheet = package.Workbook.Worksheets.Add(worksheetName);

            var row = 1;
            var col = 1;

            // 3.1) Encabezados
            foreach (var column in selectedColumns)
            {
                worksheet.Cells[row, col].Value = column.Header;
                col++;
            }

            // 3.2) Datos
            row = 2;
            foreach (var project in projects)
            {
                col = 1;
                foreach (var column in selectedColumns)
                {
                    worksheet.Cells[row, col].Value = column.Selector(project);
                    col++;
                }
                row++;
            }

            if (worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        private static string NormalizeTemplateFieldKey(string? fieldKey)
        {
            if (string.IsNullOrWhiteSpace(fieldKey))
                return string.Empty;

            return fieldKey.Trim().ToUpperInvariant() switch
            {
                "CASES_OBJETIVOS" => DynamicObjectivesKey,
                "CASES_RESEARCH_CATEGORIES" => DynamicCategoriesKey,
                _ => fieldKey.Trim().ToUpperInvariant()
            };
        }

        // =========================================================
        //          Metadata de columnas del motor
        // =========================================================

        private static IEnumerable<ColumnDef> GetBaseColumns()
        {
            var list = new List<ColumnDef>();
            var i = 1;

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_CODE",
                Header = "Código de proyecto",
                Selector = p => p.ProjectCode ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_NAME",
                Header = "Nombre del proyecto",
                Selector = p => p.ProjectName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_NUMBER",
                Header = "Número de proyecto",
                Selector = p => p.ProjectNumber
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_TYPE",
                Header = "Tipo de proyecto",
                Selector = p => p.ProjectTypeName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PROJECT_STATE",
                Header = "Estado del proyecto",
                Selector = p => p.ProjectStateName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CONVOCATION_NAME",
                Header = "Convocatoria",
                Selector = p => p.ConvocationName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "APPROVAL_DATE",
                Header = "Fecha de aprobación",
                Selector = p => FormatDate(p.ApprovalDate)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "START_DATE",
                Header = "Fecha de inicio",
                Selector = p => FormatDate(p.StartDate)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "DURATION_MONTHS",
                Header = "Duración (meses)",
                Selector = p => p.DurationInMonths
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "TENTATIVE_END_DATE",
                Header = "Fecha tentativa de fin",
                Selector = p => FormatDate(p.TentativeEndDate)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "REAL_END_DATE",
                Header = "Fecha real de fin",
                Selector = p => FormatDate(p.RealEndDate)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXECUTION_PERCENTAGE",
                Header = "% de ejecución",
                Selector = p => p.ExecutionPercentage?.ToString("N2") ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "FACULTY_NAME",
                Header = "Facultad",
                Selector = p => p.FacultyName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_NAME",
                Header = "Coordinador del proyecto",
                Selector = p => p.CoordinatorName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_EMAIL",
                Header = "Correo del coordinador",
                Selector = p => p.CoordinatorEmail ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "COORDINATOR_PHONE",
                Header = "Teléfono del coordinador",
                Selector = p => p.CoordinatorPhone ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "SENESCYT_MEMBERS",
                Header = "Investigadores SENESCYT",
                Selector = p => GetSenescytNamesSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXTERNAL_RESEARCHER_NAMES",
                Header = "Investigadores externos",
                Selector = p => GetExternalNamesSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "EXTERNAL_INSTITUTIONS",
                Header = "Instituciones de investigadores externos",
                Selector = p => GetExternalInstitutionsSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_INITIAL_SUMMARY",
                Header = "Presupuesto inicial (resumen)",
                Selector = p => GetBudgetInitialSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_FUNDINGTYPES_SUMMARY",
                Header = "Tipos de financiamiento",
                Selector = p => GetBudgetFundingTypesSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_CERTIFIED_SUMMARY",
                Header = "Presupuesto certificado (resumen)",
                Selector = p => GetBudgetCertifiedSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "BUDGET_EXECUTED_SUMMARY",
                Header = "Presupuesto ejecutado (resumen)",
                Selector = p => GetBudgetExecutedSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PRODUCT_TITLES",
                Header = "Productos (títulos)",
                Selector = p => GetProductTitlesSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "PRODUCT_TYPES",
                Header = "Productos (tipos)",
                Selector = p => GetProductTypesSummary(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CASES_OBJETIVOS",
                Header = "Objetivos del proyecto (bundle)",
                Selector = p => ResolveObjetivosBundle(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "CASES_RESEARCH_CATEGORIES",
                Header = "Categorías de investigación (bundle)",
                Selector = p => ResolveResearchCategoriesBundle(p)
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "SUBROGANT_NAME",
                Header = "Subrogante del proyecto",
                Selector = p => p.SubrogantName ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "SUBROGANT_EMAIL",
                Header = "Correo del subrogante",
                Selector = p => p.SubrogantEmail ?? EmptyPlaceholder
            });

            list.Add(new ColumnDef
            {
                Order = i++,
                FieldKey = "SUBROGANT_PHONE",
                Header = "Teléfono del subrogante",
                Selector = p => p.SubrogantPhone ?? EmptyPlaceholder
            });

            return list;
        }

        private static string ResolveObjetivosBundle(ProjectFlatReportDTO project)
        {
            if (project.Objectives is null || project.Objectives.Count == 0)
                return EmptyPlaceholder;

            var general = project.Objectives
                .Where(o => o.ObjectiveTypeId == ObjectiveTypeIds.General)
                .OrderBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .FirstOrDefault();

            var especificos = project.Objectives
                .Where(o => o.ObjectiveTypeId != ObjectiveTypeIds.Specific)
                .OrderBy(o => o.ObjectiveTypeId)
                .ThenBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(general))
                lines.Add("GENERAL: " + general);

            if (especificos.Count > 0)
            {
                lines.Add("ESPECÍFICOS:");
                lines.AddRange(especificos.Select(e => "- " + e));
            }

            return lines.Count == 0 ? EmptyPlaceholder : string.Join(Environment.NewLine, lines);
        }

        private static string ResolveResearchCategoriesBundle(ProjectFlatReportDTO project)
        {
            if (project.ResearchCategories is null || project.ResearchCategories.Count == 0)
                return EmptyPlaceholder;

            var names = project.ResearchCategories
                .Select(c =>
                    string.IsNullOrWhiteSpace(c.CategoryName)
                        ? c.ResearchCategoryId.ToString()
                        : c.CategoryName)
                .Distinct()
                .ToList();

            return names.Count == 0 ? EmptyPlaceholder : string.Join(", ", names);
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

            var offset = 0;

            foreach (var type in typeInfos)
            {
                var localTypeId = type.TypeId;
                var header = string.IsNullOrWhiteSpace(type.TypeName)
                    ? $"Tipo {localTypeId}"
                    : type.TypeName;

                list.Add(new ColumnDef
                {
                    Order = CategoryColumnsBaseOrder + (offset++),
                    FieldKey = $"{CategoryTypePrefix}{localTypeId}",
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
                return EmptyPlaceholder;

            var categoryIds = project.ResearchCategories
                .Select(rc => rc.ResearchCategoryId)
                .Distinct()
                .ToList();

            if (categoryIds.Count == 0)
                return EmptyPlaceholder;

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

            return names.Count == 0 ? EmptyPlaceholder : string.Join(", ", names);
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
                        Order = ObjectiveColumnsBaseOrder + (offset++),
                        FieldKey = $"{ObjectivePrefix}{localTypeId}_{localIndex}",
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
                ObjectiveTypeIds.General => "Objetivo general",
                ObjectiveTypeIds.Specific => "Objetivo específico",
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
                return EmptyPlaceholder;

            var objectives = project.Objectives
                .Where(o => o.ObjectiveTypeId == typeId)
                .OrderBy(o => o.ObjectiveId)
                .ToList();

            if (index <= 0 || index > objectives.Count)
                return EmptyPlaceholder;

            var obj = objectives[index - 1];
            return string.IsNullOrWhiteSpace(obj.Objective) ? EmptyPlaceholder : obj.Objective;
        }

        // =========================================================
        //   Helpers de resumen
        // =========================================================

        private static string FormatDate(DateTime? date)
            => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : EmptyPlaceholder;

        private static string GetBudgetInitialSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.InitialAmount).ToList();

            return values.Count == 0
                ? EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetFundingTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return EmptyPlaceholder;

            var names = project.Budgets
                .Select(b => b.FundingTypeName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, names);
        }

        private static string GetBudgetCertifiedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.CertifiedAmount).ToList();

            return values.Count == 0
                ? EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetExecutedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.ExecutedAmount).ToList();

            return values.Count == 0
                ? EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetProductTitlesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return EmptyPlaceholder;

            var titles = project.Products
                .Select(p => p.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return titles.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, titles);
        }

        private static string GetProductTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return EmptyPlaceholder;

            var types = project.Products
                .Select(p => p.ProductTypeName ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            return types.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, types);
        }

        private static string GetExternalNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return EmptyPlaceholder;

            var names = project.ExternalResearchers
                .Select(r => r.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            return names.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, names);
        }

        private static string GetExternalInstitutionsSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return EmptyPlaceholder;

            var insts = project.ExternalResearchers
                .Select(r => r.InstitutionName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return insts.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, insts);
        }

        private static string GetSenescytNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.SenescytMembers is null || project.SenescytMembers.Count == 0) return EmptyPlaceholder;

            var names = project.SenescytMembers
                .Select(m => m.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? EmptyPlaceholder : string.Join(PipeSeparator, names);
        }
    }
}
