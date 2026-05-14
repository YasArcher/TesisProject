namespace tesisproject.backend.Data.Entities;

public sealed class ReportingPerformanceMetric
{
    public long ReportingPerformanceMetricId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Module { get; set; } = "Reporting";
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public bool Succeeded { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Roles { get; set; }
    public string? FilterSummaryJson { get; set; }
    public int? ResultCount { get; set; }
    public long? PayloadBytes { get; set; }
    public long ManualBaselineMs { get; set; }
    public long EstimatedTimeSavedMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
