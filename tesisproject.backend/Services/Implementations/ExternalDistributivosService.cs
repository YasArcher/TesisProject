using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
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

        private const string MsgNoDistributivos = "No distributivos found.";
        private const string MsgUnexpected = "Unexpected error.";
        private const string MsgRetrieved = "Distributivos retrieved.";
        private const string MsgConfigEndpointMissing = "External API misconfiguration: DistributivosEndpoint is missing.";
        private const string MsgConfigParamMissing = "External API misconfiguration: query parameter name is missing.";
        private const string MsgInvalidDistributivoId = "Invalid distributivoId.";
        private const string MsgExternalApiError = "External API error.";
        private const string MsgDistributivoNotFound = "Distributivo not found.";
        private const string MsgDistributivoRetrieved = "Distributivo retrieved.";
        private const string MsgUnauthorizedExternalApi = "Unauthorized external API.";
        private const string MsgForbiddenExternalApi = "Forbidden external API.";
        private const string MsgCedulasRequired = "Cedulas are required.";
        private const string MsgNoDistributivosForCedulas = "No distributivos found for the specified cedulas.";
        private const string MsgCorreosRequired = "Correos are required.";
        private const string MsgNoDistributivosForCorreos = "No distributivos found for the specified correos.";
        private const string MsgPeriodosRequired = "Periodos are required.";
        private const string MsgNoDistributivosForPeriodos = "No distributivos found for the specified periodos.";
        private const string MsgFacultadesRequired = "Facultades are required.";
        private const string MsgNoDistributivosForFacultades = "No distributivos found for the specified facultades.";

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

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosAsync(CancellationToken ct = default)
        {
            var cfgFail = ValidateConfig(out var endpoint);
            if (cfgFail is not null) return cfgFail;

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
                requiredMessage: MsgCedulasRequired,
                notFoundMessage: MsgNoDistributivosForCedulas,
                logContext: "ByCedulas",
                normalize: s => s, // tal cual
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCorreosAsync(
            IEnumerable<string> correos,
            CancellationToken ct = default)
            => QueryByAsync(
                values: correos,
                queryParamName: _opts.DistributivosCorreosQueryParam,
                requiredMessage: MsgCorreosRequired,
                notFoundMessage: MsgNoDistributivosForCorreos,
                logContext: "ByCorreos",
                normalize: s => s.ToLowerInvariant(), // si el API lo requiere
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByPeriodosAsync(
            IEnumerable<string> periodos,
            CancellationToken ct = default)
            => QueryByAsync(
                values: periodos,
                queryParamName: _opts.DistributivosPeriodosQueryParam,
                requiredMessage: MsgPeriodosRequired,
                notFoundMessage: MsgNoDistributivosForPeriodos,
                logContext: "ByPeriodos",
                normalize: s => s,
                ct: ct);

        public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByFacultadesAsync(
            IEnumerable<string> facultades,
            CancellationToken ct = default)
            => QueryByAsync(
                values: facultades,
                queryParamName: _opts.DistributivosFacultadesQueryParam,
                requiredMessage: MsgFacultadesRequired,
                notFoundMessage: MsgNoDistributivosForFacultades,
                logContext: "ByFacultades",
                normalize: s => s,
                ct: ct);

        public async Task<ServiceResult<ExternalTeacherDistributivoModel>> GetDistributivoByIdAsync(int distributivoId, CancellationToken ct = default)
        {
            if (distributivoId <= 0)
                return ServiceResult<ExternalTeacherDistributivoModel>.Fail(MsgInvalidDistributivoId, ErrorType.Validation);

            try
            {
                // Fallback mientras Node no tenga /api/distributivos/:id
                var all = await GetDistributivosAsync(ct);
                if (!all.Success || all.Data is null)
                    return ServiceResult<ExternalTeacherDistributivoModel>.Fail(all.Message ?? MsgExternalApiError, all.Error);

                var item = all.Data.FirstOrDefault(x => x.DistributivoId == distributivoId);

                return item is null
                    ? ServiceResult<ExternalTeacherDistributivoModel>.Fail(MsgDistributivoNotFound, ErrorType.NotFound)
                    : ServiceResult<ExternalTeacherDistributivoModel>.Ok(item, MsgDistributivoRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivo {DistributivoId}", distributivoId);
                return ServiceResult<ExternalTeacherDistributivoModel>.Fail(MsgUnexpected, ErrorType.Unexpected);
            }
        }

        // ================= CORE (DRY) =================

        private async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> QueryByAsync(
            IEnumerable<string> values,
            string queryParamName,
            string requiredMessage,
            string notFoundMessage,
            string logContext,
            Func<string, string> normalize,
            CancellationToken ct)
        {
            var list = NormalizeList(values, normalize);
            if (list.Count == 0)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(requiredMessage, ErrorType.Validation);

            var cfgFail = ValidateConfig(out var endpoint, queryParamName);
            if (cfgFail is not null) return cfgFail;

            // IMPORTANTE: este valor va como CSV. BuildUrl preserva comas para no convertirlas en %2C.
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

            // Personaliza el mensaje de NotFound para cada filtro
            if (!res.Success && res.Error == ErrorType.NotFound)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(notFoundMessage, ErrorType.NotFound);

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
                    return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgNoDistributivos, ErrorType.NotFound);

                _logger.LogInformation("Distributivos {Context}: retrieved {Count}.", logContext, list.Count);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(list, successMessage);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 404", logContext);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgNoDistributivos, ErrorType.NotFound);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 401", logContext);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgUnauthorizedExternalApi, ErrorType.Unauthorized);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "Distributivos {Context}: 403", logContext);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgForbiddenExternalApi, ErrorType.Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Distributivos {Context}: unexpected error", logContext);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgUnexpected, ErrorType.Unexpected);
            }
        }

        private ServiceResult<List<ExternalTeacherDistributivoModel>>? ValidateConfig(out string endpoint, string? queryParamName = null)
        {
            endpoint = _opts.DistributivosEndpoint ?? string.Empty;

            if (string.IsNullOrWhiteSpace(endpoint))
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgConfigEndpointMissing, ErrorType.Unexpected);

            if (queryParamName is not null && string.IsNullOrWhiteSpace(queryParamName))
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail(MsgConfigParamMissing, ErrorType.Unexpected);

            return null;
        }

        // ================= HELPERS =================

        private static string BuildUrl(string endpoint, IReadOnlyDictionary<string, string>? query)
        {
            if (query is null || query.Count == 0)
                return endpoint;

            var sb = new StringBuilder(endpoint);

            // Si el endpoint ya trae querystring, agrega con '&'
            sb.Append(endpoint.Contains('?') ? '&' : '?');

            var first = true;
            foreach (var kv in query)
            {
                if (!first) sb.Append('&');
                first = false;

                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append('=');

                // CLAVE: preserva comas en valores CSV (evita %2C)
                sb.Append(EscapeValuePreserveCommas(kv.Value));
            }

            return sb.ToString();
        }

        private static string EscapeValuePreserveCommas(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // Escape normal, pero vuelve a dejar comas como comas (para CSV)
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
    }
}
