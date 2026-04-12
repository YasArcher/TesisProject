using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.frontend.Services.Implementations
{
    public class WorkflowClient : IWorkflowClient
    {
        private readonly IApiClient _api;

        public WorkflowClient(IApiClient api)
        {
            _api = api;
        }

        public async Task<List<WorkflowInboxItemDto>> GetReviewInboxAsync(int take = 50, CancellationToken ct = default)
            => (await GetReviewInboxResultAsync(take, ct)).Data ?? new List<WorkflowInboxItemDto>();

        public async Task<List<WorkflowInboxItemDto>> GetAuthorInboxAsync(int take = 50, CancellationToken ct = default)
            => (await GetAuthorInboxResultAsync(take, ct)).Data ?? new List<WorkflowInboxItemDto>();

        public async Task<WorkflowBatchDetailDto?> GetBatchWorkflowAsync(int batchId, CancellationToken ct = default)
            => await RequireDataAsync(
                GetBatchWorkflowResultAsync(batchId, ct),
                "No pude abrir el workflow del lote.");

        public async Task<BulkImportBatchDetailDto?> GetBatchPreviewAsync(int batchId, int previewRows = 50, CancellationToken ct = default)
            => await RequireDataAsync(
                GetBatchPreviewResultAsync(batchId, previewRows, ct),
                "No pude abrir la previsualización del artículo.");

        public async Task<WorkflowBatchDetailDto?> ClaimAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                ClaimResultAsync(batchId, request, ct),
                "No pude tomar la etapa del workflow.");

        public async Task<WorkflowBatchDetailDto?> ReturnAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                ReturnResultAsync(batchId, request, ct),
                "No pude devolver la etapa del workflow.");

        public async Task<WorkflowBatchDetailDto?> ApproveAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                ApproveResultAsync(batchId, request, ct),
                "No pude aprobar la etapa del workflow.");

        public Task<HttpResponseWrapper<List<WorkflowInboxItemDto>?>> GetReviewInboxResultAsync(int take = 50, CancellationToken ct = default)
            => _api.GetResultAsync<List<WorkflowInboxItemDto>>($"api/workflows/import-batches/inbox/review?take={take}", ct);

        public Task<HttpResponseWrapper<List<WorkflowInboxItemDto>?>> GetAuthorInboxResultAsync(int take = 50, CancellationToken ct = default)
            => _api.GetResultAsync<List<WorkflowInboxItemDto>>($"api/workflows/import-batches/inbox/author?take={take}", ct);

        public Task<HttpResponseWrapper<WorkflowBatchDetailDto?>> GetBatchWorkflowResultAsync(int batchId, CancellationToken ct = default)
            => _api.GetResultAsync<WorkflowBatchDetailDto>($"api/workflows/import-batches/{batchId}", ct);

        public Task<HttpResponseWrapper<BulkImportBatchDetailDto?>> GetBatchPreviewResultAsync(int batchId, int previewRows = 50, CancellationToken ct = default)
            => _api.GetResultAsync<BulkImportBatchDetailDto>($"api/workflows/import-batches/{batchId}/preview?previewRows={previewRows}", ct);

        public Task<HttpResponseWrapper<WorkflowBatchDetailDto?>> ClaimResultAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => PostActionResultAsync(batchId, "claim", request, ct);

        public Task<HttpResponseWrapper<WorkflowBatchDetailDto?>> ReturnResultAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => PostActionResultAsync(batchId, "return", request, ct);

        public Task<HttpResponseWrapper<WorkflowBatchDetailDto?>> ApproveResultAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default)
            => PostActionResultAsync(batchId, "approve", request, ct);

        private Task<HttpResponseWrapper<WorkflowBatchDetailDto?>> PostActionResultAsync(int batchId, string action, WorkflowActionRequest request, CancellationToken ct)
            => _api.PostResultAsync<WorkflowActionRequest, WorkflowBatchDetailDto>($"api/workflows/import-batches/{batchId}/{action}", request, ct);

        private static async Task<T?> RequireDataAsync<T>(Task<HttpResponseWrapper<T?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }

            return result.Data;
        }
    }
}
