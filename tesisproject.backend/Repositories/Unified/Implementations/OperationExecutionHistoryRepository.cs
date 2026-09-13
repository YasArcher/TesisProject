using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Contracts.Administration;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class OperationExecutionHistoryRepository(UnifiedDideDbContext context)
    : IOperationExecutionHistoryRepository
{
    public Task AddAsync(OperationExecutionHistory entity, CancellationToken ct = default)
        => context.OperationExecutionHistories.AddAsync(entity, ct).AsTask();

    public Task<OperationExecutionHistory?> GetTrackedAsync(Guid executionId, CancellationToken ct = default)
        => context.OperationExecutionHistories.SingleOrDefaultAsync(x => x.ExecutionId == executionId, ct);

    public Task<OperationExecutionItem?> GetAsync(Guid executionId, CancellationToken ct = default)
        => Project(context.OperationExecutionHistories.AsNoTracking().Where(x => x.ExecutionId == executionId))
            .SingleOrDefaultAsync(ct);

    public async Task<OperationExecutionPage> ListAsync(OperationExecutionQuery query, CancellationToken ct = default)
    {
        var source = context.OperationExecutionHistories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.OperationType)) source = source.Where(x => x.OperationType == query.OperationType);
        if (!string.IsNullOrWhiteSpace(query.OperationCode)) source = source.Where(x => x.OperationCode == query.OperationCode);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.From.HasValue) source = source.Where(x => x.StartedAt >= query.From.Value);
        if (query.To.HasValue) source = source.Where(x => x.StartedAt <= query.To.Value);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);
        var total = await source.CountAsync(ct);
        var items = await Project(source.OrderByDescending(x => x.StartedAt)
                .Skip((page - 1) * size).Take(size)).ToListAsync(ct);
        return new(items, page, size, total);
    }

    public Task<OperationExecutionItem?> FindSuccessfulAsync(string operationType, string operationCode,
        string? fileHash = null, CancellationToken ct = default)
    {
        var source = context.OperationExecutionHistories.AsNoTracking().Where(x =>
            x.OperationType == operationType && x.OperationCode == operationCode &&
            x.Status == OperationExecutionStatuses.Succeeded);
        if (fileHash is not null) source = source.Where(x => x.FileHash == fileHash);
        return Project(source.OrderByDescending(x => x.CompletedAt)).FirstOrDefaultAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
    public void ClearTracking() => context.ChangeTracker.Clear();

    private static IQueryable<OperationExecutionItem> Project(IQueryable<OperationExecutionHistory> query)
        => query.Select(x => new OperationExecutionItem(x.Id, x.ExecutionId, x.OperationType,
            x.OperationCode, x.Version, x.Status, x.StartedAt, x.CompletedAt,
            x.ExecutedByAppUserId, x.ExecutedByName, x.Source, x.FileName, x.FileHash,
            x.ResultJson, x.ErrorCode, x.ErrorMessage));
}
