using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Services.Unified.Contracts.Administration;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IOperationExecutionHistoryRepository
{
    Task AddAsync(OperationExecutionHistory entity, CancellationToken ct = default);
    Task<OperationExecutionHistory?> GetTrackedAsync(Guid executionId, CancellationToken ct = default);
    Task<OperationExecutionItem?> GetAsync(Guid executionId, CancellationToken ct = default);
    Task<OperationExecutionPage> ListAsync(OperationExecutionQuery query, CancellationToken ct = default);
    Task<OperationExecutionItem?> FindSuccessfulAsync(string operationType, string operationCode,
        string? fileHash = null, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void ClearTracking();
}
