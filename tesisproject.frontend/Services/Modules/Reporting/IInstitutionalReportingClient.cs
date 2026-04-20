using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IInstitutionalReportingClient
    {
        Task<ReportingHealthDto?> GetHealthAsync(CancellationToken ct = default);
        Task<InstitutionalReportingDashboardDto?> GetDashboardAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
        Task<AuthorReportingDashboardDto?> GetAuthorDashboardAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
        Task<ReportingHealthDto?> RunFullLoadAsync(CancellationToken ct = default);
    }
}
