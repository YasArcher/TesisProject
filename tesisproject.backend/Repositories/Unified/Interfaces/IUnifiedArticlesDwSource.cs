namespace tesisproject.backend.Repositories.Unified.Interfaces;

/// <summary>
/// Read-only operational source used by the Articles DW load.
/// Implementations own query execution; the ETL owns DW transformation and persistence.
/// </summary>
public interface IUnifiedArticlesDwSource
{
    Task<ArticlesDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwPublicationRow>> ListArticlePublicationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwAuthorRow>> ListArticleAuthorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwIndexingRow>> ListArticleIndexingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwVenueRow>> ListVenuesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwVenueMetricRow>> ListVenueMetricsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwAcademicTermRow>> ListAcademicTermsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwPublicationStatusRow>> ListPublicationStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwResearchLineRow>> ListResearchLinesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwBroadFieldRow>> ListBroadFieldsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwSpecificFieldRow>> ListSpecificFieldsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwDetailedFieldRow>> ListDetailedFieldsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwFacultyRow>> ListFacultiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticlesDwIndexingSourceRow>> ListIndexingSourcesAsync(CancellationToken ct = default);
}

public sealed record ArticlesDwDateBounds(
    DateTime? MinProductCreatedAt,
    DateTime? MaxProductCreatedAt,
    DateTime? MinPublishedAt,
    DateTime? MaxPublishedAt);

public sealed record ArticlesDwPublicationRow(
    int ProductId,
    int? ArticleId,
    int ProductTypeId,
    string Title,
    bool IsActive,
    DateTime CreatedAt,
    string? Journal,
    string? IndexingDatabase,
    decimal? Sjr,
    string? SjrRaw,
    string? Quartile,
    string? IssnIsbn,
    string? Doi,
    short? PublicationYear,
    string? PublicationYearRaw,
    string? PublicationUrl,
    int? ProjectId,
    bool IsProjectResult,
    int? ProjectFacultyId,
    DateTime? PublishedAt,
    int? PageCount,
    bool? IsOpenAccess,
    bool? HasInterculturalComponent,
    int? VenueId,
    int? AcademicTermId,
    byte? PublicationStatusId,
    int? ResearchLineId,
    int? BroadFieldId,
    int? SpecificFieldId,
    int? DetailedFieldId,
    int? ArticleFacultyId,
    string? ProceedingsName,
    string? Proceedings,
    string? EventName,
    string? GroupName,
    string? Filiacion,
    string? ExternalSource,
    string? ExternalId);

public sealed record ArticlesDwAuthorRow(
    int ProductAuthorId,
    int ProductId,
    int AuthorId,
    bool IsInstitutional,
    int? AppUserId,
    int? IdAsp,
    int? ExternalResearcherId,
    string? ExternalFullName,
    string? Orcid,
    int? AuthorOrder,
    bool IsPrimaryAuthor,
    string? Participation,
    string? NameSnapshot,
    string? AffiliationSnapshot);

public sealed record ArticlesDwIndexingRow(
    int ProductId,
    int ArticleId,
    int IndexingSourceId,
    string Name,
    string? Abbreviation,
    bool IsActive);

public sealed record ArticlesDwVenueRow(
    int VenueId,
    string Name,
    string? IssnCode,
    string? Issue,
    string? Volume,
    string? Url,
    string Type);

public sealed record ArticlesDwVenueMetricRow(
    int VenueId,
    short Year,
    decimal? Sjr,
    string? Quartile);

public sealed record ArticlesDwAcademicTermRow(
    int AcademicTermId,
    int? ExternalPeriodId,
    string Name,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record ArticlesDwPublicationStatusRow(byte PublicationStatusId, string Name);
public sealed record ArticlesDwResearchLineRow(int ResearchLineId, string Name);
public sealed record ArticlesDwBroadFieldRow(int BroadFieldId, string Name);
public sealed record ArticlesDwSpecificFieldRow(int SpecificFieldId, int BroadFieldId, string? Code, string Name);
public sealed record ArticlesDwDetailedFieldRow(int DetailedFieldId, int SpecificFieldId, string? Code, string Name);

public sealed record ArticlesDwFacultyRow(
    int FacultyId,
    int? ExternalFacultyId,
    int? ParentFacultyId,
    string? Acronym,
    string Name,
    bool IsActive);

public sealed record ArticlesDwIndexingSourceRow(
    int IndexingSourceId,
    string Name,
    string? Abbreviation,
    string? ReferenceUrl,
    bool IsActive);
