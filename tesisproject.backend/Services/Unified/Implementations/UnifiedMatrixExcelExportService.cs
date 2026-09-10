using OfficeOpenXml;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedMatrixExcelExportService : IUnifiedMatrixExcelExportService
    {


        private const string PipeSeparator = " | ";
        private const int CategoryColumnsBaseOrder = 1000; // para que vayan después de las base
        private const int ObjectiveColumnsBaseOrder = 2000; // después de categorías

        private readonly IUnifiedProjectFlatReportService _flatService;
        private readonly IUnifiedResearchCategoryService _categoryService;
        private readonly ILogger<UnifiedMatrixExcelExportService> _logger;

        public UnifiedMatrixExcelExportService(
            IUnifiedProjectFlatReportService flatService,
            IUnifiedResearchCategoryService categoryService,
            ILogger<UnifiedMatrixExcelExportService> logger)
        {
            _flatService = flatService;
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>
        /// Genera un Excel con la misma lógica de columnas que la pestaña de configuración
        /// (base + dinámicas de categorías + dinámicas de objetivos).
        /// </summary>
        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(IEnumerable<int>? projectIds = null, CancellationToken ct = default)
        {
            // 1) Cargar dataset plano
            var flatResult = await _flatService.GetFlatReportAsync(projectIds, ct);

            if (!flatResult.Success)
            {
                return ServiceResult<byte[]>.Fail(
                    flatResult.Message ?? ErrorMessages.Export.FlatReportUnavailable,
                    flatResult.Error == ErrorType.None ? ErrorType.Unexpected : flatResult.Error,
                    flatResult.ErrorCode ?? ErrorCodes.Export.FlatReportUnavailable,
                    flatResult.ValidationErrors);
            }

            if (flatResult.Data is null || flatResult.Data.Count == 0)
            {
                return ServiceResult<byte[]>.Fail(
                    ErrorMessages.Export.NoProjectsInFlatReport,
                    ErrorType.Validation,
                    ErrorCodes.Export.NoProjectsInFlatReport);
            }

            var projects = flatResult.Data.ToList();

            // 2) Cargar árbol de categorías
            var catResult = await _categoryService.GetTreeAsync(onlyActives: true, ct);
            var categoryTree = catResult.Success && catResult.Data is not null
                ? catResult.Data.ToList()
                : new List<ResearchCategoryTreeItemDTO>();

            // 3) Preparar lookups de categorías (igual que en el front)
            var categoryById = BuildCategoryLookups(categoryTree);

            // 4) Construir metadata de columnas (base + dinámicas)
            var columns = new List<ColumnDef>();

            // 4.1) Columnas base (copiadas de tu MatrixConfigTab)
            columns.AddRange(GetBaseColumns());

            // 4.2) Columnas dinámicas de categorías
            columns.AddRange(BuildCategoryColumnsMetadata(categoryById));

            // 4.3) Columnas dinámicas de objetivos
            columns.AddRange(BuildObjectiveColumnsMetadata(projects));

            // Ordenar columnas una sola vez (mismo resultado, menos duplicación)
            var orderedColumns = columns.OrderBy(c => c.Order).ToList();

            // 5) Generar Excel
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Matriz proyectos");

            var row = 1;
            var col = 1;

            // 5.1) Encabezados
            foreach (var column in orderedColumns)
            {
                worksheet.Cells[row, col].Value = column.Header;
                col++;
            }

            // 5.2) Filas de datos
            row = 2;
            foreach (var project in projects)
            {
                col = 1;
                foreach (var column in orderedColumns)
                {
                    worksheet.Cells[row, col].Value = column.Selector(project);
                    col++;
                }
                row++;
            }

            // Ajustar ancho
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return ServiceResult<byte[]>.Ok(bytes);
        }

        // =========================================================
        //          Metadata de columnas (como en el front)
        // =========================================================

        private sealed class ColumnDef
        {
            public string Header { get; init; } = string.Empty;
            public Func<ProjectFlatReportDTO, object?> Selector { get; init; } = _ => null;
            public int Order { get; init; }
        }

        private static IEnumerable<ColumnDef> GetBaseColumns()
        {
            var list = new List<ColumnDef>();
            var i = 1;

            list.Add(new ColumnDef { Order = i++, Header = "Código", Selector = p => p.ProjectCode });
            list.Add(new ColumnDef { Order = i++, Header = "Proyecto", Selector = p => p.ProjectName });
            list.Add(new ColumnDef { Order = i++, Header = "Tipo", Selector = p => p.ProjectTypeName ?? string.Empty });
            list.Add(new ColumnDef { Order = i++, Header = "Estado", Selector = p => p.ProjectStateName ?? string.Empty });
            list.Add(new ColumnDef { Order = i++, Header = "Convocatoria", Selector = p => p.ConvocationName ?? string.Empty });

            list.Add(new ColumnDef { Order = i++, Header = "F. Aprobación", Selector = p => FormatDate(p.ApprovalDate) });
            list.Add(new ColumnDef { Order = i++, Header = "F. Inicio", Selector = p => FormatDate(p.StartDate) });
            list.Add(new ColumnDef { Order = i++, Header = "Duración (m)", Selector = p => p.DurationInMonths });
            list.Add(new ColumnDef { Order = i++, Header = "F. Tentativa Fin", Selector = p => FormatDate(p.TentativeEndDate) });
            list.Add(new ColumnDef { Order = i++, Header = "F. Real Fin", Selector = p => FormatDate(p.RealEndDate) });

            list.Add(new ColumnDef { Order = i++, Header = "Facultad", Selector = p => p.FacultyName ?? string.Empty });

            list.Add(new ColumnDef { Order = i++, Header = "Coordinador", Selector = p => p.CoordinatorName ?? string.Empty });
            list.Add(new ColumnDef { Order = i++, Header = "Email Coord.", Selector = p => p.CoordinatorEmail ?? string.Empty });
            list.Add(new ColumnDef { Order = i++, Header = "Teléfono Coord.", Selector = p => p.CoordinatorPhone ?? string.Empty });

            list.Add(new ColumnDef { Order = i++, Header = "Presup. Inicial", Selector = p => GetBudgetInitialSummary(p) });
            list.Add(new ColumnDef { Order = i++, Header = "Tipo Financiamiento", Selector = p => GetBudgetFundingTypesSummary(p) });
            list.Add(new ColumnDef { Order = i++, Header = "Presup. Certificado", Selector = p => GetBudgetCertifiedSummary(p) });
            list.Add(new ColumnDef { Order = i++, Header = "Presup. Ejecutado", Selector = p => GetBudgetExecutedSummary(p) });

            list.Add(new ColumnDef { Order = i++, Header = "Productos (Títulos)", Selector = p => GetProductTitlesSummary(p) });
            list.Add(new ColumnDef { Order = i++, Header = "Productos (Tipos)", Selector = p => GetProductTypesSummary(p) });

            list.Add(new ColumnDef { Order = i++, Header = "Nombres Externos", Selector = p => GetExternalNamesSummary(p) });
            list.Add(new ColumnDef { Order = i++, Header = "Instituciones Externas", Selector = p => GetExternalInstitutionsSummary(p) });

            list.Add(new ColumnDef { Order = i++, Header = "Investigadores SENESCYT", Selector = p => GetSenescytNamesSummary(p) });

            return list;
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
                yield break;

            // group by typeId como en el front
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

                yield return new ColumnDef
                {
                    Order = CategoryColumnsBaseOrder + (offset++),
                    Header = header,
                    Selector = p => GetCategoriesForTypeSummary(p, localTypeId, categoryById)
                };
            }
        }

        private static string GetCategoriesForTypeSummary(
            ProjectFlatReportDTO project,
            int typeId,
            Dictionary<int, ResearchCategoryTreeItemDTO> categoryById)
        {
            if (project.ResearchCategories is null || project.ResearchCategories.Count == 0)
                return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var categoryIds = project.ResearchCategories
                .Select(rc => rc.ResearchCategoryId)
                .Distinct()
                .ToList();

            if (categoryIds.Count == 0)
                return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

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

            return names.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(", ", names);
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
                yield break;

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

                    yield return new ColumnDef
                    {
                        Order = ObjectiveColumnsBaseOrder + (offset++),
                        Header = header,
                        Selector = p => GetObjectiveText(p, localTypeId, localIndex)
                    };
                }
            }
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
                return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var objectives = project.Objectives
                .Where(o => o.ObjectiveTypeId == typeId)
                .OrderBy(o => o.ObjectiveId)
                .ToList();

            if (index <= 0 || index > objectives.Count)
                return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var obj = objectives[index - 1];
            return string.IsNullOrWhiteSpace(obj.Objective) ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : obj.Objective;
        }

        // =========================================================
        //   Helpers de resumen (copiados del componente Blazor)
        // =========================================================

        private static string FormatDate(DateTime? date)
            => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

        private static string GetBudgetInitialSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.InitialAmount).ToList();

            return values.Count == 0
                ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetFundingTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var names = project.Budgets
                .Select(b => b.FundingTypeName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, names);
        }

        private static string GetBudgetCertifiedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.CertifiedAmount).ToList();

            return values.Count == 0
                ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetExecutedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var values = project.Budgets.Select(b => b.ExecutedAmount).ToList();

            return values.Count == 0
                ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder
                : string.Join(PipeSeparator, values.Select(v => v.ToString("N2")));
        }

        private static string GetProductTitlesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var titles = project.Products
                .Select(p => p.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return titles.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, titles);
        }

        private static string GetProductTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var types = project.Products
                .Select(p => p.ProductTypeName ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            return types.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, types);
        }

        private static string GetExternalNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var names = project.ExternalResearchers
                .Select(r => r.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            return names.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, names);
        }

        private static string GetExternalInstitutionsSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var insts = project.ExternalResearchers
                .Select(r => r.InstitutionName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return insts.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, insts);
        }

        private static string GetSenescytNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.SenescytMembers is null || project.SenescytMembers.Count == 0) return ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder;

            var names = project.SenescytMembers
                .Select(m => m.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? ErrorMessages.UnifiedLegacy.MatrixExcelExportService_EmptyPlaceholder : string.Join(PipeSeparator, names);
        }
    }
}