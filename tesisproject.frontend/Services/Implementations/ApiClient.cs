using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http) => _http = http;

        public async Task<HttpResponseWrapper<T?>> GetAsync<T>(string url, CancellationToken ct = default)
        {
            var res = await _http.GetAsync(url, ct);
            return await WrapAsync<T>(res, ct);
        }

        public async Task<HttpResponseWrapper<TResponse?>> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync(url, body, _json, ct);
            return await WrapAsync<TResponse>(res, ct);
        }

        public async Task<HttpResponseWrapper<TResponse?>> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync(url, body, _json, ct);
            return await WrapAsync<TResponse>(res, ct);
        }

        public async Task<HttpResponseWrapper<NoContent>> DeleteAsync(string url, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync(url, ct);
            // Normalmente DELETE no trae body utilizable → no intentamos deserializar
            if (res.IsSuccessStatusCode)
                return new HttpResponseWrapper<NoContent>(true, default, null, res);

            var error = await ReadErrorAsync(res, ct);
            return new HttpResponseWrapper<NoContent>(false, default, error, res);
        }

        // ======================
        // Helpers
        // ======================
        private static async Task<HttpResponseWrapper<T?>> WrapAsync<T>(HttpResponseMessage res, CancellationToken ct)
        {
            if (res.IsSuccessStatusCode)
            {
                // Si no hay contenido (204), devolvemos default(T)
                if (res.StatusCode == HttpStatusCode.NoContent || res.Content.Headers.ContentLength == 0)
                    return new HttpResponseWrapper<T?>(true, default, null, res);

                // Intentar deserializar body
                var data = await SafeReadFromJsonAsync<T>(res.Content, ct);
                return new HttpResponseWrapper<T?>(true, data, null, res);
            }

            var error = await ReadErrorAsync(res, ct);
            return new HttpResponseWrapper<T?>(false, default, error, res);
        }

        private static async Task<T?> SafeReadFromJsonAsync<T>(HttpContent content, CancellationToken ct)
        {
            try
            {
                return await content.ReadFromJsonAsync<T>(_json, ct);
            }
            catch
            {
                // Si el servidor devolvió algo que no es JSON del tipo esperado, retornamos default
                return default;
            }
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage res, CancellationToken ct)
        {
            // Intentamos leer ProblemDetails (RFC 7807) u otro JSON y formar un mensaje legible
            string raw = string.Empty;
            try
            {
                raw = await res.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(raw)) return $"HTTP {(int)res.StatusCode} - {res.ReasonPhrase}";

                // Intento parsear como JSON para extraer campos comunes
                var node = JsonNode.Parse(raw);
                if (node is JsonObject obj)
                {
                    var title = obj["title"]?.ToString();
                    var detail = obj["detail"]?.ToString();
                    var errors = obj["errors"] as JsonObject;

                    // Unimos errors (si vienen de FluentValidation / ModelState)
                    string? errorsJoin = null;
                    if (errors is not null)
                    {
                        var sb = new StringBuilder();
                        foreach (var kv in errors)
                        {
                            var arr = kv.Value?.AsArray();
                            if (arr is null) continue;
                            foreach (var item in arr)
                                sb.AppendLine($"{kv.Key}: {item}");
                        }
                        if (sb.Length > 0) errorsJoin = sb.ToString().Trim();
                    }

                    var parts = new[] { title, detail, errorsJoin, raw }
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    return string.Join(" | ", parts!);
                }

                // No era JSON → devolver texto crudo + status
                return $"HTTP {(int)res.StatusCode} - {res.ReasonPhrase} | {raw}";
            }
            catch
            {
                // Si algo sale mal al leer/parsear, al menos devolvemos status
                return $"HTTP {(int)res.StatusCode} - {res.ReasonPhrase} | {raw}";
            }
        }
    }
}