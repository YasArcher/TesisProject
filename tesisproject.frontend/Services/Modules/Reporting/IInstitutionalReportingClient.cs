using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IInstitutionalReportingClient
    {
        Task<ReportingHealthDto?> GetHealthAsync(CancellationToken ct = default);
        Task<InstitutionalReportingDashboardDto?> GetDashboardAsync(CancellationToken ct = default);
        Task<ReportingHealthDto?> RunFullLoadAsync(CancellationToken ct = default);
    }
}
