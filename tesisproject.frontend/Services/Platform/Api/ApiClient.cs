using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.Abstractions;
using tesisproject.shared.Wrappers;

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
            if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }
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

        public async Task DeleteAsync(string url, CancellationToken ct = default)
        {
            using var res = await _http.DeleteAsync(url, ct);
            await EnsureSuccess(res, ct);
        }

        public async Task<HttpResponseWrapper<T?>> GetResultAsync<T>(string url, CancellationToken ct = default)
        {
            try
            {
                using var res = await _http.GetAsync(url, ct);
                return await ReadWrappedResultAsync<T>(res, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<T>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PostResultAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            try
            {
                using var res = await _http.PostAsJsonAsync(url, body!, _json, ct);
                return await ReadWrappedResultAsync<TResponse>(res, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> PutResultAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        {
            try
            {
                using var res = await _http.PutAsJsonAsync(url, body!, _json, ct);
                return await ReadWrappedResultAsync<TResponse>(res, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        public async Task<HttpResponseWrapper<TResponse?>> DeleteResultAsync<TResponse>(string url, CancellationToken ct = default)
        {
            try
            {
                using var res = await _http.DeleteAsync(url, ct);
                return await ReadWrappedResultAsync<TResponse>(res, ct);
            }
            catch (Exception ex)
            {
                return BuildTransportFailure<TResponse>(ex);
            }
        }

        private static async Task<T?> ReadJsonOrDefaultAsync<T>(HttpContent content, CancellationToken ct)
        {
            if (content is null)
            {
                return default;
            }

            var payload = await content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(payload, _json);
            }
            catch
            {
                return default;
            }
        }

        private static async Task EnsureSuccess(HttpResponseMessage res, CancellationToken ct = default)
        {
            if (res.IsSuccessStatusCode)
            {
                return;
            }

            string body = "";
            try
            {
                body = await res.Content.ReadAsStringAsync(ct);
            }
            catch
            {
            }

            ApiProblem? problem = null;
            try
            {
                problem = JsonSerializer.Deserialize<ApiProblem>(body, _json);
            }
            catch
            {
            }

            throw new ApiException(res.StatusCode, body, problem);
        }

        private static async Task<HttpResponseWrapper<T?>> ReadWrappedResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return HttpResponseWrapper<T?>.Ok(default, response.StatusCode);
            }

            string raw;
            try
            {
                raw = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                return HttpResponseWrapper<T?>.Fail(
                    $"No se pudo leer la respuesta del servidor: {ex.Message}",
                    response.StatusCode,
                    "CLIENT_RESPONSE_READ_ERROR");
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return response.IsSuccessStatusCode
                    ? HttpResponseWrapper<T?>.Ok(default, response.StatusCode)
                    : HttpResponseWrapper<T?>.Fail($"HTTP {(int)response.StatusCode}", response.StatusCode, "EMPTY_ERROR_RESPONSE");
            }

            var apiResult = TryDeserialize<ApiResult<T>>(raw);
            if (apiResult is not null)
            {
                return apiResult.Succeeded
                    ? HttpResponseWrapper<T?>.Ok(apiResult.Value, response.StatusCode)
                    : HttpResponseWrapper<T?>.Fail(apiResult.Error, response.StatusCode, "API_RESULT_ERROR");
            }

            var result = TryDeserialize<Result<T>>(raw);
            if (result is not null)
            {
                return result.Succeeded
                    ? HttpResponseWrapper<T?>.Ok(result.Value, response.StatusCode)
                    : HttpResponseWrapper<T?>.Fail(result.Error, response.StatusCode, "RESULT_ERROR");
            }

            if (response.IsSuccessStatusCode)
            {
                var directData = TryDeserialize<T>(raw);
                return directData is not null
                    ? HttpResponseWrapper<T?>.Ok(directData, response.StatusCode)
                    : HttpResponseWrapper<T?>.Fail(
                        "La respuesta del servidor no coincide con el contrato esperado.",
                        response.StatusCode,
                        "UNPARSEABLE_SUCCESS_RESPONSE");
            }

            var problem = TryDeserialize<ApiProblem>(raw);
            if (problem is not null)
            {
                return HttpResponseWrapper<T?>.Fail(
                    problem.Detail ?? problem.Title ?? $"HTTP {(int)response.StatusCode}",
                    response.StatusCode,
                    "PROBLEM_DETAILS_ERROR",
                    problem.Errors);
            }

            return HttpResponseWrapper<T?>.Fail(raw, response.StatusCode, "UNPARSEABLE_ERROR_RESPONSE");
        }

        private static T? TryDeserialize<T>(string raw)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(raw, _json);
            }
            catch
            {
                return default;
            }
        }

        private static HttpResponseWrapper<T?> BuildTransportFailure<T>(Exception ex)
            => HttpResponseWrapper<T?>.Fail(ex.Message, HttpStatusCode.ServiceUnavailable, "CLIENT_UNHANDLED_EXCEPTION");
    }

    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? Title { get; }
        public string? Detail { get; }
        public IDictionary<string, string[]>? Errors { get; }
        public string? RawBody { get; }

        public ApiException(HttpStatusCode status, string? body, ApiProblem? problem)
            : base(BuildMessage(status, problem, body))
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

        private static string Truncate(string? s)
            => string.IsNullOrEmpty(s) ? "" : (s.Length > 800 ? s[..800] + "..." : s);
    }

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
