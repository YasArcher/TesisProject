namespace tesisproject.backend.Services.Analytic.Contracts;

public sealed record DwFullLoadHistoryResult(
    long DurationMilliseconds,
    DwFullLoadCounts? Counts,
    IReadOnlyList<DwEtlWarningCount> Warnings);

public sealed record DwFullLoadCounts(
    int DimDate,
    int DimProjectState,
    int DimFundingType,
    int DimProductType,
    int DimFaculty,
    int DimResearchCategory,
    int DimIndexingDatabase,
    int DimQuartile,
    int DimAuthor,
    int DimJournal,
    int FactProject,
    int FactBudget,
    int FactProduct,
    int BridgeProjectResearchCategory,
    int BridgeProductAuthor);

public sealed record DwEtlWarningCount(string Code, int Count);

public sealed record ArticlesDwFullLoadHistoryResult(
    long DurationMilliseconds,
    ArticlesDwFullLoadCounts? Counts,
    IReadOnlyList<DwEtlWarningCount> Warnings);

public sealed record ArticlesDwFullLoadCounts(
    int DimDates, int DimAuthors, int DimJournals, int DimProductTypes,
    int DimFaculties, int DimIndexingDatabases, int DimQuartiles,
    int DimArticles, int DimVenues, int DimAcademicTerms,
    int DimPublicationStatuses, int DimResearchLines, int DimFields,
    int DimIndexingSources, int FactArticlePublications,
    int FactArticleAuthors, int FactArticleIndexings, int FactVenueMetricYears);
