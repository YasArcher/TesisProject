using tesisproject.shared.DTOs.Workflow;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IWorkflowClient
    {
        Task<List<WorkflowInboxItemDto>> GetReviewInboxAsync(int take = 50, CancellationToken ct = default);
        Task<List<WorkflowInboxItemDto>> GetAuthorInboxAsync(int take = 50, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto?> GetBatchWorkflowAsync(int batchId, CancellationToken ct = default);
        Task<BulkImportBatchDetailDto?> GetBatchPreviewAsync(int batchId, int previewRows = 50, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto?> ClaimAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto?> ReturnAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto?> ApproveAsync(int batchId, WorkflowActionRequest request, CancellationToken ct = default);
    }
}
