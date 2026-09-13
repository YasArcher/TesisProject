using System.Text.Json;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class OperationExecutionHistoryService(IOperationExecutionHistoryRepository repository)
    : IOperationExecutionHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationExecutionItem> StartAsync(OperationExecutionStart request, CancellationToken ct = default)
    {
        var entity = new OperationExecutionHistory
        {
            ExecutionId = Guid.NewGuid(),
            OperationType = Required(request.OperationType, nameof(request.OperationType)),
            OperationCode = Required(request.OperationCode, nameof(request.OperationCode)),
            Version = Clean(request.Version, 40),
            Status = OperationExecutionStatuses.Running,
            StartedAt = DateTime.UtcNow,
            ExecutedByAppUserId = request.ExecutedByAppUserId,
            ExecutedByName = Clean(request.ExecutedByName, 200),
            Source = Clean(request.Source, 200),
            FileName = Clean(request.FileName is null ? null : Path.GetFileName(request.FileName), 260),
            FileHash = Clean(request.FileHash, 64)
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public Task<OperationExecutionItem> CompleteSuccessAsync(Guid executionId, object? result = null, CancellationToken ct = default)
        => CompleteAsync(executionId, OperationExecutionStatuses.Succeeded, result, null, null, ct);

    public Task<OperationExecutionItem> CompletePartialAsync(Guid executionId, object? result = null,
        string? errorCode = null, string? errorMessage = null, CancellationToken ct = default)
        => CompleteAsync(executionId, OperationExecutionStatuses.PartiallySucceeded, result, errorCode, errorMessage, ct);

    public async Task<OperationExecutionItem> CompleteFailureAsync(Guid executionId, string errorCode,
        string errorMessage, object? result = null, CancellationToken ct = default)
    {
        repository.ClearTracking();
        return await CompleteAsync(executionId, OperationExecutionStatuses.Failed, result,
            errorCode, errorMessage, ct);
    }

    public Task<OperationExecutionItem?> GetAsync(Guid executionId, CancellationToken ct = default)
        => repository.GetAsync(executionId, ct);

    public Task<OperationExecutionPage> ListAsync(OperationExecutionQuery query, CancellationToken ct = default)
        => repository.ListAsync(query, ct);

    public Task<OperationExecutionItem?> FindSuccessfulAsync(string operationType, string operationCode,
        string? fileHash = null, CancellationToken ct = default)
        => repository.FindSuccessfulAsync(operationType, operationCode, fileHash, ct);

    private async Task<OperationExecutionItem> CompleteAsync(Guid executionId, string status, object? result,
        string? errorCode, string? errorMessage, CancellationToken ct)
    {
        var entity = await repository.GetTrackedAsync(executionId, ct)
            ?? throw new KeyNotFoundException("Operation execution was not found.");
        if (entity.Status != OperationExecutionStatuses.Running)
            throw new InvalidOperationException("Only a RUNNING execution can be completed.");
        entity.Status = status;
        entity.CompletedAt = DateTime.UtcNow;
        entity.ResultJson = result is null ? null : JsonSerializer.Serialize(result, JsonOptions);
        entity.ErrorCode = Clean(errorCode, 120);
        entity.ErrorMessage = Clean(errorMessage, 1000);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static string Required(string value, string name)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
    private static string? Clean(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static OperationExecutionItem Map(OperationExecutionHistory x)
        => new(x.Id, x.ExecutionId, x.OperationType, x.OperationCode, x.Version, x.Status,
            x.StartedAt, x.CompletedAt, x.ExecutedByAppUserId, x.ExecutedByName, x.Source,
            x.FileName, x.FileHash, x.ResultJson, x.ErrorCode, x.ErrorMessage);
}
