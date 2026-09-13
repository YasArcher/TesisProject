using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IDataMigrationService
{
    Task<ServiceResult<IReadOnlyList<DataMigrationItem>>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<DataMigrationApplyResult>> ApplyAsync(string code, string? suppliedSecret,
        int? actorAppUserId, string? actorName, CancellationToken ct = default);
}
