using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.frontend.Services.Implementations
{
    public class BulkImportClient : IBulkImportClient
    {
        private readonly HttpClient _http;
        private readonly IApiClient _api;

        public BulkImportClient(HttpClient http, IApiClient api)
        {
            _http = http;
            _api = api;
        }

        public async Task<List<BulkImportBatchSummaryDto>> GetBatchesAsync(string? entityName = "Article", int take = 20, CancellationToken ct = default)
        {
            return (await GetBatchesResultAsync(entityName, take, ct)).Data ?? new List<BulkImportBatchSummaryDto>();
        }

        public async Task<(byte[] Content, string FileName, string ContentType)> GenerateTemplateAsync(BulkImportTemplateRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("api/import-batches/template", request, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? $"plantilla_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return (content, fileName.Trim('"'), contentType);
        }

        public async Task<BulkImportBatchDetailDto?> UploadAsync(IBrowserFile file, string sourceType, string? notes, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream(25_000_000);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(sourceType ?? "Excel"), "sourceType");
            if (!string.IsNullOrWhiteSpace(notes))
            {
                content.Add(new StringContent(notes), "notes");
            }

            using var response = await _http.PostAsync("api/import-batches/upload", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude crear el lote en staging.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportBatchDetailDto>(cancellationToken: ct);
        }

        public async Task<BulkImportBatchDetailDto?> GetBatchAsync(int batchId, int previewRows = 25, CancellationToken ct = default)
            => await RequireDataAsync(
                GetBatchResultAsync(batchId, previewRows, ct),
                "No pude abrir el lote.");

        public async Task<BulkImportActionResultDto?> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CreateBatchFromExternalArticleResultAsync(request, ct),
                "No pude crear el lote externo en staging.");

        public async Task<BulkImportActionResultDto?> CreateBatchFromExternalArticlesAsync(ExternalArticlesImportRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CreateBatchFromExternalArticlesResultAsync(request, ct),
                "No pude crear el lote externo múltiple en staging.");

        public async Task<BulkImportActionResultDto?> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CorrectRowResultAsync(batchId, rowId, request, ct),
                "No pude corregir la fila del staging.");

        public async Task<BulkImportActionResultDto?> ValidateBatchAsync(int batchId, CancellationToken ct = default)
            => await RequireDataAsync(
                ValidateBatchResultAsync(batchId, ct),
                "No pude validar el lote.");

        public async Task<BulkImportActionResultDto?> ProcessBatchAsync(int batchId, CancellationToken ct = default)
            => await RequireDataAsync(
                ProcessBatchResultAsync(batchId, ct),
                "No pude procesar el lote.");

        public Task<HttpResponseWrapper<List<BulkImportBatchSummaryDto>?>> GetBatchesResultAsync(string? entityName = "Article", int take = 20, CancellationToken ct = default)
        {
            var url = $"api/import-batches?entityName={Uri.EscapeDataString(entityName ?? "Article")}&take={take}";
            return _api.GetResultAsync<List<BulkImportBatchSummaryDto>>(url, ct);
        }

        public Task<HttpResponseWrapper<BulkImportBatchDetailDto?>> GetBatchResultAsync(int batchId, int previewRows = 25, CancellationToken ct = default)
            => _api.GetResultAsync<BulkImportBatchDetailDto>($"api/import-batches/{batchId}?previewRows={previewRows}", ct);

        public Task<HttpResponseWrapper<BulkImportActionResultDto?>> CreateBatchFromExternalArticleResultAsync(ExternalArticleImportRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<ExternalArticleImportRequest, BulkImportActionResultDto>("api/import-batches/external-article", request, ct);

        public Task<HttpResponseWrapper<BulkImportActionResultDto?>> CreateBatchFromExternalArticlesResultAsync(ExternalArticlesImportRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<ExternalArticlesImportRequest, BulkImportActionResultDto>("api/import-batches/external-articles", request, ct);

        public Task<HttpResponseWrapper<BulkImportActionResultDto?>> CorrectRowResultAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default)
            => _api.PutResultAsync<BulkImportRowCorrectionRequest, BulkImportActionResultDto>($"api/import-batches/{batchId}/rows/{rowId}", request, ct);

        public Task<HttpResponseWrapper<BulkImportActionResultDto?>> ValidateBatchResultAsync(int batchId, CancellationToken ct = default)
            => _api.PostResultAsync<object?, BulkImportActionResultDto>($"api/import-batches/{batchId}/validate", null, ct);

        public Task<HttpResponseWrapper<BulkImportActionResultDto?>> ProcessBatchResultAsync(int batchId, CancellationToken ct = default)
            => _api.PostResultAsync<object?, BulkImportActionResultDto>($"api/import-batches/{batchId}/process", null, ct);

        private static async Task<T?> RequireDataAsync<T>(Task<HttpResponseWrapper<T?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }

            return result.Data;
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback, CancellationToken ct)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;

                if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? fallback;
                }

                if (root.TryGetProperty("detail", out var detailElement) && detailElement.ValueKind == JsonValueKind.String)
                {
                    return detailElement.GetString() ?? fallback;
                }

                if (root.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
                {
                    return titleElement.GetString() ?? fallback;
                }
            }
            catch
            {
            }

            return raw;
        }
    }
}
