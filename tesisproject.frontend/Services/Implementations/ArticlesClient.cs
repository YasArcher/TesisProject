using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.Abstractions;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Implementations
{
    public class ArticlesClient : IArticlesClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ArticlesClient(HttpClient http)
        {
            _http = http;
            if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType?.Contains("json") == true))
                _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        // ===== GET ALL =====
        // La interfaz la tienes como: Task<Result<IReadOnlyList<ArticleDto>>?> GetAllAsync()
        public async Task<Result<IReadOnlyList<ArticleDto>>?> GetAllAsync()
        {
            var url = "/api/articles";
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!resp.IsSuccessStatusCode)
                return FailFrom<IReadOnlyList<ArticleDto>>(resp, "No se pudieron obtener los artículos.");

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            var payload = await resp.Content.ReadAsByteArrayAsync();

            if (IsJson(contentType))
            {
                // 1) Intento: Result<IReadOnlyList<ArticleDto>>
                if (TryDeserialize(payload, out Result<IReadOnlyList<ArticleDto>>? typed) && typed is not null)
                    return typed;

                // 2) Intento: lista plana
                if (TryDeserialize(payload, out List<ArticleDto>? list) && list is not null)
                    return new Result<IReadOnlyList<ArticleDto>>(true, list, null);
            }

            var sample = SampleText(payload);
            return Result<IReadOnlyList<ArticleDto>>.Fail(
                $"Respuesta no compatible del API en {url}. Content-Type: {contentType}. Muestra: {sample}"
            );
        }

        // ===== GET BY ID =====
        public async Task<Result<ArticleDto>> GetByIdAsync(int id)
        {
            var url = $"/api/articles/{id}";
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!resp.IsSuccessStatusCode)
                return FailFrom<ArticleDto>(resp, $"No se pudo obtener el artículo {id}.");

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            var payload = await resp.Content.ReadAsByteArrayAsync();

            if (IsJson(contentType))
            {
                // 1) Result<ArticleDto>
                if (TryDeserialize(payload, out Result<ArticleDto>? typed) && typed is not null)
                    return typed;

                // 2) Objeto plano
                if (TryDeserialize(payload, out ArticleDto? obj) && obj is not null)
                    return new Result<ArticleDto>(true, obj, null);
            }

            var sample = SampleText(payload);
            return Result<ArticleDto>.Fail(
                $"Respuesta no compatible del API en {url}. Content-Type: {contentType}. Muestra: {sample}"
            );
        }

        // ===== CREATE =====
        public async Task<Result<int>> CreateAsync(CreateArticleRequest req)
        {
            var url = "/api/articles";
            using var resp = await _http.PostAsJsonAsync(url, req, _json);

            if (!resp.IsSuccessStatusCode)
                return FailFrom<int>(resp, "No se pudo crear el artículo.");

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            var payload = await resp.Content.ReadAsByteArrayAsync();

            if (IsJson(contentType))
            {
                // 1) Result<int>
                if (TryDeserialize(payload, out Result<int>? typed) && typed is not null)
                    return typed;

                // 2) { "value": 123 }
                if (TryDeserialize(payload, out AnonValueInt? anon) && anon is not null)
                    return new Result<int>(true, anon.Value, null);
            }

            var sample = SampleText(payload);
            return Result<int>.Fail(
                $"Respuesta no compatible del API en {url}. Content-Type: {contentType}. Muestra: {sample}"
            );
        }

        // ===== UPDATE =====
        public async Task<Result> UpdateAsync(UpdateArticleRequest req)
        {
            var url = "/api/articles";
            using var resp = await _http.PutAsJsonAsync(url, req, _json);

            if (!resp.IsSuccessStatusCode)
                return FailFrom(resp, "No se pudo actualizar el artículo.");

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            var payload = await resp.Content.ReadAsByteArrayAsync();

            if (IsJson(contentType))
            {
                // 1) Result (no genérico)
                if (TryDeserialize(payload, out Result? typed) && typed is not null)
                    return typed;

                // 2) booleano simple (true = ok)
                if (TryDeserialize(payload, out bool ok) && ok)
                    return new Result(true, null);
            }

            var sample = SampleText(payload);
            return Result.Fail(
                $"Respuesta no compatible del API en {url}. Content-Type: {contentType}. Muestra: {sample}"
            );
        }

        // ===== Helpers =====
        private static bool IsJson(string contentType)
            => !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

        private static bool TryDeserialize<T>(byte[] payload, out T? value)
        {
            try
            {
                value = JsonSerializer.Deserialize<T>(payload, _json);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static string SampleText(byte[] payload)
        {
            if (payload is null || payload.Length == 0) return "<vacío>";
            var text = Encoding.UTF8.GetString(payload);
            return text.Length <= 280 ? text : text[..280] + "…";
        }

        private static Result<T> FailFrom<T>(HttpResponseMessage resp, string friendly)
        {
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return Result<T>.Fail($"{friendly} (401 Unauthorized). ¿Token ausente/expirado?");
            if (resp.StatusCode == HttpStatusCode.Forbidden)
                return Result<T>.Fail($"{friendly} (403 Forbidden). Sin permisos.");
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return Result<T>.Fail($"{friendly} (404 NotFound).");

            // Leer un pedazo del cuerpo para diagnosticar
            try
            {
                var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var sample = string.IsNullOrWhiteSpace(body) ? "<vacío>" :
                             (body.Length <= 280 ? body : body[..280] + "…");
                return Result<T>.Fail($"{friendly} (HTTP {(int)resp.StatusCode}). Respuesta: {sample}");
            }
            catch
            {
                return Result<T>.Fail($"{friendly} (HTTP {(int)resp.StatusCode}).");
            }
        }

        private static Result FailFrom(HttpResponseMessage resp, string friendly)
        {
            // Reutilizamos el método genérico solo para construir el texto de error
            var tmp = FailFrom<string?>(resp, friendly);
            return Result.Fail(tmp.Error ?? friendly);
        }

        private sealed class AnonValueInt
        {
            public int Value { get; set; }
        }
    }
}
