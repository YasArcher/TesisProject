using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
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

        // ===== Strings (mismo texto exacto que antes) =====
        private const string MsgNoExternalPeriodsFound = "No external periods found.";
        private const string MsgUnexpectedError = "Unexpected error.";
        private const string MsgAllExternalPeriodsRetrieved = "All external periods retrieved";
        private const string MsgExternalPeriodsRetrievedByNames = "External periods retrieved by names";
        private const string MsgAtLeastOnePeriodNameRequired = "At least one period name is required.";
        private const string MsgValidPeriodIdRequired = "A valid period id is required.";
        private const string MsgExternalPeriodNotFound = "External period not found.";
        private const string MsgExternalPeriodRetrievedById = "External period retrieved by id";

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

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var api = await FetchPeriodsListAsync(_opts.PeriodsEndpoint, ct);

                if (api is null || api.Count == 0)
                    return FailNoPeriodsFound();

                _logger.LogInformation("Retrieved {Count} periods.", api.Count);
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(
                    api,
                    MsgAllExternalPeriodsRetrieved);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 on GetAll");
                return FailNoPeriodsFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external periods");
                return FailUnexpected();
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetByNamesAsync(
            IEnumerable<string> names,
            CancellationToken ct = default)
        {
            var list = NormalizeDistinct(names);
            if (list.Count == 0)
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Fail(
                    MsgAtLeastOnePeriodNameRequired,
                    ErrorType.Validation);

            try
            {
                // Node controller: GET /api/periodos?nombres=a,b,c
                var url = BuildBatchUrl(_opts.PeriodsEndpoint, _opts.PeriodsNamesQueryParam, list);

                var api = await FetchPeriodsListAsync(url, ct);

                if (api is null || api.Count == 0)
                    return FailNoPeriodsFound();

                _logger.LogInformation("Retrieved {Count} periods by names.", api.Count);
                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(
                    api,
                    MsgExternalPeriodsRetrievedByNames);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for names query");
                return FailNoPeriodsFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error querying external periods by names");
                return FailUnexpected();
            }
        }

        public async Task<ServiceResult<ExternalAcademicPeriodModel>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ExternalAcademicPeriodModel>.Fail(
                    MsgValidPeriodIdRequired,
                    ErrorType.Validation);

            try
            {
                var endpoint = $"{_opts.PeriodsEndpoint.TrimEnd('/')}/{id}";
                var api = await _http.GetFromJsonAsync<ExternalAcademicPeriodModel>(endpoint, _jsonOpts, ct);

                if (api is null)
                    return ServiceResult<ExternalAcademicPeriodModel>.Fail(
                        MsgExternalPeriodNotFound,
                        ErrorType.NotFound);

                _logger.LogInformation("Retrieved period {Id}.", id);
                return ServiceResult<ExternalAcademicPeriodModel>.Ok(
                    api,
                    MsgExternalPeriodRetrievedById);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for id {Id}", id);
                return ServiceResult<ExternalAcademicPeriodModel>.Fail(
                    MsgExternalPeriodNotFound,
                    ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external period by id {Id}", id);
                return ServiceResult<ExternalAcademicPeriodModel>.Fail(
                    MsgUnexpectedError,
                    ErrorType.Unexpected);
            }
        }

        // ============== Helpers ==============

        private Task<List<ExternalAcademicPeriodModel>?> FetchPeriodsListAsync(string url, CancellationToken ct)
            => _http.GetFromJsonAsync<List<ExternalAcademicPeriodModel>>(url, _jsonOpts, ct);

        private static ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>> FailNoPeriodsFound()
            => ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Fail(
                MsgNoExternalPeriodsFound,
                ErrorType.NotFound);

        private static ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>> FailUnexpected()
            => ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Fail(
                MsgUnexpectedError,
                ErrorType.Unexpected);

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