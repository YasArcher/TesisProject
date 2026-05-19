using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IWorkflowService
    {
        Task EnsureSeedDataAsync(CancellationToken ct = default);
        Task<WorkflowInstance?> CreateWorkflowForBatchAsync(int importBatchId, string workflowKey, string? submittedByUserId, CancellationToken ct = default);
        Task<bool> CanProcessBatchAsync(int importBatchId, string? userId, CancellationToken ct = default);
        Task<List<WorkflowInboxItemDto>> GetReviewInboxAsync(string? userId, IReadOnlyCollection<string> roleNames, int take = 50, CancellationToken ct = default);
        Task<List<WorkflowInboxItemDto>> GetAuthorInboxAsync(string? userId, int take = 50, CancellationToken ct = default);
        Task<bool> CanAuthorAccessBatchAsync(string? userId, int importBatchId, bool requireReturnedStatus = false, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto?> GetBatchWorkflowAsync(int importBatchId, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> ClaimCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> DeclineCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> ReturnCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> ReturnCurrentStageToPreviousStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> ApproveCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> ResubmitReturnedBatchAsync(int importBatchId, string? userId, string? comments, CancellationToken ct = default);
        Task<WorkflowBatchDetailDto> CancelReturnedBatchAsync(int importBatchId, string? userId, string? comments, CancellationToken ct = default);
    }
}
