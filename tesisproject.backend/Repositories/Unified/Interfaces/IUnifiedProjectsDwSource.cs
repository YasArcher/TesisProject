using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

/// <summary>
/// Read-only operational source used by the Projects DW load.
/// Implementations own query execution; the ETL owns DW transformation and persistence.
/// </summary>
public interface IUnifiedProjectsDwSource
{
    Task<ProjectsDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProjectStatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwCatalogRow>> ListFundingTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProductTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwFacultyRow>> ListUsedFacultiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwResearchCategoryRow>> ListResearchCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListProjectProductAttributeValuesAsync(BaseProductAttributeId attributeId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwProjectResearchCategoryRow>> ListProjectResearchCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwProjectRow>> ListProjectsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwBudgetRow>> ListBudgetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwProductRow>> ListProjectProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectsDwProductAuthorRow>> ListProjectProductAuthorsAsync(CancellationToken ct = default);
}

public sealed record ProjectsDwDateBounds(
    DateTime? MinProjectStartDate,
    DateTime? MaxProjectStartDate,
    DateTime? MinProjectEndDate,
    DateTime? MaxProjectEndDate,
    DateTime? MinProjectApprovalDate,
    DateTime? MaxProjectApprovalDate,
    DateTime? MinBudgetApprovedDate,
    DateTime? MaxBudgetApprovedDate,
    DateTime? MinProductCreatedDate,
    DateTime? MaxProductCreatedDate);

public sealed record ProjectsDwCatalogRow(int Id, string Name, bool IsActive);

public sealed record ProjectsDwFacultyRow(int FacultyId, string? Acronym, string? Name);

public sealed record ProjectsDwResearchCategoryRow(
    int Id,
    string Name,
    int ResearchCategoryTypeId,
    string CategoryTypeName,
    int ResearchCategoryGroupId,
    string ResearchCategoryGroupName,
    int? ParentCategoryId);

public sealed record ProjectsDwProjectResearchCategoryRow(int ProjectId, int ResearchCategoryId);

public sealed record ProjectsDwProjectRow(
    int ProjectId,
    int DurationInMonths,
    decimal? ExecutionPercentage,
    int FacultyId,
    int ProjectStateId,
    DateTime? ApprovalDate,
    DateTime? StartDate,
    DateTime? RealEndDate);

public sealed record ProjectsDwBudgetRow(
    int BudgetId,
    int ProjectId,
    decimal InitialAmount,
    decimal CertifiedAmount,
    decimal ExecutedAmount,
    int FacultyId,
    int FundingTypeId,
    DateTime? ApprovedAt);

public sealed record ProjectsDwProductRow(
    int ProductId,
    int ProjectId,
    int ProductTypeId,
    bool IsActive,
    DateTime CreatedAt,
    int FacultyId,
    string? IndexingDatabase,
    string? Quartile,
    string Title,
    string? Journal,
    string? Sjr,
    string? Doi,
    string? PublicationYearRaw,
    string? IssnIsbn,
    string? ConsultationUrl,
    int AuthorCount);

public sealed record ProjectsDwProductAuthorRow(
    int ProductAuthorId,
    int ProductId,
    int AuthorId,
    int? AppUserId,
    int? IdAsp,
    int? ExternalResearcherId,
    string? ExternalFullName,
    string? Orcid,
    int? AuthorOrder,
    bool IsPrimaryAuthor,
    string? Participation,
    string? NameSnapshot,
    bool IsInstitutional);
