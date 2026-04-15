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

public sealed class ReportingArticleDetailRow
{
    public long FactArticlePublicationId { get; set; }
    public int ArticleKey { get; set; }
    public int ArticleId_OLTP { get; set; }
    public string? Title { get; set; }
    public string? Doi { get; set; }
    public short? ArticleYear { get; set; }
    public string? PublicationUrl { get; set; }
    public bool IsOpenAccess { get; set; }
    public bool IsProjectResult { get; set; }
    public bool HasInterculturalComponent { get; set; }
    public string? VenueName { get; set; }
    public string? VenueType { get; set; }
    public string? PublicationStatus { get; set; }
    public string? AcademicTerm { get; set; }
    public string? ResearchLine { get; set; }
    public string? BroadFieldName { get; set; }
    public string? SpecificFieldName { get; set; }
    public string? DetailedFieldName { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? PublishedDate { get; set; }
    public int? PageCount { get; set; }
    public int ArticleCount { get; set; }
}

public sealed class ReportingQuartileDistributionRow
{
    public string? Quartile { get; set; }
    public int TotalVenues { get; set; }
}

public sealed class ReportingVenueMetricRow
{
    public short YearNumber { get; set; }
    public string? VenueName { get; set; }
    public string? VenueType { get; set; }
    public decimal? SJR { get; set; }
    public decimal? CiteScore { get; set; }
    public int? HIndex { get; set; }
    public string? Quartile { get; set; }
}

public sealed class ReportingArticleAuthorSummaryRow
{
    public string Name { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}
