using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ExternalPeriodsClient : IExternalPeriodsClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalPeriodsClient> _logger;
        private readonly ExternalApiOptions _opts;

        private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public ExternalPeriodsClient(
            HttpClient http,
            IOptions<ExternalApiOptions> opts,
            ILogger<ExternalPeriodsClient> logger)
        {
            _http = http;
            _logger = logger;
            _opts = opts.Value;

            // Safety: allow setting BaseAddress via options if named client didn't set it.
            if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
                _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var api = await _http.GetFromJsonAsync<List<ExternalAcademicPeriodDTO>>(
                    _opts.PeriodsEndpoint,
                    _jsonOpts,
                    ct);

                if (api is null || api.Count == 0)
                    return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                        "No external periods found.",
                        ErrorType.NotFound);

                _logger.LogInformation("Retrieved {Count} periods.", api.Count);
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Ok(
                    api,
                    "All external periods retrieved");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 on GetAll");
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                    "No external periods found.",
                    ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external periods");
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                    "Unexpected error.",
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>> GetByNamesAsync(
            IEnumerable<string> names,
            CancellationToken ct = default)
        {
            var list = NormalizeDistinct(names);
            if (list.Count == 0)
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                    "At least one period name is required.",
                    ErrorType.Validation);

            try
            {
                // Node controller: GET /api/periodos?nombres=a,b,c
                var url = BuildBatchUrl(_opts.PeriodsEndpoint, _opts.PeriodsNamesQueryParam, list);

                var api = await _http.GetFromJsonAsync<List<ExternalAcademicPeriodDTO>>(url, _jsonOpts, ct);

                if (api is null || api.Count == 0)
                    return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                        "No external periods found.",
                        ErrorType.NotFound);

                _logger.LogInformation("Retrieved {Count} periods by names.", api.Count);
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Ok(
                    api,
                    "External periods retrieved by names");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for names query");
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                    "No external periods found.",
                    ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error querying external periods by names");
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>.Fail(
                    "Unexpected error.",
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalAcademicPeriodDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ExternalAcademicPeriodDTO>.Fail(
                    "A valid period id is required.",
                    ErrorType.Validation);

            try
            {
                var endpoint = $"{_opts.PeriodsEndpoint.TrimEnd('/')}/{id}";
                var api = await _http.GetFromJsonAsync<ExternalAcademicPeriodDTO>(endpoint, _jsonOpts, ct);

                if (api is null)
                    return ServiceResult<ExternalAcademicPeriodDTO>.Fail(
                        "External period not found.",
                        ErrorType.NotFound);

                _logger.LogInformation("Retrieved period {Id}.", id);
                return ServiceResult<ExternalAcademicPeriodDTO>.Ok(
                    api,
                    "External period retrieved by id");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for id {Id}", id);
                return ServiceResult<ExternalAcademicPeriodDTO>.Fail(
                    "External period not found.",
                    ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external period by id {Id}", id);
                return ServiceResult<ExternalAcademicPeriodDTO>.Fail(
                    "Unexpected error.",
                    ErrorType.Unexpected);
            }
        }

        // ============== Helpers ==============

        private static List<string> NormalizeDistinct(IEnumerable<string> source) =>
            source?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            ?? new List<string>();

        private static string BuildBatchUrl(string baseEndpoint, string queryParamName, IReadOnlyList<string> values)
        {
            var joined = string.Join(",", values.Select(Uri.EscapeDataString));
            var sep = baseEndpoint.Contains('?') ? "&" : "?";
            return $"{baseEndpoint}{sep}{queryParamName}={joined}";
        }
    }
}
