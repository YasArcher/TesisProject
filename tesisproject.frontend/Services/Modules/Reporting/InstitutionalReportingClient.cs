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

        public Task<InstitutionalReportingDashboardDto?> GetDashboardAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default)
        {
            return _apiClient.GetAsync<InstitutionalReportingDashboardDto>(BuildDashboardUrl(filter), ct);
        }

        public Task<ReportingHealthDto?> RunFullLoadAsync(CancellationToken ct = default)
        {
            return _apiClient.PostAsync<object, ReportingHealthDto>("api/reporting/etl/full", new { }, ct);
        }

        public static string BuildDashboardUrl(InstitutionalReportingFilterDto? filter, string path = "api/reporting/dashboard")
        {
            if (filter is null)
            {
                return path;
            }

            var query = new List<string>();
            AddDate(query, nameof(filter.CreatedFrom), filter.CreatedFrom);
            AddDate(query, nameof(filter.CreatedTo), filter.CreatedTo);
            AddDate(query, nameof(filter.PublishedFrom), filter.PublishedFrom);
            AddDate(query, nameof(filter.PublishedTo), filter.PublishedTo);
            AddString(query, nameof(filter.AcademicTerm), filter.AcademicTerm);
            AddString(query, nameof(filter.PublicationStatus), filter.PublicationStatus);
            AddString(query, nameof(filter.ResearchLine), filter.ResearchLine);
            AddString(query, nameof(filter.Faculty), filter.Faculty);
            AddString(query, nameof(filter.IndexingSource), filter.IndexingSource);
            AddString(query, nameof(filter.BroadField), filter.BroadField);
            AddString(query, nameof(filter.SpecificField), filter.SpecificField);
            AddString(query, nameof(filter.DetailedField), filter.DetailedField);
            AddString(query, nameof(filter.VenueName), filter.VenueName);
            AddString(query, nameof(filter.VenueType), filter.VenueType);
            AddString(query, nameof(filter.Quartile), filter.Quartile);
            AddString(query, nameof(filter.PeriodDateType), filter.PeriodDateType);

            if (filter.ArticleYear.HasValue)
            {
                query.Add($"{nameof(filter.ArticleYear)}={filter.ArticleYear.Value}");
            }

            if (filter.IsOpenAccess.HasValue)
            {
                query.Add($"{nameof(filter.IsOpenAccess)}={filter.IsOpenAccess.Value.ToString().ToLowerInvariant()}");
            }

            return query.Count == 0 ? path : $"{path}?{string.Join("&", query)}";
        }

        private static void AddDate(List<string> query, string name, DateTime? value)
        {
            if (value.HasValue)
            {
                query.Add($"{name}={Uri.EscapeDataString(value.Value.ToString("yyyy-MM-dd"))}");
            }
        }

        private static void AddString(List<string> query, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
            }
        }
    }
}
