using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
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

        private const string MsgAllExternalPeriodsRetrieved = "All external periods retrieved";
        private const string MsgExternalPeriodsRetrievedByNames = "External periods retrieved by names";
        private const string MsgExternalPeriodRetrievedById = "External period retrieved by id";

        public ExternalPeriodsClient(
            HttpClient http,
            IOptions<ExternalApiOptions> opts,
            ILogger<ExternalPeriodsClient> logger)
        {
            _http = http;
            _logger = logger;
            _opts = opts.Value;

            if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
                _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetAllAsync(
            CancellationToken ct = default)
        {
            var cfgFail = ValidateConfig<IReadOnlyList<ExternalAcademicPeriodModel>>(out var endpoint);
            if (cfgFail is not null)
                return cfgFail;

            try
            {
                var api = await FetchPeriodsListAsync(endpoint, ct);

                if (api is null || api.Count == 0)
                    return NoExternalPeriodsFound();

                _logger.LogInformation("Retrieved {Count} periods.", api.Count);

                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(
                    api,
                    MsgAllExternalPeriodsRetrieved);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 on GetAll");
                return NoExternalPeriodsFound();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 401 on GetAll");
                return UnauthorizedExternalApi<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 403 on GetAll");
                return ForbiddenExternalApi<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external periods");
                return Unexpected<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetByNamesAsync(
            IEnumerable<string> names,
            CancellationToken ct = default)
        {
            var list = NormalizeDistinct(names);
            if (list.Count == 0)
                return AtLeastOnePeriodNameRequired();

            var cfgFail = ValidateConfig<IReadOnlyList<ExternalAcademicPeriodModel>>(
                out var endpoint,
                _opts.PeriodsNamesQueryParam);

            if (cfgFail is not null)
                return cfgFail;

            try
            {
                var url = BuildBatchUrl(endpoint, _opts.PeriodsNamesQueryParam!, list);
                var api = await FetchPeriodsListAsync(url, ct);

                if (api is null || api.Count == 0)
                    return NoExternalPeriodsFound();

                _logger.LogInformation("Retrieved {Count} periods by names.", api.Count);

                return ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(
                    api,
                    MsgExternalPeriodsRetrievedByNames);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for names query");
                return NoExternalPeriodsFound();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 401 for names query");
                return UnauthorizedExternalApi<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 403 for names query");
                return ForbiddenExternalApi<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error querying external periods by names");
                return Unexpected<IReadOnlyList<ExternalAcademicPeriodModel>>();
            }
        }

        public async Task<ServiceResult<ExternalAcademicPeriodModel>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ValidPeriodIdRequired();

            var cfgFail = ValidateConfig<ExternalAcademicPeriodModel>(out var endpoint);
            if (cfgFail is not null)
                return cfgFail;

            try
            {
                var itemEndpoint = $"{endpoint.TrimEnd('/')}/{id}";
                var api = await _http.GetFromJsonAsync<ExternalAcademicPeriodModel>(itemEndpoint, _jsonOpts, ct);

                if (api is null)
                    return ExternalPeriodNotFound();

                _logger.LogInformation("Retrieved period {Id}.", id);

                return ServiceResult<ExternalAcademicPeriodModel>.Ok(
                    api,
                    MsgExternalPeriodRetrievedById);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 404 for id {Id}", id);
                return ExternalPeriodNotFound();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 401 for id {Id}", id);
                return UnauthorizedExternalApi<ExternalAcademicPeriodModel>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "Periods endpoint returned 403 for id {Id}", id);
                return ForbiddenExternalApi<ExternalAcademicPeriodModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external period by id {Id}", id);
                return Unexpected<ExternalAcademicPeriodModel>();
            }
        }

        // ============== Helpers ==============

        private Task<List<ExternalAcademicPeriodModel>?> FetchPeriodsListAsync(string url, CancellationToken ct)
            => _http.GetFromJsonAsync<List<ExternalAcademicPeriodModel>>(url, _jsonOpts, ct);

        private ServiceResult<T>? ValidateConfig<T>(out string endpoint, string? queryParamName = null)
        {
            endpoint = _opts.PeriodsEndpoint ?? string.Empty;

            if (string.IsNullOrWhiteSpace(endpoint))
                return ConfigEndpointMissing<T>();

            if (queryParamName is not null && string.IsNullOrWhiteSpace(queryParamName))
                return ConfigParamMissing<T>();

            return null;
        }

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

        // ============== Error helpers ==============

        private static ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>> NoExternalPeriodsFound()
            => ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Fail(
                ErrorMessages.ExternalPeriods.NoExternalPeriodsFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalPeriods.NoExternalPeriodsFound);

        private static ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>> AtLeastOnePeriodNameRequired()
            => ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Fail(
                ErrorMessages.ExternalPeriods.AtLeastOnePeriodNameRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalPeriods.AtLeastOnePeriodNameRequired);

        private static ServiceResult<ExternalAcademicPeriodModel> ValidPeriodIdRequired()
            => ServiceResult<ExternalAcademicPeriodModel>.Fail(
                ErrorMessages.ExternalPeriods.ValidPeriodIdRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalPeriods.ValidPeriodIdRequired);

        private static ServiceResult<ExternalAcademicPeriodModel> ExternalPeriodNotFound()
            => ServiceResult<ExternalAcademicPeriodModel>.Fail(
                ErrorMessages.ExternalPeriods.ExternalPeriodNotFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalPeriods.ExternalPeriodNotFound);

        private static ServiceResult<T> ConfigEndpointMissing<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalPeriods.ConfigEndpointMissing,
                ErrorType.Unexpected,
                ErrorCodes.ExternalPeriods.ConfigEndpointMissing);

        private static ServiceResult<T> ConfigParamMissing<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalPeriods.ConfigParamMissing,
                ErrorType.Unexpected,
                ErrorCodes.ExternalPeriods.ConfigParamMissing);

        private static ServiceResult<T> UnauthorizedExternalApi<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalPeriods.UnauthorizedExternalApi,
                ErrorType.Unauthorized,
                ErrorCodes.ExternalPeriods.UnauthorizedExternalApi);

        private static ServiceResult<T> ForbiddenExternalApi<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalPeriods.ForbiddenExternalApi,
                ErrorType.Forbidden,
                ErrorCodes.ExternalPeriods.ForbiddenExternalApi);

        private static ServiceResult<T> Unexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);
    }
}