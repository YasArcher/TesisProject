using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Constants;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExportTemplateExcelService : IExportTemplateExcelService
    {
        private readonly IProjectFlatReportService _flatService;
        private readonly ILogger<ExportTemplateExcelService> _logger;

        private const string PipeSep = " | ";
        private const string NullRequestMessage = "La solicitud de exportación es nula.";
        private const string NoColumnsDefinedMessage = "No se han definido columnas para la exportación.";
        private const string FlatReportUnavailableMessage = "No se pudo obtener el informe plano de proyectos.";
        private const string NoProjectsInFlatReportMessage = "No hay proyectos en el informe plano.";
        private const string OperationCanceledMessage = "La operación de exportación fue cancelada.";
        private const string ErrorGeneratingExcelMessage = "Error al generar el archivo Excel.";

        private static readonly IReadOnlyDictionary<string, Func<ProjectFlatReportDTO, string>> FieldResolvers
            = new Dictionary<string, Func<ProjectFlatReportDTO, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [ExportFieldKeys.ProjectId] = p => p.ProjectId.ToString(),
                [ExportFieldKeys.ProjectCode] = p => p.ProjectCode ?? string.Empty,
                [ExportFieldKeys.ProjectName] = p => p.ProjectName ?? string.Empty,
                [ExportFieldKeys.ProjectNumber] = p => p.ProjectNumber.ToString(),

                [ExportFieldKeys.ProjectTypeId] = p => p.ProjectTypeId.ToString(),
                [ExportFieldKeys.ProjectTypeName] = p => p.ProjectTypeName ?? string.Empty,

                [ExportFieldKeys.ProjectStateId] = p => p.ProjectStateId.ToString(),
                [ExportFieldKeys.ProjectStateName] = p => p.ProjectStateName ?? string.Empty,

                [ExportFieldKeys.ProjectGroupId] = p => p.ProjectGroupId.ToString(),
                [ExportFieldKeys.GroupName] = p => p.GroupName ?? string.Empty,
                [ExportFieldKeys.GroupTypeName] = p => p.GroupTypeName ?? string.Empty,

                [ExportFieldKeys.ConvocationId] = p => p.ConvocationId.ToString(),
                [ExportFieldKeys.ConvocationName] = p => p.ConvocationName ?? string.Empty,

                [ExportFieldKeys.ApprovalDate] = p => FormatDate(p.ApprovalDate),
                [ExportFieldKeys.StartDate] = p => FormatDate(p.StartDate),
                [ExportFieldKeys.DurationMonths] = p => p.DurationInMonths.ToString(),
                [ExportFieldKeys.TentativeEndDate] = p => FormatDate(p.TentativeEndDate),
                [ExportFieldKeys.RealEndDate] = p => FormatDate(p.RealEndDate),
                [ExportFieldKeys.ExecutionPercentage] = p => p.ExecutionPercentage?.ToString("N2") ?? string.Empty,

                [ExportFieldKeys.FacultyId] = p => p.FacultyId.ToString(),
                [ExportFieldKeys.FacultyName] = p => p.FacultyName ?? string.Empty,

                [ExportFieldKeys.CoordinatorName] = p => p.CoordinatorName ?? string.Empty,
                [ExportFieldKeys.CoordinatorEmail] = p => p.CoordinatorEmail ?? string.Empty,
                [ExportFieldKeys.CoordinatorPhone] = p => p.CoordinatorPhone ?? string.Empty,

                [ExportFieldKeys.BudgetInitialSummary] = GetBudgetInitialSummary,
                [ExportFieldKeys.BudgetCertifiedSummary] = GetBudgetCertifiedSummary,
                [ExportFieldKeys.BudgetExecutedSummary] = GetBudgetExecutedSummary,
                [ExportFieldKeys.BudgetFundingTypes] = GetBudgetFundingTypesSummary,

                [ExportFieldKeys.ProductTitles] = GetProductTitlesSummary,
                [ExportFieldKeys.ProductTypes] = GetProductTypesSummary,

                [ExportFieldKeys.ExternalResearcherNames] = GetExternalNamesSummary,
                [ExportFieldKeys.ExternalResearcherInstitutions] = GetExternalInstitutionsSummary,

                [ExportFieldKeys.SenescytMemberNames] = GetSenescytNamesSummary,

                [ExportFieldKeys.CasesObjetivos] = ResolveObjetivosBundle,
                [ExportFieldKeys.CasesResearchCategories] = ResolveResearchCategoriesBundle,
            };

        public ExportTemplateExcelService(
            IProjectFlatReportService flatService,
            ILogger<ExportTemplateExcelService> logger)
        {
            _flatService = flatService;
            _logger = logger;
        }

        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                {
                    return ValidationFailure<byte[]>(
                        NullRequestMessage,
                        ErrorCodes.Common.InvalidRequest,
                        "Request");
                }

                if (request.Columns is null || request.Columns.Count == 0)
                {
                    return ValidationFailure<byte[]>(
                        NoColumnsDefinedMessage,
                        ErrorCodes.Export.NoColumnsDefined,
                        nameof(ExportRequestDTO.Columns));
                }

                var flatResult = await _flatService.GetFlatReportAsync(null, ct);
                if (!flatResult.Success || flatResult.Data is null)
                {
                    return ServiceResult<byte[]>.Fail(
                        flatResult.Message ?? FlatReportUnavailableMessage,
                        flatResult.Error == ErrorType.None ? ErrorType.Unexpected : flatResult.Error,
                        flatResult.ErrorCode ?? ErrorCodes.Export.FlatReportUnavailable,
                        flatResult.ValidationErrors);
                }

                var projects = flatResult.Data.ToList();
                if (projects.Count == 0)
                {
                    return ServiceResult<byte[]>.Fail(
                        NoProjectsInFlatReportMessage,
                        ErrorType.Validation,
                        ErrorCodes.Export.NoProjectsInFlatReport);
                }

                var bytes = GenerateExcelInternal(request, projects);
                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<byte[]>.Fail(
                    OperationCanceledMessage,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.OperationCanceled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el Excel para la exportación solicitada.");
                return ServiceResult<byte[]>.Fail(
                    ErrorGeneratingExcelMessage,
                    ErrorType.Unexpected,
                    ErrorCodes.Export.ExcelGenerationFailed);
            }
        }

        private byte[] GenerateExcelInternal(ExportRequestDTO request, List<ProjectFlatReportDTO> projects)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(request.Name) ? "Proyectos" : request.Name);

            var orderedColumns = request.Columns
                .OrderBy(c => c.OrderIndex)
                .ToList();

            var resolvers = BuildColumnResolvers(orderedColumns);

            const int headerRow = 1;
            for (int i = 0; i < orderedColumns.Count; i++)
            {
                var colDef = orderedColumns[i];
                var header = string.IsNullOrWhiteSpace(colDef.Header) ? colDef.FieldKey : colDef.Header!;
                ws.Cell(headerRow, i + 1).Value = header;
            }

            if (orderedColumns.Count > 0)
            {
                var headerRange = ws.Range(headerRow, 1, headerRow, orderedColumns.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            int row = headerRow + 1;
            foreach (var p in projects)
            {
                for (int i = 0; i < resolvers.Count; i++)
                {
                    ws.Cell(row, i + 1).Value = resolvers[i](p);
                }
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private List<Func<ProjectFlatReportDTO, string>> BuildColumnResolvers(IReadOnlyList<ExportColumnDTO> orderedColumns)
        {
            var list = new List<Func<ProjectFlatReportDTO, string>>(orderedColumns.Count);

            foreach (var col in orderedColumns)
            {
                var key = (col.FieldKey ?? string.Empty).Trim();

                if (key.Length == 0)
                {
                    list.Add(_ => string.Empty);
                    continue;
                }

                if (FieldResolvers.TryGetValue(key, out var resolver))
                {
                    list.Add(resolver);
                }
                else
                {
                    _logger.LogDebug("FieldKey {FieldKey} not yet mapped in export.", col.FieldKey);
                    list.Add(_ => string.Empty);
                }
            }

            return list;
        }

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
        }

        private static string JoinPipe(IEnumerable<string?> items, bool distinct = false)
            => JoinNonEmpty(items, PipeSep, distinct);

        private static string JoinNonEmpty(IEnumerable<string?> items, string separator, bool distinct = false)
        {
            if (items is null) return string.Empty;

            var q = items
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s));

            if (distinct)
                q = q.Distinct(StringComparer.OrdinalIgnoreCase);

            var list = q.ToList();
            return list.Count == 0 ? string.Empty : string.Join(separator, list);
        }

        private static string FormatNumber<T>(T value, string format = "N2")
        {
            if (value is null) return string.Empty;

            return value is IFormattable f
                ? f.ToString(format, CultureInfo.InvariantCulture)
                : value.ToString() ?? string.Empty;
        }

        private static string GetBudgetInitialSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;
            return JoinPipe(project.Budgets.Select(b => FormatNumber(b.InitialAmount)));
        }

        private static string GetBudgetFundingTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;
            return JoinPipe(project.Budgets.Select(b => b.FundingTypeName), distinct: true);
        }

        private static string GetBudgetCertifiedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;
            return JoinPipe(project.Budgets.Select(b => FormatNumber(b.CertifiedAmount)));
        }

        private static string GetBudgetExecutedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;
            return JoinPipe(project.Budgets.Select(b => FormatNumber(b.ExecutedAmount)));
        }

        private static string GetProductTitlesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return string.Empty;
            return JoinPipe(project.Products.Select(p => p.Title));
        }

        private static string GetProductTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return string.Empty;
            return JoinPipe(project.Products.Select(p => p.ProductTypeName), distinct: true);
        }

        private static string GetExternalNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return string.Empty;
            return JoinPipe(project.ExternalResearchers.Select(r => r.FullName));
        }

        private static string GetExternalInstitutionsSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return string.Empty;
            return JoinPipe(project.ExternalResearchers.Select(r => r.InstitutionName), distinct: true);
        }

        private static string GetSenescytNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.SenescytMembers is null || project.SenescytMembers.Count == 0) return string.Empty;
            return JoinPipe(project.SenescytMembers.Select(m => m.FullName), distinct: true);
        }

        private static string ResolveObjetivosBundle(ProjectFlatReportDTO p)
        {
            if (p.Objectives is null || p.Objectives.Count == 0)
                return string.Empty;

            var general = p.Objectives
                .Where(o => o.ObjectiveTypeId == ObjectiveTypeIds.General)
                .OrderBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .FirstOrDefault();

            var especificos = p.Objectives
                .Where(o => o.ObjectiveTypeId == ObjectiveTypeIds.Specific)
                .OrderBy(o => o.ObjectiveId)
                .Select(o => o.Objective)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var sb = new List<string>();

            if (!string.IsNullOrWhiteSpace(general))
                sb.Add("GENERAL: " + general);

            if (especificos.Count > 0)
            {
                sb.Add("ESPECÍFICOS:");
                sb.AddRange(especificos.Select(e => "- " + e));
            }

            return sb.Count == 0 ? string.Empty : string.Join(Environment.NewLine, sb);
        }

        private static string ResolveResearchCategoriesBundle(ProjectFlatReportDTO p)
        {
            if (p.ResearchCategories is null || p.ResearchCategories.Count == 0)
                return string.Empty;

            var names = p.ResearchCategories
                .Select(c => string.IsNullOrWhiteSpace(c.CategoryName) ? c.ResearchCategoryId.ToString() : c.CategoryName)
                .Distinct()
                .ToList();

            return JoinNonEmpty(names, ", ", distinct: false);
        }

        private static string FormatDate(DateTime? date)
            => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}