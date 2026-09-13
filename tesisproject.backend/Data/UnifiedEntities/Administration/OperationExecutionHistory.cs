namespace tesisproject.backend.Data.UnifiedEntities.Administration;

public sealed class OperationExecutionHistory
{
    public long Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string OperationCode { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? ExecutedByAppUserId { get; set; }
    public string? ExecutedByName { get; set; }
    public string? Source { get; set; }
    public string? FileName { get; set; }
    public string? FileHash { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
