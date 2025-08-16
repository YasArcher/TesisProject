using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http) => _http = http;

        public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        {
            var res = await _http.GetAsync(url, ct);
            await EnsureSuccess(res);
            return await res.Content.ReadFromJsonAsync<T>(_json, ct);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync(url, body, _json, ct);
            await EnsureSuccess(res);
            return await res.Content.ReadFromJsonAsync<TResponse>(_json, ct);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync(url, body, _json, ct);
            await EnsureSuccess(res);
            return await res.Content.ReadFromJsonAsync<TResponse>(_json, ct);
        }

        public async Task DeleteAsync(string url, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync(url, ct);
            await EnsureSuccess(res);
        }

        private static async Task EnsureSuccess(HttpResponseMessage res)
        {
            if (res.IsSuccessStatusCode) return;

            string payload = await res.Content.ReadAsStringAsync();

            // Mapear ProblemDetails
            throw new ApiException(res.StatusCode, payload);
        }
    }

    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ApiException(HttpStatusCode status, string? body)
            : base($"API Error {(int)status} - {status}: {Truncate(body)}")
        {
            StatusCode = status;
        }

        private static string Truncate(string? s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 800 ? s[..800] + "..." : s);
    }
}