namespace tesisproject.backend.Services.Unified.Contracts;

/// <summary>Counts for one validated snapshot; Updated excludes timestamp-only initialization.</summary>
public sealed record CatalogSynchronizationResult(int TotalExternal, int Inserted, int Updated,
    int Unchanged, int Skipped, int Failed, DateTime SyncedAt);
