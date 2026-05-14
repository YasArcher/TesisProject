using tesisproject.frontend.Services.Implementations;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Components;

public static class ReportPdfRequestBuilder
{
    public const string DashboardPdfEndpoint = "api/reporting/dashboard/pdf";

    public static InstitutionalReportingFilterDto CreateDefaultSelection()
        => new();

    public static void ApplySelection(
        InstitutionalReportingFilterDto target,
        InstitutionalReportingFilterDto selection)
    {
        target.IncludePdfKpis = selection.IncludePdfKpis;
        target.IncludePdfFilters = selection.IncludePdfFilters;
        target.IncludePdfCharts = selection.IncludePdfCharts;
        target.IncludePdfPeriod = selection.IncludePdfPeriod;
        target.IncludePdfFields = selection.IncludePdfFields;
        target.IncludePdfVenues = selection.IncludePdfVenues;
        target.IncludePdfAuthors = selection.IncludePdfAuthors;
        target.IncludePdfPediIiit = selection.IncludePdfPediIiit;
        target.IncludePdfTddTotal = selection.IncludePdfTddTotal;
        target.IncludePdfParticipation = selection.IncludePdfParticipation;
        target.IncludePdfArticles = selection.IncludePdfArticles;
    }

    public static bool HasAnySectionSelected(InstitutionalReportingFilterDto selection)
        => selection.IncludePdfKpis
        || selection.IncludePdfFilters
        || selection.IncludePdfCharts
        || selection.IncludePdfPeriod
        || selection.IncludePdfFields
        || selection.IncludePdfVenues
        || selection.IncludePdfAuthors
        || selection.IncludePdfPediIiit
        || selection.IncludePdfTddTotal
        || selection.IncludePdfParticipation
        || selection.IncludePdfArticles;

    public static string BuildDashboardPdfUrl(InstitutionalReportingFilterDto filter)
        => InstitutionalReportingClient.BuildDashboardUrl(
            filter,
            DashboardPdfEndpoint,
            includePdfOptions: true);
}
