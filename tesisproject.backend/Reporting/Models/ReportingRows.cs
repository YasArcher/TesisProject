namespace tesisproject.backend.Reporting.Models;

public sealed class ReportingEtlRunRow
{
    public long EtlRunId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class ScientificProductionKpiRow
{
    public int? TotalArticles { get; set; }
    public int? OpenAccessArticles { get; set; }
    public int? ProjectResultArticles { get; set; }
    public int? InterculturalArticles { get; set; }
}

public sealed class LoadQualityKpiRow
{
    public int? TotalBatches { get; set; }
    public int? TotalRows { get; set; }
    public int? SuccessfulRows { get; set; }
    public int? ErrorRows { get; set; }
}

public sealed class WorkflowKpiRow
{
    public int? TotalStageExecutions { get; set; }
    public decimal? AvgStageDurationSeconds { get; set; }
    public int? ApprovedStages { get; set; }
    public int? ReturnedStages { get; set; }
}

public sealed class ArticlesByYearRow
{
    public short YearNumber { get; set; }
    public int TotalArticles { get; set; }
    public int OpenAccessArticles { get; set; }
    public int ProjectResultArticles { get; set; }
    public int InterculturalArticles { get; set; }
}

public sealed class IndexingSourceSummaryRow
{
    public string IndexingSourceName { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}

public sealed class WorkflowCurrentStageRow
{
    public int BatchId_OLTP { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string? StageGroupName { get; set; }
    public string? StageStatus { get; set; }
    public int? StartDateKey { get; set; }
    public int? EndDateKey { get; set; }
    public int? StageDurationSeconds { get; set; }
    public bool ApprovedFlag { get; set; }
    public bool ReturnedFlag { get; set; }
}
