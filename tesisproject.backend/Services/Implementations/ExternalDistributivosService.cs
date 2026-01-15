using Microsoft.Extensions.Options;
using System.Net;
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
            try
            {
                var raw = await FetchRawAsync(endpoint: _opts.DistributivosEndpoint, query: null, ct);

                return raw.Count == 0
                    ? ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(raw, "Distributivos retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "External endpoint {Endpoint} returned 404", _opts.DistributivosEndpoint);
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Distributivos not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivos");
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCedulasAsync(IEnumerable<string> cedulas, CancellationToken ct = default)
        {
            var list = NormalizeList(cedulas);
            if (list.Count == 0)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Cedulas are required.", ErrorType.Validation);

            try
            {
                var query = new Dictionary<string, string>
                {
                    [_opts.DistributivosCedulasQueryParam] = string.Join(',', list)
                };

                var raw = await FetchRawAsync(_opts.DistributivosEndpoint, query, ct);

                return raw.Count == 0
                    ? ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found for the specified cedulas.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(raw, "Distributivos retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivos by cedulas");
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCorreosAsync(IEnumerable<string> correos, CancellationToken ct = default)
        {
            var list = NormalizeList(correos)
                .Select(x => x.ToLowerInvariant())
                .ToList();

            if (list.Count == 0)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Correos are required.", ErrorType.Validation);

            try
            {
                var query = new Dictionary<string, string>
                {
                    [_opts.DistributivosCorreosQueryParam] = string.Join(',', list)
                };

                var raw = await FetchRawAsync(_opts.DistributivosEndpoint, query, ct);

                return raw.Count == 0
                    ? ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found for the specified correos.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(raw, "Distributivos retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivos by correos");
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByPeriodosAsync(IEnumerable<string> periodos, CancellationToken ct = default)
        {
            var list = NormalizeList(periodos);
            if (list.Count == 0)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Periodos are required.", ErrorType.Validation);

            try
            {
                var query = new Dictionary<string, string>
                {
                    [_opts.DistributivosPeriodosQueryParam] = string.Join(',', list)
                };

                var raw = await FetchRawAsync(_opts.DistributivosEndpoint, query, ct);

                return raw.Count == 0
                    ? ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found for the specified periodos.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(raw, "Distributivos retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivos by periodos");
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByFacultadesAsync(IEnumerable<string> facultades, CancellationToken ct = default)
        {
            var list = NormalizeList(facultades);
            if (list.Count == 0)
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Facultades are required.", ErrorType.Validation);

            try
            {
                var query = new Dictionary<string, string>
                {
                    [_opts.DistributivosFacultadesQueryParam] = string.Join(',', list)
                };

                var raw = await FetchRawAsync(_opts.DistributivosEndpoint, query, ct);

                return raw.Count == 0
                    ? ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found for the specified facultades.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(raw, "Distributivos retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("No distributivos found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivos by facultades");
                return ServiceResult<List<ExternalTeacherDistributivoModel>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalTeacherDistributivoModel>> GetDistributivoByIdAsync(int distributivoId, CancellationToken ct = default)
        {
            if (distributivoId <= 0)
                return ServiceResult<ExternalTeacherDistributivoModel>.Fail("Invalid distributivoId.", ErrorType.Validation);

            try
            {
                // Fallback mientras Node no tenga /api/distributivos/:id
                var all = await GetDistributivosAsync(ct);
                if (!all.Success || all.Data is null)
                    return ServiceResult<ExternalTeacherDistributivoModel>.Fail("API error.", ErrorType.Unexpected);

                var item = all.Data.FirstOrDefault(x => x.DistributivoId == distributivoId);

                return item is null
                    ? ServiceResult<ExternalTeacherDistributivoModel>.Fail("Distributivo not found.", ErrorType.NotFound)
                    : ServiceResult<ExternalTeacherDistributivoModel>.Ok(item, "Distributivo retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving distributivo {DistributivoId}", distributivoId);
                return ServiceResult<ExternalTeacherDistributivoModel>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        // ================= PRIVATE HELPERS =================

        private async Task<List<ExternalTeacherDistributivoModel>> FetchRawAsync(
            string endpoint,
            IReadOnlyDictionary<string, string>? query,
            CancellationToken ct)
        {
            var url = BuildUrl(endpoint, query);

            var list = await _http.GetFromJsonAsync<List<ExternalTeacherDistributivoModel>>(url, _jsonOpts, ct);
            return list ?? new List<ExternalTeacherDistributivoModel>();
        }

        private static string BuildUrl(string endpoint, IReadOnlyDictionary<string, string>? query)
        {
            if (query is null || query.Count == 0)
                return endpoint;

            var sb = new StringBuilder();
            sb.Append(endpoint);
            sb.Append('?');

            var first = true;
            foreach (var kv in query)
            {
                if (!first) sb.Append('&');
                first = false;

                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value));
            }

            return sb.ToString();
        }

        private static List<string> NormalizeList(IEnumerable<string>? input)
        {
            return input?
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }
    }
}
