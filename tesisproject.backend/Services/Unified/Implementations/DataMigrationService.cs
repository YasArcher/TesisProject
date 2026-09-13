using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using tesisproject.backend.Data;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class DataMigrationService : IDataMigrationService
{
    private readonly IReadOnlyDictionary<string, IDataMigration> _migrations;
    private readonly IOperationExecutionHistoryService _history;
    private readonly UnifiedDideDbContext _context;
    private readonly AdministrativeOperationsOptions _options;

    public DataMigrationService(IEnumerable<IDataMigration> migrations,
        IOperationExecutionHistoryService history, UnifiedDideDbContext context,
        IOptions<AdministrativeOperationsOptions> options)
    {
        var all = migrations.OrderBy(x => x.Order).ToArray();
        var duplicate = all.GroupBy(x => x.Code, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate data migration code: {duplicate.Key}");
        _migrations = all.ToDictionary(x => x.Code, StringComparer.Ordinal);
        _history = history;
        _context = context;
        _options = options.Value;
    }

    public async Task<ServiceResult<IReadOnlyList<DataMigrationItem>>> ListAsync(CancellationToken ct = default)
    {
        var items = new List<DataMigrationItem>(_migrations.Count);
        foreach (var migration in _migrations.Values.OrderBy(x => x.Order))
        {
            var applied = await _history.FindSuccessfulAsync(OperationExecutionTypes.DataMigration, migration.Code, null, ct);
            items.Add(new(migration.Code, migration.Description, migration.Version, migration.Order,
                applied is not null, applied?.CompletedAt, applied?.ExecutionId));
        }
        return ServiceResult<IReadOnlyList<DataMigrationItem>>.Ok(items);
    }

    public async Task<ServiceResult<DataMigrationApplyResult>> ApplyAsync(string code, string? suppliedSecret,
        int? actorAppUserId, string? actorName, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Fail(ErrorMessages.AdministrativeOperations.Disabled, ErrorType.Forbidden, ErrorCodes.AdministrativeOperations.Disabled);
        if (string.IsNullOrWhiteSpace(_options.Secret))
            return Fail(ErrorMessages.AdministrativeOperations.ConfigurationInvalid, ErrorType.Unexpected, ErrorCodes.AdministrativeOperations.ConfigurationInvalid);
        if (!SecretMatches(_options.Secret, suppliedSecret))
            return Fail(ErrorMessages.AdministrativeOperations.SecretInvalid, ErrorType.Forbidden, ErrorCodes.AdministrativeOperations.SecretInvalid);
        if (!_migrations.TryGetValue(code, out var migration))
            return Fail(ErrorMessages.AdministrativeOperations.DataMigrationNotFound, ErrorType.NotFound, ErrorCodes.AdministrativeOperations.DataMigrationNotFound);
        var previous = await _history.FindSuccessfulAsync(OperationExecutionTypes.DataMigration, migration.Code, null, ct);
        if (previous is not null)
            return Fail(ErrorMessages.AdministrativeOperations.DataMigrationAlreadyApplied, ErrorType.Conflict, ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied);

        var execution = await _history.StartAsync(new(OperationExecutionTypes.DataMigration,
            migration.Code, migration.Version, actorAppUserId, actorName, "runtime-csharp"), ct);
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var result = await migration.ApplyAsync(ct);
            var completed = await _history.CompleteSuccessAsync(execution.ExecutionId, result, ct);
            await transaction.CommitAsync(ct);
            return ServiceResult<DataMigrationApplyResult>.Ok(new(migration.Code,
                completed.ExecutionId, completed.Status, completed.ResultJson));
        }
        catch (OperationCanceledException)
        {
            await RecordFailureAsync(execution.ExecutionId, ErrorCodes.Common.OperationCanceled,
                ErrorMessages.Common.OperationCanceled);
            throw;
        }
        catch (Exception)
        {
            await RecordFailureAsync(execution.ExecutionId,
                ErrorCodes.AdministrativeOperations.DataMigrationFailed,
                ErrorMessages.AdministrativeOperations.DataMigrationFailed);
            return Fail(ErrorMessages.AdministrativeOperations.DataMigrationFailed,
                ErrorType.Conflict, ErrorCodes.AdministrativeOperations.DataMigrationFailed);
        }
    }

    private async Task RecordFailureAsync(Guid executionId, string code, string message)
    {
        try { await _history.CompleteFailureAsync(executionId, code, message, null, CancellationToken.None); }
        catch { /* Preserve original operation outcome; normal logging occurs at API boundary. */ }
    }

    private static bool SecretMatches(string expected, string? supplied)
    {
        if (string.IsNullOrEmpty(supplied)) return false;
        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private static ServiceResult<DataMigrationApplyResult> Fail(string message, ErrorType type, string code)
        => ServiceResult<DataMigrationApplyResult>.Fail(message, type, code);
}
