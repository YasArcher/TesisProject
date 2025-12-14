using ClosedXML.Excel;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Entities.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExportTemplateExcelService : IExportTemplateExcelService
    {
        private readonly IProjectFlatReportService _flatService;
        private readonly ILogger<ExportTemplateExcelService> _logger;

        public ExportTemplateExcelService(
            IProjectFlatReportService flatService,
            ILogger<ExportTemplateExcelService> logger)
        {
            _flatService = flatService;
            _logger = logger;
        }

        /// <summary>
        /// Genera un Excel a partir de una definición genérica de columnas,
        /// ya sea que provenga de una plantilla guardada o de una selección ad-hoc.
        /// </summary>
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

            // 1) Cargar dataset plano
            var flatResult = await _flatService.GetFlatReportAsync(null,ct);
            if (!flatResult.Success || flatResult.Data is null)
            {
                return ServiceResult<byte[]>.Fail(
                    flatResult.Message ?? "No se pudo obtener el informe plano de proyectos.");
            }

            var projects = flatResult.Data.ToList();
            if (projects.Count == 0)
            {
                return ServiceResult<byte[]>.Fail(
                    "No hay proyectos en el informe plano.",
                    ErrorType.Validation);
            }

            try
            {
                // 2) Generar Excel en memoria
                var bytes = GenerateExcelInternal(request, projects);
                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al generar el Excel para la exportación solicitada.");

                return ServiceResult<byte[]>.Fail(
                    "Error al generar el archivo Excel.",
                    ErrorType.Unexpected);
            }
        }

        // ===========================================
        //          LÓGICA INTERNA DE EXCEL
        // ===========================================
        private byte[] GenerateExcelInternal(
            ExportRequestDTO request,
            List<ProjectFlatReportDTO> projects)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(request.Name)
                ? "Proyectos"
                : request.Name);

            // ===============================
            // 1) ENCABEZADOS
            // ===============================
            int col = 1;
            int row = 1;

            var orderedColumns = request.Columns
                .OrderBy(c => c.OrderIndex)
                .ToList();

            foreach (var colDef in orderedColumns)
            {
                var header = string.IsNullOrWhiteSpace(colDef.Header)
                    ? colDef.FieldKey
                    : colDef.Header!;

                ws.Cell(row, col).Value = header;
                ws.Cell(row, col).Style.Font.Bold = true;
                ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                ws.Cell(row, col).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                col++;
            }

            row++;

            // ===============================
            // 2) FILAS DE DATOS
            // ===============================
            foreach (var p in projects)
            {
                col = 1;

                foreach (var colDef in orderedColumns)
                {
                    var rawValue = ResolveFieldValue(p, colDef.FieldKey);
                    var finalValue = ApplyFormat(rawValue, colDef);

                    ws.Cell(row, col).Value = finalValue;
                    col++;
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ===========================================
        //   3) RESOLVER VALOR POR FIELD KEY
        // ===========================================
        private string ResolveFieldValue(ProjectFlatReportDTO p, string fieldKey)
        {
            if (p == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(fieldKey)) return string.Empty;

            switch (fieldKey.ToUpperInvariant())
            {
                // =========================
                //      PROJECT GRAIN
                // =========================
                case "PROJECT_ID":
                    return p.ProjectId.ToString();
                case "PROJECT_CODE":
                    return p.ProjectCode ?? string.Empty;
                case "PROJECT_NAME":
                    return p.ProjectName ?? string.Empty;
                case "PROJECT_NUMBER":
                    return p.ProjectNumber.ToString();

                case "PROJECT_TYPE_ID":
                    return p.ProjectTypeId.ToString();
                case "PROJECT_TYPE_NAME":
                    return p.ProjectTypeName ?? string.Empty;

                case "PROJECT_STATE_ID":
                    return p.ProjectStateId.ToString();
                case "PROJECT_STATE_NAME":
                    return p.ProjectStateName ?? string.Empty;

                case "PROJECT_GROUP_ID":
                    return p.ProjectGroupId.ToString();
                case "GROUP_NAME":
                    return p.GroupName ?? string.Empty;
                case "GROUP_TYPE_NAME":
                    return p.GroupTypeName ?? string.Empty;

                case "CONVOCATION_ID":
                    return p.ConvocationId.ToString();
                case "CONVOCATION_NAME":
                    return p.ConvocationName ?? string.Empty;

                case "APPROVAL_DATE":
                    return FormatDate(p.ApprovalDate);
                case "START_DATE":
                    return FormatDate(p.StartDate);
                case "DURATION_MONTHS":
                    return p.DurationInMonths.ToString();
                case "TENTATIVE_END_DATE":
                    return FormatDate(p.TentativeEndDate);
                case "REAL_END_DATE":
                    return FormatDate(p.RealEndDate);
                case "EXECUTION_PERCENTAGE":
                    return p.ExecutionPercentage?.ToString("N2") ?? string.Empty;

                case "FACULTY_ID":
                    return p.FacultyId.ToString();
                case "FACULTY_NAME":
                    return p.FacultyName ?? string.Empty;

                // =========================
                //  COORDINADOR / DIRECTOR
                // =========================
                case "COORDINATOR_NAME":
                    return p.CoordinatorName ?? string.Empty;
                case "COORDINATOR_EMAIL":
                    return p.CoordinatorEmail ?? string.Empty;
                case "COORDINATOR_PHONE":
                    return p.CoordinatorPhone ?? string.Empty;

                // =========================
                //        BUDGETS
                // =========================
                case "BUDGET_INITIAL_SUMMARY":
                    return GetBudgetInitialSummary(p);
                case "BUDGET_CERTIFIED_SUMMARY":
                    return GetBudgetCertifiedSummary(p);
                case "BUDGET_EXECUTED_SUMMARY":
                    return GetBudgetExecutedSummary(p);
                case "BUDGET_FUNDING_TYPES":
                    return GetBudgetFundingTypesSummary(p);

                // =========================
                //        PRODUCTS
                // =========================
                case "PRODUCT_TITLES":
                    return GetProductTitlesSummary(p);
                case "PRODUCT_TYPES":
                    return GetProductTypesSummary(p);

                // =========================
                //  INVESTIGADORES EXTERNOS
                // =========================
                case "EXTERNAL_RESEARCHER_NAMES":
                    return GetExternalNamesSummary(p);
                case "EXTERNAL_RESEARCHER_INSTITUTIONS":
                    return GetExternalInstitutionsSummary(p);

                // =========================
                //  INVESTIGADORES SENESCYT
                // =========================
                case "SENESCYT_MEMBER_NAMES":
                    return GetSenescytNamesSummary(p);

                // =========================
                //     BUNDLES ESPECIALES
                // =========================
                case "CASES_OBJETIVOS":
                    return ResolveObjetivosBundle(p);
                case "CASES_RESEARCH_CATEGORIES":
                    return ResolveResearchCategoriesBundle(p);

                default:
                    // Si el campo todavía no está mapeado, devolvemos vacío
                    _logger.LogDebug("FieldKey {FieldKey} not yet mapped in ResolveFieldValue.", fieldKey);
                    return string.Empty;
            }
        }

        private string ApplyFormat(string rawValue, ExportColumnDTO colDef)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return string.Empty;

            // Por ahora el Format/Separator se dejan como informativos.
            // Aquí podrías aplicar formatos de número/fecha o separadores personalizados.
            return rawValue;
        }

        // =========================
        //   Helpers de BUDGETS
        // =========================
        private static string GetBudgetInitialSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;

            var values = project.Budgets.Select(b => b.InitialAmount).ToList();

            return values.Count == 0
                ? string.Empty
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetFundingTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;

            var names = project.Budgets
                .Select(b => b.FundingTypeName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? string.Empty : string.Join(" | ", names);
        }

        private static string GetBudgetCertifiedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;

            var values = project.Budgets
                .Select(b => b.CertifiedAmount)
                .ToList();

            return values.Count == 0
                ? string.Empty
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        private static string GetBudgetExecutedSummary(ProjectFlatReportDTO project)
        {
            if (project.Budgets is null || project.Budgets.Count == 0) return string.Empty;

            var values = project.Budgets
                .Select(b => b.ExecutedAmount)
                .ToList();

            return values.Count == 0
                ? string.Empty
                : string.Join(" | ", values.Select(v => v.ToString("N2")));
        }

        // =========================
        //    Helpers de PRODUCTS
        // =========================
        private static string GetProductTitlesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return string.Empty;

            var titles = project.Products
                .Select(p => p.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return titles.Count == 0 ? string.Empty : string.Join(" | ", titles);
        }

        private static string GetProductTypesSummary(ProjectFlatReportDTO project)
        {
            if (project.Products is null || project.Products.Count == 0) return string.Empty;

            var types = project.Products
                .Select(p => p.ProductTypeName ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            return types.Count == 0 ? string.Empty : string.Join(" | ", types);
        }

        // =========================
        //   Helpers de EXTERNOS
        // =========================
        private static string GetExternalNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return string.Empty;

            var names = project.ExternalResearchers
                .Select(r => r.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            return names.Count == 0 ? string.Empty : string.Join(" | ", names);
        }

        private static string GetExternalInstitutionsSummary(ProjectFlatReportDTO project)
        {
            if (project.ExternalResearchers is null || project.ExternalResearchers.Count == 0) return string.Empty;

            var insts = project.ExternalResearchers
                .Select(r => r.InstitutionName ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return insts.Count == 0 ? string.Empty : string.Join(" | ", insts);
        }

        private static string GetSenescytNamesSummary(ProjectFlatReportDTO project)
        {
            if (project.SenescytMembers is null || project.SenescytMembers.Count == 0) return string.Empty;

            var names = project.SenescytMembers
                .Select(m => m.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? string.Empty : string.Join(" | ", names);
        }

        // =========================
        //   Helpers de BUNDLES
        // =========================
        private static string ResolveObjetivosBundle(ProjectFlatReportDTO p)
        {
            if (p.Objectives is null || p.Objectives.Count == 0)
                return string.Empty;

            // Objetivo general (asumiendo typeId == 1)
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

            return sb.Count == 0 ? string.Empty : string.Join(Environment.NewLine, sb);
        }

        private static string ResolveResearchCategoriesBundle(ProjectFlatReportDTO p)
        {
            if (p.ResearchCategories is null || p.ResearchCategories.Count == 0)
                return string.Empty;

            // Si CategoryName viene poblado en el DTO, lo usamos; si no, devolvemos IDs.
            var names = p.ResearchCategories
                .Select(c =>
                    string.IsNullOrWhiteSpace(c.CategoryName)
                        ? c.ResearchCategoryId.ToString()
                        : c.CategoryName)
                .Distinct()
                .ToList();

            return names.Count == 0 ? string.Empty : string.Join(", ", names);
        }

        // =========================
        //   Formato de fechas
        // =========================
        private static string FormatDate(DateTime? date)
            => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}