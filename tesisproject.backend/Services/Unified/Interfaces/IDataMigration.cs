namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IDataMigration
{
    string Code { get; }
    string Description { get; }
    string? Version { get; }
    int Order { get; }
    Task<object?> ApplyAsync(CancellationToken ct = default);
}
