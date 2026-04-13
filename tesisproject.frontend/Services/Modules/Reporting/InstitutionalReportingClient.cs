using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class InstitutionalReportingClient : IInstitutionalReportingClient
    {
        private readonly IApiClient _apiClient;

        public InstitutionalReportingClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<ReportingHealthDto?> GetHealthAsync(CancellationToken ct = default)
        {
            return _apiClient.GetAsync<ReportingHealthDto>("api/reporting/health", ct);
        }

        public Task<InstitutionalReportingDashboardDto?> GetDashboardAsync(CancellationToken ct = default)
        {
            return _apiClient.GetAsync<InstitutionalReportingDashboardDto>("api/reporting/dashboard", ct);
        }

        public Task<ReportingHealthDto?> RunFullLoadAsync(CancellationToken ct = default)
        {
            return _apiClient.PostAsync<object, ReportingHealthDto>("api/reporting/etl/full", new { }, ct);
        }
    }
}
