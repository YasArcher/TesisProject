using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using tesisproject.frontend.Services.Errors;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.frontend.Services.Implementations
{
    public class BulkImportClient : IBulkImportClient
    {
        private readonly HttpClient _http;

        public BulkImportClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<BulkImportBatchSummaryDto>> GetBatchesAsync(string? entityName = "Article", int take = 20, CancellationToken ct = default)
        {
            var url = $"api/import-batches?entityName={Uri.EscapeDataString(entityName ?? "Article")}&take={take}";
            return await _http.GetFromJsonAsync<List<BulkImportBatchSummaryDto>>(url, ct) ?? new List<BulkImportBatchSummaryDto>();
        }

        public async Task<(byte[] Content, string FileName, string ContentType)> GenerateTemplateAsync(BulkImportTemplateRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("api/import-batches/template", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude generar la plantilla de carga.", ct));
            }

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
        {
            using var response = await _http.GetAsync($"api/import-batches/{batchId}?previewRows={previewRows}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude abrir el lote.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportBatchDetailDto>(cancellationToken: ct);
        }

        public async Task<BulkImportActionResultDto?> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("api/import-batches/external-article", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude crear el lote externo en staging.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportActionResultDto>(cancellationToken: ct);
        }

        public async Task<BulkImportActionResultDto?> CreateBatchFromExternalArticlesAsync(ExternalArticlesImportRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("api/import-batches/external-articles", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude crear el lote externo múltiple en staging.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportActionResultDto>(cancellationToken: ct);
        }

        public async Task<BulkImportActionResultDto?> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PutAsJsonAsync($"api/import-batches/{batchId}/rows/{rowId}", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude corregir la fila del staging.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportActionResultDto>(cancellationToken: ct);
        }

        public async Task<BulkImportActionResultDto?> ValidateBatchAsync(int batchId, CancellationToken ct = default)
        {
            using var response = await _http.PostAsync($"api/import-batches/{batchId}/validate", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude validar el lote.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportActionResultDto>(cancellationToken: ct);
        }

        public async Task<BulkImportActionResultDto?> ProcessBatchAsync(int batchId, CancellationToken ct = default)
        {
            using var response = await _http.PostAsync($"api/import-batches/{batchId}/process", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude procesar el lote.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportActionResultDto>(cancellationToken: ct);
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback, CancellationToken ct)
            => await UserFacingErrorMapper.FromHttpResponseAsync(response, fallback, ct);
    }
}
