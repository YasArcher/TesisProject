using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly ITokenStore _tokenStore;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http, ITokenStore tokenStore)
        {
            _http = http;
            _tokenStore = tokenStore;
        }

        // ==================== GET ====================
        public async Task<HttpResponseWrapper<T?>> GetAsync<T>(string url, CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Get, url, content: null, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<T>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<T>(ex);
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
                using var content = JsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<TResponse>(ex);
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
                using var content = JsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Put, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<TResponse>(ex);
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
                using var content = JsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Patch, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<TResponse>(ex);
            }
        }

        // ==================== DELETE ====================
        public async Task<HttpResponseWrapper<TResponse?>> DeleteAsync<TResponse>(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Delete, url, content: null, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Delete, url, content: null, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<NoContent?>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<NoContent?>(ex);
            }
        }

        // ==================== POST MULTIPART ====================
        public async Task<HttpResponseWrapper<TResponse?>> PostMultipartAsync<TResponse>(
            string url,
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ParseServiceResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return Fail<TResponse>(ex);
            }
        }

        // ==================== GET FILE (BLOB) ====================
        public async Task<HttpResponseWrapper<FilePayloadDTO?>> GetFileAsync(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Get, url, content: null, ct);

                using var resp = await _http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorResult = await TryReadServiceResultAsync<object>(resp, ct);

                    return new HttpResponseWrapper<FilePayloadDTO?>(
                        success: false,
                        response: null,
                        message: errorResult?.Message ?? $"HTTP {(int)resp.StatusCode}",
                        errorType: errorResult?.Error ?? ErrorType.Unexpected,
                        errorCode: errorResult?.ErrorCode,
                        validationErrors: errorResult?.ValidationErrors,
                        httpResponse: Snapshot(resp));
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                var contentType = resp.Content.Headers.ContentType?.ToString()
                                  ?? "application/octet-stream";

                var fileName = TryGetFileName(resp) ?? "document";

                return new HttpResponseWrapper<FilePayloadDTO?>(
                    success: true,
                    response: new FilePayloadDTO(bytes, contentType, fileName),
                    message: null,
                    errorType: ErrorType.None,
                    errorCode: null,
                    validationErrors: null,
                    httpResponse: Snapshot(resp));
            }
            catch (Exception ex)
            {
                return Fail<FilePayloadDTO>(ex);
            }
        }

        public async Task<HttpResponseWrapper<FilePayloadDTO?>> PostFileAsync<TRequest>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);

                using var resp = await _http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorResult = await TryReadServiceResultAsync<object>(resp, ct);

                    return new HttpResponseWrapper<FilePayloadDTO?>(
                        success: false,
                        response: null,
                        message: errorResult?.Message ?? $"HTTP {(int)resp.StatusCode}",
                        errorType: errorResult?.Error ?? ErrorType.Unexpected,
                        errorCode: errorResult?.ErrorCode,
                        validationErrors: errorResult?.ValidationErrors,
                        httpResponse: Snapshot(resp));
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                var contentType = resp.Content.Headers.ContentType?.ToString()
                                  ?? "application/octet-stream";

                var fileName = TryGetFileName(resp) ?? "export.xlsx";

                return new HttpResponseWrapper<FilePayloadDTO?>(
                    success: true,
                    response: new FilePayloadDTO(bytes, contentType, fileName),
                    message: null,
                    errorType: ErrorType.None,
                    errorCode: null,
                    validationErrors: null,
                    httpResponse: Snapshot(resp));
            }
            catch (Exception ex)
            {
                return Fail<FilePayloadDTO>(ex);
            }
        }

        // ==================== CORE JSON PARSER ====================
        private static async Task<HttpResponseWrapper<T?>> ParseServiceResultAsync<T>(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            string raw = string.Empty;

            try
            {
                raw = await resp.Content.ReadAsStringAsync(ct);
            }
            catch
            {
                // Si no se puede leer, igual devolvemos status.
            }

            ServiceResult<T>? result = null;

            // 1) intenta ServiceResult<T>
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    result = JsonSerializer.Deserialize<ServiceResult<T>>(raw, _jsonOptions);
                }
                catch
                {
                    // ignore
                }
            }

            if (result is not null)
            {
                var ok = resp.IsSuccessStatusCode && result.Success;

                return new HttpResponseWrapper<T?>(
                    success: ok,
                    response: ok ? result.Data : default,
                    message: result.Message ?? $"HTTP {(int)resp.StatusCode}",
                    errorType: result.Error,
                    errorCode: result.ErrorCode,
                    validationErrors: result.ValidationErrors,
                    httpResponse: Snapshot(resp));
            }

            // 2) fallback ServiceResult<object> para extraer metadata
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var generic = JsonSerializer.Deserialize<ServiceResult<object>>(raw, _jsonOptions);
                    if (generic is not null)
                    {
                        var ok = resp.IsSuccessStatusCode && generic.Success;

                        return new HttpResponseWrapper<T?>(
                            success: ok,
                            response: default,
                            message: generic.Message ?? $"HTTP {(int)resp.StatusCode}",
                            errorType: generic.Error,
                            errorCode: generic.ErrorCode,
                            validationErrors: generic.ValidationErrors,
                            httpResponse: Snapshot(resp));
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // 3) fallback final: texto crudo
            var msg = string.IsNullOrWhiteSpace(raw)
                ? $"HTTP {(int)resp.StatusCode}"
                : raw;

            return new HttpResponseWrapper<T?>(
                success: resp.IsSuccessStatusCode,
                response: default,
                message: msg,
                errorType: resp.IsSuccessStatusCode ? ErrorType.None : ErrorType.Unexpected,
                errorCode: null,
                validationErrors: null,
                httpResponse: Snapshot(resp));
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(
            HttpMethod method,
            string url,
            HttpContent? content,
            CancellationToken ct)
        {
            var req = new HttpRequestMessage(method, url)
            {
                Content = content
            };

            var token = await _tokenStore.GetAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return req;
        }

        private static StringContent JsonContent<T>(T body)
            => new(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");

        private static HttpResponseWrapper<T?> Fail<T>(Exception ex)
            => new(
                success: false,
                response: default,
                message: ex.Message,
                errorType: ErrorType.Unexpected,
                errorCode: "CLIENT_UNHANDLED_EXCEPTION",
                validationErrors: null,
                httpResponse: new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = ex.Message
                });

        private static HttpResponseMessage Snapshot(HttpResponseMessage resp)
            => new(resp.StatusCode)
            {
                ReasonPhrase = resp.ReasonPhrase
            };

        private static async Task<ServiceResult<T>?> TryReadServiceResultAsync<T>(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            try
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                try
                {
                    return JsonSerializer.Deserialize<ServiceResult<T>>(raw, _jsonOptions);
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetFileName(HttpResponseMessage resp)
        {
            var cd = resp.Content.Headers.ContentDisposition;
            var fromContent = cd?.FileNameStar ?? cd?.FileName;

            if (!string.IsNullOrWhiteSpace(fromContent))
                return fromContent.Trim('"');

            if (resp.Headers.TryGetValues("Content-Disposition", out var values) ||
                resp.Content.Headers.TryGetValues("Content-Disposition", out values))
            {
                var raw = values.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(raw) &&
                    ContentDispositionHeaderValue.TryParse(raw, out var parsed))
                {
                    var name = parsed.FileNameStar ?? parsed.FileName;
                    if (!string.IsNullOrWhiteSpace(name))
                        return name.Trim('"');
                }
            }

            return null;
        }
    }
}