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

        public async Task<HttpResponseWrapper<T?>> GetAsync<T>(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Get, url, content: null, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<T>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<T>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                using var content = BuildJsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PutAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                using var content = BuildJsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Put, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PatchAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                using var content = BuildJsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Patch, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> DeleteAsync<TResponse>(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Delete, url, content: null, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
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
                return await ReadJsonResultAsync<NoContent?>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<NoContent?>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PostMultipartAsync<TResponse>(
            string url,
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);
                using var resp = await _http.SendAsync(req, ct);
                return await ReadJsonResultAsync<TResponse>(resp, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<FilePayloadDTO?>> GetFileAsync(
            string url,
            CancellationToken ct = default)
        {
            try
            {
                using var req = await CreateRequestAsync(HttpMethod.Get, url, content: null, ct);
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!resp.IsSuccessStatusCode)
                    return await BuildFileFailureAsync(resp, ct);

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var fileName = TryGetFileName(resp) ?? "document";

                return HttpResponseWrapper<FilePayloadDTO?>.Ok(
                    new FilePayloadDTO(bytes, contentType, fileName));
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<FilePayloadDTO>(ex);
            }
        }

        public async Task<HttpResponseWrapper<FilePayloadDTO?>> PostFileAsync<TRequest>(
            string url,
            TRequest body,
            CancellationToken ct = default)
        {
            try
            {
                using var content = BuildJsonContent(body);
                using var req = await CreateRequestAsync(HttpMethod.Post, url, content, ct);
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!resp.IsSuccessStatusCode)
                    return await BuildFileFailureAsync(resp, ct);

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var fileName = TryGetFileName(resp) ?? "export.xlsx";

                return HttpResponseWrapper<FilePayloadDTO?>.Ok(
                    new FilePayloadDTO(bytes, contentType, fileName));
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<FilePayloadDTO>(ex);
            }
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

        private static StringContent BuildJsonContent<T>(T body)
            => new(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");

        // IMPORTANTE:
        // Este método es el punto central de traducción entre la respuesta real del backend
        // y el contrato que consume el frontend mediante HttpResponseWrapper<T?>.
        //
        // Si en el futuro cambia el shape de la respuesta del backend
        // (por ejemplo: nombres de propiedades, estructura del error, ubicación de Data/Message/ErrorCode/
        // ValidationErrors o cualquier ajuste arquitectónico del contrato HTTP),
        // la adaptación debe hacerse aquí.
        //
        // La idea es que los componentes, páginas y client services NO tengan que cambiar;
        // solo este traductor debe absorber esos cambios mientras mantenga estable
        // el contrato que usa el frontend.
        private static async Task<HttpResponseWrapper<T?>> ReadJsonResultAsync<T>(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            // 204 o respuesta vacía exitosa
            if (resp.StatusCode == HttpStatusCode.NoContent)
            {
                return HttpResponseWrapper<T?>.Ok(default);
            }

            string raw;

            try
            {
                raw = await resp.Content.ReadAsStringAsync(ct);
            }
            catch
            {
                return HttpResponseWrapper<T?>.Fail(
                    message: $"No se pudo leer la respuesta del servidor. HTTP {(int)resp.StatusCode}.",
                    error: MapHttpStatusToError(resp.StatusCode),
                    errorCode: "CLIENT_RESPONSE_READ_ERROR");
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return resp.IsSuccessStatusCode
                    ? HttpResponseWrapper<T?>.Ok(default)
                    : HttpResponseWrapper<T?>.Fail(
                        message: $"HTTP {(int)resp.StatusCode}",
                        error: MapHttpStatusToError(resp.StatusCode),
                        errorCode: "EMPTY_ERROR_RESPONSE");
            }

            // 1) Intenta con el shape backend actual
            try
            {
                var backendResult = JsonSerializer.Deserialize<ServiceResult<T>>(raw, _jsonOptions);
                if (backendResult is not null)
                {
                    return MapBackendResult(backendResult, resp.StatusCode);
                }
            }
            catch
            {
                // ignore
            }

            // 2) Fallback genérico para rescatar metadata aunque T no coincida
            try
            {
                var genericBackendResult = JsonSerializer.Deserialize<ServiceResult<object>>(raw, _jsonOptions);
                if (genericBackendResult is not null)
                {
                    return new HttpResponseWrapper<T?>(
                        success: false,
                        data: default,
                        message: genericBackendResult.Message ?? $"HTTP {(int)resp.StatusCode}",
                        error: NormalizeError(genericBackendResult.Error, resp.StatusCode),
                        errorCode: genericBackendResult.ErrorCode ?? "UNMAPPED_BACKEND_ERROR",
                        validationErrors: genericBackendResult.ValidationErrors);
                }
            }
            catch
            {
                // ignore
            }

            // 3) Si no se pudo interpretar el contrato, devolvemos fallo controlado
            return HttpResponseWrapper<T?>.Fail(
                message: resp.IsSuccessStatusCode
                    ? "La respuesta del servidor no coincide con el contrato esperado."
                    : raw,
                error: MapHttpStatusToError(resp.StatusCode),
                errorCode: "UNPARSEABLE_BACKEND_RESPONSE");
        }

        // Mapea el contrato backend actualmente esperado al contrato estable que consume el frontend.
        // Si cambia el backend pero sigue pudiendo deserializarse, ajustar esta conversión aquí.
        private static HttpResponseWrapper<T?> MapBackendResult<T>(
            ServiceResult<T> backendResult,
            HttpStatusCode statusCode)
        {
            var httpOk = (int)statusCode >= 200 && (int)statusCode <= 299;
            var success = httpOk && backendResult.Success;

            if (success)
            {
                return HttpResponseWrapper<T?>.Ok(
                    backendResult.Data,
                    backendResult.Message);
            }

            return new HttpResponseWrapper<T?>(
                success: false,
                data: default,
                message: backendResult.Message ?? $"HTTP {(int)statusCode}",
                error: NormalizeError(backendResult.Error, statusCode),
                errorCode: backendResult.ErrorCode,
                validationErrors: backendResult.ValidationErrors);
        }

        private static ErrorType NormalizeError(ErrorType backendError, HttpStatusCode statusCode)
        {
            if (backendError != ErrorType.None)
                return backendError;

            return MapHttpStatusToError(statusCode);
        }

        private static ErrorType MapHttpStatusToError(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest => ErrorType.Validation,
                HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
                HttpStatusCode.Forbidden => ErrorType.Forbidden,
                HttpStatusCode.NotFound => ErrorType.NotFound,
                HttpStatusCode.Conflict => ErrorType.Conflict,
                _ => ErrorType.Unexpected
            };
        }

        private static HttpResponseWrapper<T?> BuildTransportFailure<T>(Exception ex)
        {
            return HttpResponseWrapper<T?>.Fail(
                message: ex.Message,
                error: ErrorType.Unexpected,
                errorCode: "CLIENT_UNHANDLED_EXCEPTION");
        }

        private static async Task<HttpResponseWrapper<FilePayloadDTO?>> BuildFileFailureAsync(
            HttpResponseMessage resp,
            CancellationToken ct)
        {
            try
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var backend = JsonSerializer.Deserialize<ServiceResult<object>>(raw, _jsonOptions);
                        if (backend is not null)
                        {
                            return HttpResponseWrapper<FilePayloadDTO?>.Fail(
                                message: backend.Message ?? $"HTTP {(int)resp.StatusCode}",
                                error: NormalizeError(backend.Error, resp.StatusCode),
                                errorCode: backend.ErrorCode,
                                validationErrors: backend.ValidationErrors);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                return HttpResponseWrapper<FilePayloadDTO?>.Fail(
                    message: $"HTTP {(int)resp.StatusCode}",
                    error: MapHttpStatusToError(resp.StatusCode),
                    errorCode: "FILE_REQUEST_FAILED");
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<FilePayloadDTO>(ex);
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