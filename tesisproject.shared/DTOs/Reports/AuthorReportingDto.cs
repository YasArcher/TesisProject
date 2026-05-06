namespace tesisproject.shared.DTOs.Reports;

public sealed class AuthorReportingDashboardDto
{
    public AuthorReportingKpiDto Kpis { get; set; } = new();
    public AuthorReportingFilterOptionsDto FilterOptions { get; set; } = new();
    public List<AuthorReportingSummaryDto> Authors { get; set; } = new();
    public List<AuthorPublicationDto> Publications { get; set; } = new();
    public List<AuthorCoauthorDto> Coauthors { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByAffiliation { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByFaculty { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByIndexingSource { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByQuartile { get; set; } = new();
    public List<ReportingSummaryItemDto> ArticlesByMonth { get; set; } = new();
}

public sealed class AuthorReportingKpiDto
{
    public int TotalAuthors { get; set; }
    public int TotalAuthorArticleLinks { get; set; }
    public int TotalArticles { get; set; }
    public int ArticlesWithAuthorTrace { get; set; }
    public int ArticlesWithoutAuthorTrace { get; set; }
    public int PrimaryAuthorLinks { get; set; }
    public int CoauthorLinks { get; set; }
    public int AuthorsWithOrcid { get; set; }
    public int AuthorsWithAffiliation { get; set; }
    public decimal AverageAuthorsPerArticle { get; set; }
}

public sealed class AuthorReportingSummaryDto
{
    public int AuthorKey { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public string Affiliation { get; set; } = string.Empty;
    public string ParticipantType { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Orcid { get; set; }
    public int TotalArticles { get; set; }
    public int PrimaryAuthorArticles { get; set; }
    public int CoauthorArticles { get; set; }
}

public sealed class AuthorPublicationDto
{
    public int AuthorKey { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public string Affiliation { get; set; } = string.Empty;
    public string ParticipantType { get; set; } = string.Empty;
    public bool IsPrimaryAuthor { get; set; }
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Doi { get; set; }
    public string? Issn { get; set; }
    public string? JournalUrl { get; set; }
    public string? PublicationUrl { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? Year { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string IndexingSourceName { get; set; } = string.Empty;
    public string Quartile { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string ResearchLine { get; set; } = string.Empty;
    public string BroadField { get; set; } = string.Empty;
    public string SpecificField { get; set; } = string.Empty;
    public string DetailedField { get; set; } = string.Empty;
}

public sealed class AuthorCoauthorDto
{
    public int AuthorKey { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int CoauthorKey { get; set; }
    public string CoauthorName { get; set; } = string.Empty;
    public string CoauthorAffiliation { get; set; } = string.Empty;
    public int SharedArticles { get; set; }
}

public sealed class AuthorReportingFilterOptionsDto
{
    public List<string> Authors { get; set; } = new();
    public List<string> Affiliations { get; set; } = new();
    public List<string> ParticipantTypes { get; set; } = new();
}
