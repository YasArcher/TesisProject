// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Reports;

public sealed class ReportingPerformanceSummaryDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public decimal AverageDurationSeconds { get; set; }
    public decimal AverageEstimatedManualMinutes { get; set; }
    public decimal AverageEstimatedSavedMinutes { get; set; }
    public decimal EstimatedReductionPercent { get; set; }
    public List<ReportingPerformanceOperationDto> Operations { get; set; } = new();
    public List<ReportingPerformanceDailyDto> DailyUsage { get; set; } = new();
}

public sealed class ReportingPerformanceOperationDto
{
    public string Operation { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public decimal AverageDurationSeconds { get; set; }
    public decimal AverageEstimatedManualMinutes { get; set; }
    public decimal EstimatedReductionPercent { get; set; }
}

public sealed class ReportingPerformanceDailyDto
{
    public DateTime Date { get; set; }
    public int TotalOperations { get; set; }
    public decimal AverageDurationSeconds { get; set; }
}

