using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.frontend.Services.Errors;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public ApiClient(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            // (Opcional) Asegurar Accept JSON:
            if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
                _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        {
            using var res = await _http.GetAsync(url, ct);
            await EnsureSuccess(res, ct);
            return await ReadJsonOrDefaultAsync<T>(res.Content, ct);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            using var res = await _http.PostAsJsonAsync(url, body!, _json, ct);
            await EnsureSuccess(res, ct);
            return await ReadJsonOrDefaultAsync<TResponse>(res.Content, ct);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            using var res = await _http.PutAsJsonAsync(url, body!, _json, ct);
            await EnsureSuccess(res, ct);
            return await ReadJsonOrDefaultAsync<TResponse>(res.Content, ct);
        }

        // Útil para endpoints que devuelven 204/empty body
        public async Task DeleteAsync(string url, CancellationToken ct = default)
        {
            using var res = await _http.DeleteAsync(url, ct);
            await EnsureSuccess(res, ct);
        }

        // ---- Helpers ----

        private static async Task<T?> ReadJsonOrDefaultAsync<T>(HttpContent content, CancellationToken ct)
        {
            if (content is null) return default;

            var payload = await content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(payload, _json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo deserializar la respuesta de la API como {typeof(T).Name}. Body: {TruncatePayload(payload)}",
                    ex);
            }
        }

        private static async Task EnsureSuccess(HttpResponseMessage res, CancellationToken ct = default)
        {
            if (res.IsSuccessStatusCode) return;

            string body = "";
            try { body = await res.Content.ReadAsStringAsync(ct); } catch { /* ignore */ }

            // Intenta parsear ProblemDetails / ValidationProblemDetails-like
            ApiProblem? problem = null;
            try { problem = JsonSerializer.Deserialize<ApiProblem>(body, _json); } catch { /* ignore */ }

            throw new ApiException(res.StatusCode, body, problem);
        }

        private static string TruncatePayload(string? value) =>
            string.IsNullOrEmpty(value) ? "" : (value.Length > 800 ? value[..800] + "..." : value);
    }

    /// <summary>
    /// Excepción con información útil del error del backend.
    /// Intenta mapear el contrato de ProblemDetails (title, detail, errors).
    /// </summary>
    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? Title { get; }
        public string? Detail { get; }
        public IDictionary<string, string[]>? Errors { get; }
        public string? RawBody { get; }

        public ApiException(HttpStatusCode status, string? body, ApiProblem? problem)
            : base(UserFacingErrorMapper.FromRawHttpError(status, BuildMessage(status, problem, body), "No pude completar la operación. Revisa los datos e inténtalo nuevamente."))
        {
            StatusCode = status;
            Title = problem?.Title;
            Detail = problem?.Detail;
            Errors = problem?.Errors;
            RawBody = Truncate(body);
        }

        private static string BuildMessage(HttpStatusCode status, ApiProblem? p, string? body)
        {
            var sb = new StringBuilder($"API Error {(int)status} - {status}");
            if (!string.IsNullOrWhiteSpace(p?.Title)) sb.Append($": {p!.Title}");
            if (!string.IsNullOrWhiteSpace(p?.Detail)) sb.Append($" | {p!.Detail}");
            if (p?.Errors is { Count: > 0 })
            {
                var first = p.Errors.First();
                sb.Append($" | {first.Key}: {string.Join(", ", first.Value)}");
            }
            else if (!string.IsNullOrWhiteSpace(body))
            {
                sb.Append($" | Body: {Truncate(body)}");
            }
            return sb.ToString();
        }

        private static string Truncate(string? s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 800 ? s[..800] + "..." : s);
    }

    /// <summary>
    /// Contrato flexible para problem+json (ProblemDetails/ValidationProblemDetails).
    /// </summary>
    public sealed class ApiProblem
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
