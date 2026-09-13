namespace tesisproject.backend.Services.Unified.Contracts.Administration;

public sealed record ProjectImportIdentifier(int ProjectId, string ProjectCode, int ProjectNumber);

public sealed record ProjectsMatrixImportResult(
    int TotalRows,
    int Inserted,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<ProjectImportIdentifier> Projects);

public sealed record CatalogSynchronizationHistoryResult(
    int Received,
    int Inserted,
    int Updated,
    int Unchanged,
    int Skipped,
    int Failed,
    IReadOnlyList<CatalogSynchronizationIdentifier> Items);

public sealed record CatalogSynchronizationIdentifier(int ExternalId, int LocalId);
