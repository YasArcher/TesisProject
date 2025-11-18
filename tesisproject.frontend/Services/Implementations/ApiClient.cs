using System.Text;
using System.Text.Json;
using System.Net.Http;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http) => _http = http;

        // ==================== GET ====================
        public async Task<HttpResponseWrapper<T?>> GetAsync<T>(string url, CancellationToken ct = default)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                return await ParseResponseAsync<T>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<T?>(false, default, ex.Message,
                    new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== POST ====================
        public async Task<HttpResponseWrapper<TResponse?>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                var json = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var resp = await _http.PostAsync(url, json, ct);
                return await ParseResponseAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<TResponse?>(false, default, ex.Message,
                    new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== PUT ====================
        public async Task<HttpResponseWrapper<TResponse?>> PutAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                var json = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var resp = await _http.PutAsync(url, json, ct);
                return await ParseResponseAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<TResponse?>(false, default, ex.Message,
                    new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== PATCH ====================
        public async Task<HttpResponseWrapper<TResponse?>> PatchAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                var json = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = json
                };

                using var resp = await _http.SendAsync(request, ct);
                return await ParseResponseAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<TResponse?>(false, default, ex.Message,
                    new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== DELETE ====================
        public async Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var resp = await _http.DeleteAsync(url, ct);
                return await ParseResponseAsync<NoContent?>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<NoContent?>(false, default, ex.Message,
                    new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== CORE PARSER ====================
        private static async Task<HttpResponseWrapper<T?>> ParseResponseAsync<T>(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            var raw = await resp.Content.ReadAsStringAsync(ct);

            ApiResponse<T>? apiResponse;
            try
            {
                apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(raw, _jsonOptions);
            }
            catch
            {
                return new HttpResponseWrapper<T?>(false, default, $"Invalid server response: {raw}", resp);
            }

            if (apiResponse is null)
                return new HttpResponseWrapper<T?>(false, default, "Empty server response.", resp);

            if (resp.IsSuccessStatusCode && apiResponse.Success)
            {
                return new HttpResponseWrapper<T?>(true, apiResponse.Data, apiResponse.Message, resp);
            }

            return new HttpResponseWrapper<T?>(false, default,
                apiResponse.Message ?? $"HTTP {(int)resp.StatusCode}", resp);
        }
    }
}
