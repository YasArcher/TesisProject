using System.Net.Http.Json;
using System.Text.Json;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.frontend.Services.Implementations
{
    public class WorkflowClient : IWorkflowClient
    {
        private readonly HttpClient _http;

        public WorkflowClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<WorkflowInboxItemDto>> GetReviewInboxAsync(int take = 50, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/workflows/import-batches/inbox/review?take={take}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude cargar la bandeja de revisión.", ct));
            }

            return await response.Content.ReadFromJsonAsync<List<WorkflowInboxItemDto>>(cancellationToken: ct)
                ?? new List<WorkflowInboxItemDto>();
        }

        public async Task<List<WorkflowInboxItemDto>> GetAuthorInboxAsync(int take = 50, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/workflows/import-batches/inbox/author?take={take}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude cargar la bandeja del autor.", ct));
            }

            return await response.Content.ReadFromJsonAsync<List<WorkflowInboxItemDto>>(cancellationToken: ct)
                ?? new List<WorkflowInboxItemDto>();
        }

        public async Task<WorkflowBatchDetailDto?> GetBatchWorkflowAsync(int batchId, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/workflows/import-batches/{batchId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude abrir el workflow del lote.", ct));
            }

            return await response.Content.ReadFromJsonAsync<WorkflowBatchDetailDto>(cancellationToken: ct);
        }

        public async Task<BulkImportBatchDetailDto?> GetBatchPreviewAsync(int batchId, int previewRows = 50, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/workflows/import-batches/{batchId}/preview?previewRows={previewRows}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude abrir la previsualización del artículo.", ct));
            }

            return await response.Content.ReadFromJsonAsync<BulkImportBatchDetailDto>(cancellationToken: ct);
        }

        public async Task<WorkflowBatchDetailDto?> ClaimAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
        {
            return await PostActionAsync(batchId, "claim", request, "No pude tomar la etapa del workflow.", ct);
        }

        public async Task<WorkflowBatchDetailDto?> ReturnAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
        {
            return await PostActionAsync(batchId, "return", request, "No pude devolver la etapa del workflow.", ct);
        }

        public async Task<WorkflowBatchDetailDto?> ApproveAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
        {
            return await PostActionAsync(batchId, "approve", request, "No pude aprobar la etapa del workflow.", ct);
        }

        private async Task<WorkflowBatchDetailDto?> PostActionAsync(int batchId, string action, WorkflowActionRequest request, string fallback, CancellationToken ct)
        {
            using var response = await _http.PostAsJsonAsync($"api/workflows/import-batches/{batchId}/{action}", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, fallback, ct));
            }

            return await response.Content.ReadFromJsonAsync<WorkflowBatchDetailDto>(cancellationToken: ct);
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
