using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
                return new HttpResponseWrapper<T?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
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
                return new HttpResponseWrapper<TResponse?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
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
                return new HttpResponseWrapper<TResponse?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
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
                return new HttpResponseWrapper<TResponse?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
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
                return new HttpResponseWrapper<NoContent?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }

        // ==================== CORE PARSER ====================
        private static async Task<HttpResponseWrapper<T?>> ParseResponseAsync<T>(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            var raw = await resp.Content.ReadAsStringAsync(ct);

            ApiResponse<T>? apiResponse = null;

            // 1) Intentar deserializar como ApiResponse<T>
            try
            {
                apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(raw, _jsonOptions);
            }
            catch
            {
                // 2) Si falla, intentar deserializar como ApiResponse<object>
                try
                {
                    var generic = JsonSerializer.Deserialize<ApiResponse<object>>(raw, _jsonOptions);

                    if (generic is not null)
                    {
                        // Si el servidor indica éxito y el HTTP también, marcamos Success=true,
                        // aunque no tengamos Data tipado (T) en este camino fallback.
                        if (resp.IsSuccessStatusCode && generic.Success)
                        {
                            return new HttpResponseWrapper<T?>(
                                success: true,
                                response: default,
                                error: generic.Message,
                                httpResponse: resp);
                        }

                        return new HttpResponseWrapper<T?>(
                            success: false,
                            response: default,
                            error: generic.Message ?? $"HTTP {(int)resp.StatusCode}",
                            httpResponse: resp);
                    }
                }
                catch
                {
                    // 3) Si tampoco encaja ApiResponse<object>, devolvemos el raw
                    return new HttpResponseWrapper<T?>(
                        success: false,
                        response: default,
                        error: $"Invalid server response: {raw}",
                        httpResponse: resp);
                }
            }

            // Si no se pudo deserializar ni siquiera a ApiResponse<T>
            if (apiResponse is null)
            {
                return new HttpResponseWrapper<T?>(
                    success: false,
                    response: default,
                    error: "Empty server response.",
                    httpResponse: resp);
            }

            // Caso éxito normal: HTTP 2xx y Success = true
            if (resp.IsSuccessStatusCode && apiResponse.Success)
            {
                return new HttpResponseWrapper<T?>(
                    success: true,
                    response: apiResponse.Data,
                    error: apiResponse.Message,
                    httpResponse: resp);
            }

            // Caso error: devolvemos siempre el mensaje del servidor si existe
            return new HttpResponseWrapper<T?>(
                success: false,
                response: default,
                error: apiResponse.Message ?? $"HTTP {(int)resp.StatusCode}",
                httpResponse: resp);
        }

        // ==================== POST MULTIPART ====================
        public async Task<HttpResponseWrapper<TResponse?>> PostMultipartAsync<TResponse>(
            string url,
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            try
            {
                using var resp = await _http.PostAsync(url, content, ct);
                return await ParseResponseAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper<TResponse?>(
                    success: false,
                    response: default,
                    error: ex.Message,
                    httpResponse: new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
            }
        }
    }
}
