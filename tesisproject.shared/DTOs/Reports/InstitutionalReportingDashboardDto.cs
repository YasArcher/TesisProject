namespace tesisproject.shared.DTOs.Reports;

public sealed class InstitutionalReportingDashboardDto
{
    public ReportingHealthDto Health { get; set; } = new();
    public ScientificProductionKpiDto ScientificProduction { get; set; } = new();
    public LoadQualityKpiDto LoadQuality { get; set; } = new();
    public WorkflowKpiDto Workflow { get; set; } = new();
    public List<ArticlesByYearDto> ArticlesByYear { get; set; } = new();
    public List<IndexingSourceSummaryDto> ArticlesByIndexingSource { get; set; } = new();
    public List<WorkflowCurrentStageDto> WorkflowCurrentStages { get; set; } = new();
}

public sealed class ScientificProductionKpiDto
{
    public int TotalArticles { get; set; }
    public int OpenAccessArticles { get; set; }
    public int ProjectResultArticles { get; set; }
    public int InterculturalArticles { get; set; }
}

public sealed class LoadQualityKpiDto
{
    public int TotalBatches { get; set; }
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int ErrorRows { get; set; }
}

public sealed class WorkflowKpiDto
{
    public int TotalStageExecutions { get; set; }
    public decimal? AvgStageDurationSeconds { get; set; }
    public int ApprovedStages { get; set; }
    public int ReturnedStages { get; set; }
}

public sealed class IndexingSourceSummaryDto
{
    public string IndexingSourceName { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}

public sealed class WorkflowCurrentStageDto
{
    public int BatchId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string? StageGroupName { get; set; }
    public string? StageStatus { get; set; }
    public int? StartDateKey { get; set; }
    public int? EndDateKey { get; set; }
    public int? StageDurationSeconds { get; set; }
    public bool Approved { get; set; }
    public bool Returned { get; set; }
}
