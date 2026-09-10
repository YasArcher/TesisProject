using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.UnitOfWork.Unified.Interfaces;

/// <summary>
/// Coordinates repositories backed by one unified DIDE context.
/// </summary>
public interface IUnifiedUnitOfWork : IAsyncDisposable
{
    IUnifiedArticleReadRepository ArticleReads { get; }
    IUnifiedArticleRegistrationRepository ArticleRegistration { get; }
    IUnifiedArticleConfigurationRepository ArticleConfiguration { get; }
    IUnifiedArticleRegistrationMatrixRepository ArticleRegistrationMatrices { get; }
    IGenericRepository<RegistrationMatrixColumn> RegistrationMatrixColumns { get; }
    IGenericRepository<RegistrationMatrixRow> RegistrationMatrixRows { get; }
    IGenericRepository<RegistrationMatrixCell> RegistrationMatrixCells { get; }
    IGenericRepository<FieldCatalogEntry> ArticleFields { get; }
    IUnifiedFacultyRepository Faculties { get; }
    IUnifiedAcademicTermRepository AcademicTerms { get; }
    IUnifiedAppConfigurationRepository AppConfigurations { get; }
    IUnifiedAppUserRepository AppUsers { get; }
    IUnifiedAuthorRepository Authors { get; }
    IUnifiedBudgetRepository Budgets { get; }
    IUnifiedConvocationRepository Convocations { get; }
    IUnifiedDocumentRepository Documents { get; }
    IUnifiedExportFieldRepository ExportFields { get; }
    IUnifiedExportTemplateColumnRepository ExportTemplateColumns { get; }
    IUnifiedExportTemplateRepository ExportTemplates { get; }
    IUnifiedExternalResearcherProjectRepository ExternalResearcherProjects { get; }
    IUnifiedExternalResearcherRepository ExternalResearchers { get; }
    IUnifiedFacultyScopeFacultyRepository FacultyScopeFaculties { get; }
    IUnifiedFacultyScopeRepository FacultyScopes { get; }
    IUnifiedGroupMemberRepository GroupMembers { get; }
    IUnifiedGroupRepository Groups { get; }
    IUnifiedObjectiveActivityRepository ObjectiveActivities { get; }
    IUnifiedObjectiveActivityUserRepository ObjectiveActivityUsers { get; }
    IUnifiedProductAuthorRepository ProductAuthors { get; }
    IUnifiedProductAttributeDefinitionRepository ProductAttributeDefinitions { get; }
    IUnifiedProductRepository Products { get; }
    IUnifiedProductValueRepository ProductValues { get; }
    IUnifiedProjectDocumentRepository ProjectDocuments { get; }
    IUnifiedProjectExtensionRepository ProjectExtensions { get; }
    IUnifiedProjectObjectiveRepository ProjectObjectives { get; }
    IUnifiedProjectRepository Projects { get; }
    IUnifiedProjectResearchCategoryRepository ProjectResearchCategories { get; }
    IUnifiedResearchCategoryRepository ResearchCategories { get; }
    IUnifiedUserFacultyScopeAssignmentRepository UserFacultyScopeAssignments { get; }
    IUnifiedVisitIssueRepository VisitIssues { get; }
    IUnifiedVisitObjectiveActivityProgressRepository VisitObjectiveActivityProgresses { get; }
    IUnifiedVisitRepository Visits { get; }

    IUnifiedCatalogRepository<Country> Countries { get; }
    IUnifiedCatalogRepository<DocumentType> DocumentTypes { get; }
    IUnifiedCatalogRepository<FundingType> FundingTypes { get; }
    IUnifiedCatalogRepository<IndexingSource> IndexingSources { get; }
    IUnifiedCatalogRepository<Institution> Institutions { get; }
    IUnifiedCatalogRepository<MemberRoleType> MemberRoleTypes { get; }
    IUnifiedCatalogRepository<ObjectiveType> ObjectiveTypes { get; }
    IUnifiedCatalogRepository<ProductAttribute> ProductAttributes { get; }
    IUnifiedCatalogRepository<ProductType> ProductTypes { get; }
    IUnifiedCatalogRepository<ProjectExtensionType> ProjectExtensionTypes { get; }
    IUnifiedCatalogRepository<ProjectOriginType> ProjectOriginTypes { get; }
    IUnifiedCatalogRepository<ProjectState> ProjectStates { get; }
    IUnifiedCatalogRepository<ProjectType> ProjectTypes { get; }
    IUnifiedCatalogRepository<ResearchCategoryGroup> ResearchCategoryGroups { get; }
    IUnifiedCatalogRepository<ResearchCategoryType> ResearchCategoryTypes { get; }
    IUnifiedCatalogRepository<TransactionType> TransactionTypes { get; }
    IUnifiedCatalogRepository<VisitState> VisitStates { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Owns a clean scope aggregate transaction. Provision Identity beforehand. Throw to roll back.</summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
