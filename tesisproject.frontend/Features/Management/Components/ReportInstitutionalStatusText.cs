using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Components;

public static class ReportInstitutionalStatusText
{
    public static string FormatEtlDate(DateTime? value)
        => value.HasValue
            ? value.Value.ToString("dd/MM/yyyy HH:mm")
            : "Sin fecha";

    public static string GetEtlStatusClass(string? status)
        => string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            ? "reports-dw-status reports-dw-status--success"
            : "reports-dw-status reports-dw-status--warning";

    public static string GetInstitutionalStatusLabel(string? status)
        => string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            ? "actualizada"
            : "pendiente de actualización";

    public static bool HasReportingData(InstitutionalReportingDashboardDto? dashboard)
        => (dashboard?.ScientificProduction.TotalArticles ?? 0) > 0
        || (dashboard?.ParticipationSummary.TotalArticles ?? 0) > 0
        || (dashboard?.RecentArticles.Count ?? 0) > 0
        || (dashboard?.PediIiitArticles.Count ?? 0) > 0
        || (dashboard?.TddTotalArticles.Count ?? 0) > 0;

    public static string GetBlockingTitle(bool isRunningEtl, bool isRefreshing)
    {
        if (isRunningEtl)
        {
            return "Actualizando modelo analítico";
        }

        return isRefreshing
            ? "Aplicando filtros de reportería"
            : "Cargando reportería institucional";
    }

    public static string GetBlockingMessage(bool isRunningEtl, bool isRefreshing)
    {
        if (isRunningEtl)
        {
            return "Estamos ejecutando el ETL y sincronizando los indicadores del DW. Mantén esta pantalla abierta.";
        }

        return isRefreshing
            ? "Se están recalculando los bloques, tablas y porcentajes con los filtros seleccionados."
            : "Estamos preparando los indicadores institucionales, filtros y bloques analíticos.";
    }

    public static int GetProjectResultPercent(InstitutionalReportingDashboardDto? dashboard)
    {
        var totalArticles = dashboard?.ScientificProduction.TotalArticles ?? 0;
        if (totalArticles <= 0)
        {
            return 0;
        }

        return (int)Math.Round((dashboard!.ScientificProduction.ProjectResultArticles / (double)totalArticles) * 100);
    }

    public static string GetLastLoadedLabel(DateTime? value)
        => value.HasValue
            ? value.Value.ToString("dd/MM/yyyy HH:mm:ss")
            : "Sin lectura reciente";

    public static string GetScopeLabel(int activeFilterCount)
        => activeFilterCount == 0
            ? "Vista general institucional"
            : $"{activeFilterCount} filtros activos";

    public static string GetActiveFilterSummaryLabel(int activeFilterCount)
        => activeFilterCount switch
        {
            0 => "Sin segmentación adicional",
            1 => "1 criterio aplicado",
            _ => $"{activeFilterCount} criterios aplicados"
        };

    public static string GetDataPulseLabel(InstitutionalReportingDashboardDto? dashboard)
        => $"{(dashboard?.ScientificProduction.TotalArticles ?? 0):N0} artículos · {(dashboard?.ParticipationSummary.TotalIndexingLinks ?? 0):N0} indexaciones · {(dashboard?.ParticipationSummary.ByFaculty.Count ?? 0):N0} facultades";

    public static string GetPediIiitScopeLabel(InstitutionalReportingDashboardDto? dashboard)
        => $"{CountUniqueIndexedArticles(dashboard?.PediIiitArticles):N0} artículos únicos · {(dashboard?.PediIiitArticles.Count ?? 0):N0} filas reportables";

    public static string GetTddTotalScopeLabel(InstitutionalReportingDashboardDto? dashboard)
        => $"{CountUniqueIndexedArticles(dashboard?.TddTotalArticles):N0} artículos únicos · {(dashboard?.TddTotalArticles.Count ?? 0):N0} filas reportables";

    public static string GetParticipationScopeLabel(InstitutionalReportingDashboardDto? dashboard)
        => $"{(dashboard?.ParticipationSummary.TotalArticles ?? 0):N0} artículos · {(dashboard?.ParticipationSummary.TotalIndexingLinks ?? 0):N0} vínculos";

    private static int CountUniqueIndexedArticles(IEnumerable<ReportingArticleIndexingDetailDto>? articles)
        => articles?
            .Select(x => x.ArticleId)
            .Where(x => x > 0)
            .Distinct()
            .Count() ?? 0;
}
