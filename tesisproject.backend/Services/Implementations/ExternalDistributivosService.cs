using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ExternalDistributivosService : IExternalDistributivosService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalDistributivosService> _logger;
        private readonly ExternalApiOptions _opts;

        private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private const string MsgRetrieved = "Distributivos retrieved.";
        private const string MsgDistributivoRetrieved = "Distributivo retrieved.";

        public ExternalDistributivosService(
            HttpClient http,
            IOptions<ExternalApiOptions> opts,
            ILogger<ExternalDistributivosService> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;

            if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
                _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        // ================= PUBLIC =================

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosAsync(
            CancellationToken ct = default)
        {
            var cfgFail = ValidateConfig(out var endpoint);
            if (cfgFail is not null)
                return cfgFail;

            return await FetchAsync(
                url: endpoint,
                successMessage: MsgRetrieved,
                logContext: "GetAll",
                ct: ct);
        }

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCedulasAsync(
            IEnumerable<string> cedulas,
            CancellationToken ct = default)
            => QueryByAsync(
                values: cedulas,
                queryParamName: _opts.DistributivosCedulasQueryParam,
                requiredFailureFactory: CedulasRequired,
                notFoundFailureFactory: NoDistributivosForCedulas,
                logContext: "ByCedulas",
                normalize: s => s,
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCorreosAsync(
            IEnumerable<string> correos,
            CancellationToken ct = default)
            => QueryByAsync(
                values: correos,
                queryParamName: _opts.DistributivosCorreosQueryParam,
                requiredFailureFactory: CorreosRequired,
                notFoundFailureFactory: NoDistributivosForCorreos,
                logContext: "ByCorreos",
                normalize: s => s.ToLowerInvariant(),
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByPeriodosAsync(
            IEnumerable<string> periodos,
            CancellationToken ct = default)
            => QueryByAsync(
                values: periodos,
                queryParamName: _opts.DistributivosPeriodosQueryParam,
                requiredFailureFactory: PeriodosRequired,
                notFoundFailureFactory: NoDistributivosForPeriodos,
                logContext: "ByPeriodos",
                normalize: s => s,
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByFacultadesAsync(
            IEnumerable<string> facultades,
            CancellationToken ct = default)
            => QueryByAsync(
                values: facultades,
                queryParamName: _opts.DistributivosFacultadesQueryParam,
                requiredFailureFactory: FacultadesRequired,
                notFoundFailureFactory: NoDistributivosForFacultades,
                logContext: "ByFacultades",
                normalize: s => s,
                ct: ct);

        public async Task<ServiceResult<ExternalTeacherDistributivoModel>> GetDistributivoByIdAsync(
            int distributivoId,
            CancellationToken ct = default)
        {
            if (distributivoId <= 0)
                return InvalidDistributivoId();

            try
            {
                // Fallback mientras Node no tenga /api/distributivos/:id
                var all = await GetDistributivosAsync(ct);
                if (!all.Success || all.Data is null)
                    return RelayFailure<ExternalTeacherDistributivoModel, List<ExternalTeacherDistributivoModel>>(all);

                var item = all.Data.FirstOrDefault(x => x.DistributivoId == distributivoId);

                return item is null
                    ? DistributivoNotFound()
                    : ServiceResult<ExternalTeacherDistributivoModel>.Ok(item, MsgDistributivoRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivo {DistributivoId}", distributivoId);
                return UnexpectedSingle();
            }
        }

        // ================= CORE (DRY) =================

        private async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> QueryByAsync(
            IEnumerable<string> values,
            string queryParamName,
            Func<ServiceResult<List<ExternalTeacherDistributivoModel>>> requiredFailureFactory,
            Func<ServiceResult<List<ExternalTeacherDistributivoModel>>> notFoundFailureFactory,
            string logContext,
            Func<string, string> normalize,
            CancellationToken ct)
        {
            var list = NormalizeList(values, normalize);
            if (list.Count == 0)
                return requiredFailureFactory();

            var cfgFail = ValidateConfig(out var endpoint, queryParamName);
            if (cfgFail is not null)
                return cfgFail;

            var query = new Dictionary<string, string>
            {
                [queryParamName] = string.Join(',', list)
            };

            var url = BuildUrl(endpoint, query);

            var res = await FetchAsync(
                url: url,
                successMessage: MsgRetrieved,
                logContext: logContext,
                ct: ct);

            if (!res.Success && res.Error == ErrorType.NotFound)
                return notFoundFailureFactory();

            return res;
        }

        private async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> FetchAsync(
            string url,
            string successMessage,
            string logContext,
            CancellationToken ct)
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<ExternalTeacherDistributivoModel>>(url, _jsonOpts, ct);

                if (list is null || list.Count == 0)
                    return NoDistributivosFound();

                _logger.LogInformation("Distributivos {Context}: retrieved {Count}.", logContext, list.Count);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(list, successMessage);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 404", logContext);
                return NoDistributivosFound();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 401", logContext);
                return UnauthorizedExternalApi();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 403", logContext);
                return ForbiddenExternalApi();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Distributivos {Context}: unexpected error", logContext);
                return UnexpectedList();
            }
        }

        private ServiceResult<List<ExternalTeacherDistributivoModel>>? ValidateConfig(
            out string endpoint,
            string? queryParamName = null)
        {
            endpoint = _opts.DistributivosEndpoint ?? string.Empty;

            if (string.IsNullOrWhiteSpace(endpoint))
                return ConfigEndpointMissing();

            if (queryParamName is not null && string.IsNullOrWhiteSpace(queryParamName))
                return ConfigParamMissing();

            return null;
        }

        // ================= HELPERS =================

        private static string BuildUrl(string endpoint, IReadOnlyDictionary<string, string>? query)
        {
            if (query is null || query.Count == 0)
                return endpoint;

            var sb = new StringBuilder(endpoint);
            sb.Append(endpoint.Contains('?') ? '&' : '?');

            var first = true;
            foreach (var kv in query)
            {
                if (!first)
                    sb.Append('&');

                first = false;

                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append('=');
                sb.Append(EscapeValuePreserveCommas(kv.Value));
            }

            return sb.ToString();
        }

        private static string EscapeValuePreserveCommas(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return Uri.EscapeDataString(value).Replace("%2C", ",");
        }

        private static List<string> NormalizeList(IEnumerable<string>? input, Func<string, string> normalize)
        {
            return input?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => normalize(x.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        // ================= ERROR HELPERS =================

        private static ServiceResult<TTarget> RelayFailure<TTarget, TSource>(ServiceResult<TSource> source)
        {
            var error = source.Error == ErrorType.None
                ? ErrorType.Unexpected
                : source.Error;

            return ServiceResult<TTarget>.Fail(
                source.Message ?? ErrorMessages.Common.UnexpectedError,
                error,
                source.ErrorCode ?? (error == ErrorType.Unexpected ? ErrorCodes.Common.UnexpectedError : null),
                source.ValidationErrors);
        }

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> NoDistributivosFound()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.NoDistributivosFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.NoDistributivosFound);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> ConfigEndpointMissing()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.ConfigEndpointMissing,
                ErrorType.Unexpected,
                ErrorCodes.ExternalDistributivos.ConfigEndpointMissing);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> ConfigParamMissing()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.ConfigParamMissing,
                ErrorType.Unexpected,
                ErrorCodes.ExternalDistributivos.ConfigParamMissing);

        private static ServiceResult<ExternalTeacherDistributivoModel> InvalidDistributivoId()
            => ServiceResult<ExternalTeacherDistributivoModel>.Fail(
                ErrorMessages.ExternalDistributivos.InvalidDistributivoId,
                ErrorType.Validation,
                ErrorCodes.ExternalDistributivos.InvalidDistributivoId);

        private static ServiceResult<ExternalTeacherDistributivoModel> DistributivoNotFound()
            => ServiceResult<ExternalTeacherDistributivoModel>.Fail(
                ErrorMessages.ExternalDistributivos.DistributivoNotFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.DistributivoNotFound);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> UnauthorizedExternalApi()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.UnauthorizedExternalApi,
                ErrorType.Unauthorized,
                ErrorCodes.ExternalDistributivos.UnauthorizedExternalApi);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> ForbiddenExternalApi()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.ForbiddenExternalApi,
                ErrorType.Forbidden,
                ErrorCodes.ExternalDistributivos.ForbiddenExternalApi);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> CedulasRequired()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.CedulasRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalDistributivos.CedulasRequired);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> NoDistributivosForCedulas()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.NoDistributivosForCedulas,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.NoDistributivosForCedulas);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> CorreosRequired()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.CorreosRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalDistributivos.CorreosRequired);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> NoDistributivosForCorreos()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.NoDistributivosForCorreos,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.NoDistributivosForCorreos);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> PeriodosRequired()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.PeriodosRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalDistributivos.PeriodosRequired);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> NoDistributivosForPeriodos()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.NoDistributivosForPeriodos,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.NoDistributivosForPeriodos);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> FacultadesRequired()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.FacultadesRequired,
                ErrorType.Validation,
                ErrorCodes.ExternalDistributivos.FacultadesRequired);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> NoDistributivosForFacultades()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.ExternalDistributivos.NoDistributivosForFacultades,
                ErrorType.NotFound,
                ErrorCodes.ExternalDistributivos.NoDistributivosForFacultades);

        private static ServiceResult<List<ExternalTeacherDistributivoModel>> UnexpectedList()
            => ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<ExternalTeacherDistributivoModel> UnexpectedSingle()
            => ServiceResult<ExternalTeacherDistributivoModel>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);
    }
}