namespace tesisproject.backend.Services.Unified.Contracts.Administration;

public static class OperationExecutionTypes
{
    public const string DataMigration = "DATA_MIGRATION";
    public const string BulkImport = "BULK_IMPORT";
    public const string Synchronization = "SYNCHRONIZATION";
    public const string Etl = "ETL";
    public const string BackgroundJob = "BACKGROUND_JOB";
    public const string ManualProcess = "MANUAL_PROCESS";
}

public static class OperationExecutionStatuses
{
    public const string Running = "RUNNING";
    public const string Succeeded = "SUCCEEDED";
    public const string PartiallySucceeded = "PARTIALLY_SUCCEEDED";
    public const string Failed = "FAILED";
}

public static class OperationCodes
{
    public const string ProjectsInitialCatalogV1 = "PROJECTS_INITIAL_CATALOG_V1";
    public const string ProjectsMatrixImport = "PROJECTS_MATRIX_IMPORT";
    public const string FacultiesSync = "FACULTIES_SYNC";
    public const string AcademicTermsSync = "ACADEMIC_TERMS_SYNC";
    public const string DwFullLoad = "DW_FULL_LOAD";
    public const string ProjectsDwFullLoad = "PROJECTS_DW_FULL_LOAD";
    public const string ArticlesDwFullLoad = "ARTICLES_DW_FULL_LOAD";
}

public sealed record OperationExecutionStart(
    string OperationType,
    string OperationCode,
    string? Version = null,
    int? ExecutedByAppUserId = null,
    string? ExecutedByName = null,
    string? Source = null,
    string? FileName = null,
    string? FileHash = null);

public sealed record OperationExecutionItem(
    long Id, Guid ExecutionId, string OperationType, string OperationCode,
    string? Version, string Status, DateTime StartedAt, DateTime? CompletedAt,
    int? ExecutedByAppUserId, string? ExecutedByName, string? Source,
    string? FileName, string? FileHash, string? ResultJson,
    string? ErrorCode, string? ErrorMessage);

public sealed record OperationExecutionQuery(
    string? OperationType = null,
    string? OperationCode = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);

public sealed record OperationExecutionPage(
    IReadOnlyList<OperationExecutionItem> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record DataMigrationItem(
    string Code, string Description, string? Version, int Order,
    bool Applied, DateTime? AppliedAt, Guid? ExecutionId);

public sealed record DataMigrationApplyResult(
    string Code, Guid ExecutionId, string Status, string? ResultJson);
