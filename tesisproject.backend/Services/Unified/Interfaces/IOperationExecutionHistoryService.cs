using tesisproject.backend.Services.Unified.Contracts.Administration;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IOperationExecutionHistoryService
{
    Task<OperationExecutionItem> StartAsync(OperationExecutionStart request, CancellationToken ct = default);
    Task<OperationExecutionItem> CompleteSuccessAsync(Guid executionId, object? result = null, CancellationToken ct = default);
    Task<OperationExecutionItem> CompletePartialAsync(Guid executionId, object? result = null,
        string? errorCode = null, string? errorMessage = null, CancellationToken ct = default);
    Task<OperationExecutionItem> CompleteFailureAsync(Guid executionId, string errorCode,
        string errorMessage, object? result = null, CancellationToken ct = default);
    Task<OperationExecutionItem?> GetAsync(Guid executionId, CancellationToken ct = default);
    Task<OperationExecutionPage> ListAsync(OperationExecutionQuery query, CancellationToken ct = default);
    Task<OperationExecutionItem?> FindSuccessfulAsync(string operationType, string operationCode,
        string? fileHash = null, CancellationToken ct = default);
}
