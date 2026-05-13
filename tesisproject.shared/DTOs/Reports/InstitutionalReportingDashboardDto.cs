namespace tesisproject.shared.DTOs.Reports;

public sealed class InstitutionalReportingDashboardDto
{
    public ReportingHealthDto Health { get; set; } = new();
    public ScientificProductionKpiDto ScientificProduction { get; set; } = new();
    public AuthorTraceCoverageDto AuthorTraceCoverage { get; set; } = new();
    public LoadQualityKpiDto LoadQuality { get; set; } = new();
    public WorkflowKpiDto Workflow { get; set; } = new();
    public InstitutionalReportingFilterOptionsDto FilterOptions { get; set; } = new();
    public List<ArticlesByYearDto> ArticlesByYear { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByMonth { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByDay { get; set; } = new();
    public List<IndexingSourceSummaryDto> ArticlesByIndexingSource { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByPublicationStatus { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByAcademicTerm { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByResearchLine { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByAuthor { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByFaculty { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByVenueType { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByQuartile { get; set; } = new();
    public List<ReportingFieldSummaryDto> ArticlesByField { get; set; } = new();
    public List<ReportingVenueSummaryDto> ArticlesByVenue { get; set; } = new();
    public List<ReportingQuartileSummaryDto> QuartileDistribution { get; set; } = new();
    public List<ReportingOpenAccessByYearDto> OpenAccessByYear { get; set; } = new();
    public List<ReportingVenueMetricDto> VenueMetricsByYear { get; set; } = new();
    public List<ReportingArticleDetailDto> RecentArticles { get; set; } = new();
    public List<WorkflowCurrentStageDto> WorkflowCurrentStages { get; set; } = new();
    public List<ReportingArticleIndexingDetailDto> PediIiitArticles { get; set; } = new();
    public List<ReportingArticleIndexingDetailDto> TddTotalArticles { get; set; } = new();
    public ReportingParticipationSummaryDto ParticipationSummary { get; set; } = new();
}

public sealed class AuthorTraceCoverageDto
{
    public int ArticlesWithAuthorTrace { get; set; }
    public int ArticlesWithoutAuthorTrace { get; set; }
}

public sealed class InstitutionalReportingFilterDto
{
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    public string? AcademicTerm { get; set; }
    public string? PublicationStatus { get; set; }
    public string? ResearchLine { get; set; }
    public string? Faculty { get; set; }
    public string? IndexingSource { get; set; }
    public string? BroadField { get; set; }
    public string? SpecificField { get; set; }
    public string? DetailedField { get; set; }
    public string? VenueName { get; set; }
    public string? VenueType { get; set; }
    public int? ArticleYear { get; set; }
    public string? ArticleMonth { get; set; }
    public string? Quartile { get; set; }
    public bool? IsOpenAccess { get; set; }
    public bool? IsProjectResult { get; set; }
    public bool? HasInterculturalComponent { get; set; }
    public string? PeriodDateType { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAffiliation { get; set; }
    public string? ParticipantType { get; set; }
    public bool? HasOrcid { get; set; }
    public bool? OnlyPrimaryAuthors { get; set; }
    public string? CoauthorName { get; set; }
    public bool IncludePdfKpis { get; set; } = true;
    public bool IncludePdfFilters { get; set; } = true;
    public bool IncludePdfPeriod { get; set; } = true;
    public bool IncludePdfFields { get; set; } = true;
    public bool IncludePdfVenues { get; set; } = true;
    public bool IncludePdfAuthors { get; set; } = true;
    public bool IncludePdfPediIiit { get; set; } = true;
    public bool IncludePdfTddTotal { get; set; } = true;
    public bool IncludePdfParticipation { get; set; } = true;
    public bool IncludePdfArticles { get; set; } = true;
}

public sealed class InstitutionalReportingFilterOptionsDto
{
    public List<string> AcademicTerms { get; set; } = new();
    public List<string> PublicationStatuses { get; set; } = new();
    public List<string> ResearchLines { get; set; } = new();
    public List<string> Faculties { get; set; } = new();
    public List<string> IndexingSources { get; set; } = new();
    public List<string> BroadFields { get; set; } = new();
    public List<string> SpecificFields { get; set; } = new();
    public List<string> DetailedFields { get; set; } = new();
    public List<string> Venues { get; set; } = new();
    public List<string> VenueTypes { get; set; } = new();
    public List<int> ArticleYears { get; set; } = new();
    public List<string> ArticleMonths { get; set; } = new();
    public List<string> Quartiles { get; set; } = new();
    public List<string> Authors { get; set; } = new();
    public List<string> Affiliations { get; set; } = new();
    public List<string> ParticipantTypes { get; set; } = new();
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

public sealed class ReportingSummaryItemDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}

public sealed class ReportingFieldSummaryDto
{
    public string BroadField { get; set; } = string.Empty;
    public string SpecificField { get; set; } = string.Empty;
    public string DetailedField { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}

public sealed class ReportingVenueSummaryDto
{
    public string VenueName { get; set; } = string.Empty;
    public string VenueType { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}

public sealed class ReportingQuartileSummaryDto
{
    public string Quartile { get; set; } = string.Empty;
    public int TotalVenues { get; set; }
}

public sealed class ReportingOpenAccessByYearDto
{
    public int Year { get; set; }
    public int OpenAccessArticles { get; set; }
    public int NonOpenAccessArticles { get; set; }
}

public sealed class ReportingVenueMetricDto
{
    public int Year { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueType { get; set; } = string.Empty;
    public decimal? Sjr { get; set; }
    public decimal? CiteScore { get; set; }
    public int? HIndex { get; set; }
    public string? Quartile { get; set; }
}

public sealed class ReportingArticleDetailDto
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Doi { get; set; }
    public int? Year { get; set; }
    public string? VenueName { get; set; }
    public string? PublicationStatus { get; set; }
    public string? ResearchLine { get; set; }
    public string? BroadField { get; set; }
    public bool IsOpenAccess { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? PublishedDate { get; set; }
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

public sealed class ReportingArticleIndexingDetailDto
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Doi { get; set; }
    public string IndexingSourceName { get; set; } = string.Empty;
    public string? VenueName { get; set; }
    public string? Issn { get; set; }
    public string? JournalUrl { get; set; }
    public string? PublicationUrl { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string PublicationMonth { get; set; } = string.Empty;
    public string? AuthorIdentification { get; set; }
    public string? AuthorName { get; set; }
    public string? ParticipantType { get; set; }
    public string? Career { get; set; }
    public bool IsProjectResult { get; set; }
    public string? ProjectName { get; set; }
    public bool HasInterculturalComponent { get; set; }
    public string Quartile { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string? BroadField { get; set; }
    public string? SpecificField { get; set; }
    public string? DetailedField { get; set; }
}

public sealed class ReportingParticipationSummaryDto
{
    public int TotalArticles { get; set; }
    public int TotalIndexingLinks { get; set; }
    public List<ReportingParticipationItemDto> ByFaculty { get; set; } = new();
    public List<ReportingParticipationItemDto> ByIndexingSource { get; set; } = new();
    public List<ReportingParticipationItemDto> ByQuartile { get; set; } = new();
    public List<ReportingFacultyIndexingBreakdownDto> IndexingByFaculty { get; set; } = new();
}

public sealed class ReportingParticipationItemDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class ReportingFacultyIndexingBreakdownDto
{
    public string Faculty { get; set; } = string.Empty;
    public string IndexingSourceName { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
}
