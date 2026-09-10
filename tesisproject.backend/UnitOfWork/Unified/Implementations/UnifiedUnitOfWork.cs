using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;

namespace tesisproject.backend.UnitOfWork.Unified.Implementations;

/// <summary>
/// Coordinates repository instances created by dependency injection over one
/// <see cref="UnifiedDideDbContext"/> scope.
/// </summary>
public sealed class UnifiedUnitOfWork : IUnifiedUnitOfWork
{
    private readonly UnifiedDideDbContext _context;

    public IUnifiedArticleReadRepository ArticleReads { get; }
    public IUnifiedArticleRegistrationRepository ArticleRegistration { get; }
    public IUnifiedArticleConfigurationRepository ArticleConfiguration { get; }
    public IUnifiedArticleRegistrationMatrixRepository ArticleRegistrationMatrices { get; }
    public IGenericRepository<RegistrationMatrixColumn> RegistrationMatrixColumns { get; }
    public IGenericRepository<RegistrationMatrixRow> RegistrationMatrixRows { get; }
    public IGenericRepository<RegistrationMatrixCell> RegistrationMatrixCells { get; }
    public IGenericRepository<FieldCatalogEntry> ArticleFields { get; }

    public IUnifiedFacultyRepository Faculties { get; }
    public IUnifiedAcademicTermRepository AcademicTerms { get; }
    public IUnifiedAppConfigurationRepository AppConfigurations { get; }
    public IUnifiedAppUserRepository AppUsers { get; }
    public IUnifiedAuthorRepository Authors { get; }
    public IUnifiedBudgetRepository Budgets { get; }
    public IUnifiedConvocationRepository Convocations { get; }
    public IUnifiedDocumentRepository Documents { get; }
    public IUnifiedExportFieldRepository ExportFields { get; }
    public IUnifiedExportTemplateColumnRepository ExportTemplateColumns { get; }
    public IUnifiedExportTemplateRepository ExportTemplates { get; }
    public IUnifiedExternalResearcherProjectRepository ExternalResearcherProjects { get; }
    public IUnifiedExternalResearcherRepository ExternalResearchers { get; }
    public IUnifiedFacultyScopeFacultyRepository FacultyScopeFaculties { get; }
    public IUnifiedFacultyScopeRepository FacultyScopes { get; }
    public IUnifiedGroupMemberRepository GroupMembers { get; }
    public IUnifiedGroupRepository Groups { get; }
    public IUnifiedObjectiveActivityRepository ObjectiveActivities { get; }
    public IUnifiedObjectiveActivityUserRepository ObjectiveActivityUsers { get; }
    public IUnifiedProductAuthorRepository ProductAuthors { get; }
    public IUnifiedProductAttributeDefinitionRepository ProductAttributeDefinitions { get; }
    public IUnifiedProductRepository Products { get; }
    public IUnifiedProductValueRepository ProductValues { get; }
    public IUnifiedProjectDocumentRepository ProjectDocuments { get; }
    public IUnifiedProjectExtensionRepository ProjectExtensions { get; }
    public IUnifiedProjectObjectiveRepository ProjectObjectives { get; }
    public IUnifiedProjectRepository Projects { get; }
    public IUnifiedProjectResearchCategoryRepository ProjectResearchCategories { get; }
    public IUnifiedResearchCategoryRepository ResearchCategories { get; }
    public IUnifiedUserFacultyScopeAssignmentRepository UserFacultyScopeAssignments { get; }
    public IUnifiedVisitIssueRepository VisitIssues { get; }
    public IUnifiedVisitObjectiveActivityProgressRepository VisitObjectiveActivityProgresses { get; }
    public IUnifiedVisitRepository Visits { get; }

    public IUnifiedCatalogRepository<Country> Countries { get; }
    public IUnifiedCatalogRepository<DocumentType> DocumentTypes { get; }
    public IUnifiedCatalogRepository<FundingType> FundingTypes { get; }
    public IUnifiedCatalogRepository<IndexingSource> IndexingSources { get; }
    public IUnifiedCatalogRepository<Institution> Institutions { get; }
    public IUnifiedCatalogRepository<MemberRoleType> MemberRoleTypes { get; }
    public IUnifiedCatalogRepository<ObjectiveType> ObjectiveTypes { get; }
    public IUnifiedCatalogRepository<ProductAttribute> ProductAttributes { get; }
    public IUnifiedCatalogRepository<ProductType> ProductTypes { get; }
    public IUnifiedCatalogRepository<ProjectExtensionType> ProjectExtensionTypes { get; }
    public IUnifiedCatalogRepository<ProjectOriginType> ProjectOriginTypes { get; }
    public IUnifiedCatalogRepository<ProjectState> ProjectStates { get; }
    public IUnifiedCatalogRepository<ProjectType> ProjectTypes { get; }
    public IUnifiedCatalogRepository<ResearchCategoryGroup> ResearchCategoryGroups { get; }
    public IUnifiedCatalogRepository<ResearchCategoryType> ResearchCategoryTypes { get; }
    public IUnifiedCatalogRepository<TransactionType> TransactionTypes { get; }
    public IUnifiedCatalogRepository<VisitState> VisitStates { get; }

    public UnifiedUnitOfWork(
        UnifiedDideDbContext context,
        IUnifiedArticleReadRepository articleReads,
        IUnifiedArticleRegistrationRepository articleRegistration,
        IUnifiedArticleConfigurationRepository articleConfiguration,
        IUnifiedArticleRegistrationMatrixRepository articleRegistrationMatrices,
        IGenericRepository<RegistrationMatrixColumn> registrationMatrixColumns,
        IGenericRepository<RegistrationMatrixRow> registrationMatrixRows,
        IGenericRepository<RegistrationMatrixCell> registrationMatrixCells,
        IGenericRepository<FieldCatalogEntry> articleFields,
        IUnifiedFacultyRepository faculties,
        IUnifiedAcademicTermRepository academicTerms,
        IUnifiedAppConfigurationRepository appConfigurations,
        IUnifiedAppUserRepository appUsers,
        IUnifiedAuthorRepository authors,
        IUnifiedBudgetRepository budgets,
        IUnifiedConvocationRepository convocations,
        IUnifiedDocumentRepository documents,
        IUnifiedExportFieldRepository exportFields,
        IUnifiedExportTemplateColumnRepository exportTemplateColumns,
        IUnifiedExportTemplateRepository exportTemplates,
        IUnifiedExternalResearcherProjectRepository externalResearcherProjects,
        IUnifiedExternalResearcherRepository externalResearchers,
        IUnifiedFacultyScopeFacultyRepository facultyScopeFaculties,
        IUnifiedFacultyScopeRepository facultyScopes,
        IUnifiedGroupMemberRepository groupMembers,
        IUnifiedGroupRepository groups,
        IUnifiedObjectiveActivityRepository objectiveActivities,
        IUnifiedObjectiveActivityUserRepository objectiveActivityUsers,
        IUnifiedProductAuthorRepository productAuthors,
        IUnifiedProductAttributeDefinitionRepository productAttributeDefinitions,
        IUnifiedProductRepository products,
        IUnifiedProductValueRepository productValues,
        IUnifiedProjectDocumentRepository projectDocuments,
        IUnifiedProjectExtensionRepository projectExtensions,
        IUnifiedProjectObjectiveRepository projectObjectives,
        IUnifiedProjectRepository projects,
        IUnifiedProjectResearchCategoryRepository projectResearchCategories,
        IUnifiedResearchCategoryRepository researchCategories,
        IUnifiedUserFacultyScopeAssignmentRepository userFacultyScopeAssignments,
        IUnifiedVisitIssueRepository visitIssues,
        IUnifiedVisitObjectiveActivityProgressRepository visitObjectiveActivityProgresses,
        IUnifiedVisitRepository visits,
        IUnifiedCatalogRepository<Country> countries,
        IUnifiedCatalogRepository<DocumentType> documentTypes,
        IUnifiedCatalogRepository<FundingType> fundingTypes,
        IUnifiedCatalogRepository<IndexingSource> indexingSources,
        IUnifiedCatalogRepository<Institution> institutions,
        IUnifiedCatalogRepository<MemberRoleType> memberRoleTypes,
        IUnifiedCatalogRepository<ObjectiveType> objectiveTypes,
        IUnifiedCatalogRepository<ProductAttribute> productAttributes,
        IUnifiedCatalogRepository<ProductType> productTypes,
        IUnifiedCatalogRepository<ProjectExtensionType> projectExtensionTypes,
        IUnifiedCatalogRepository<ProjectOriginType> projectOriginTypes,
        IUnifiedCatalogRepository<ProjectState> projectStates,
        IUnifiedCatalogRepository<ProjectType> projectTypes,
        IUnifiedCatalogRepository<ResearchCategoryGroup> researchCategoryGroups,
        IUnifiedCatalogRepository<ResearchCategoryType> researchCategoryTypes,
        IUnifiedCatalogRepository<TransactionType> transactionTypes,
        IUnifiedCatalogRepository<VisitState> visitStates)
    {
        _context = context;
        ArticleReads = articleReads;
        ArticleRegistration = articleRegistration;
        ArticleConfiguration = articleConfiguration;
        ArticleRegistrationMatrices = articleRegistrationMatrices;
        RegistrationMatrixColumns = registrationMatrixColumns;
        RegistrationMatrixRows = registrationMatrixRows;
        RegistrationMatrixCells = registrationMatrixCells;
        ArticleFields = articleFields;
        Faculties = faculties;
        AcademicTerms = academicTerms;
        AppConfigurations = appConfigurations;
        AppUsers = appUsers;
        Authors = authors;
        Budgets = budgets;
        Convocations = convocations;
        Documents = documents;
        ExportFields = exportFields;
        ExportTemplateColumns = exportTemplateColumns;
        ExportTemplates = exportTemplates;
        ExternalResearcherProjects = externalResearcherProjects;
        ExternalResearchers = externalResearchers;
        FacultyScopeFaculties = facultyScopeFaculties;
        FacultyScopes = facultyScopes;
        GroupMembers = groupMembers;
        Groups = groups;
        ObjectiveActivities = objectiveActivities;
        ObjectiveActivityUsers = objectiveActivityUsers;
        ProductAuthors = productAuthors;
        ProductAttributeDefinitions = productAttributeDefinitions;
        Products = products;
        ProductValues = productValues;
        ProjectDocuments = projectDocuments;
        ProjectExtensions = projectExtensions;
        ProjectObjectives = projectObjectives;
        Projects = projects;
        ProjectResearchCategories = projectResearchCategories;
        ResearchCategories = researchCategories;
        UserFacultyScopeAssignments = userFacultyScopeAssignments;
        VisitIssues = visitIssues;
        VisitObjectiveActivityProgresses = visitObjectiveActivityProgresses;
        Visits = visits;
        Countries = countries;
        DocumentTypes = documentTypes;
        FundingTypes = fundingTypes;
        IndexingSources = indexingSources;
        Institutions = institutions;
        MemberRoleTypes = memberRoleTypes;
        ObjectiveTypes = objectiveTypes;
        ProductAttributes = productAttributes;
        ProductTypes = productTypes;
        ProjectExtensionTypes = projectExtensionTypes;
        ProjectOriginTypes = projectOriginTypes;
        ProjectStates = projectStates;
        ProjectTypes = projectTypes;
        ResearchCategoryGroups = researchCategoryGroups;
        ResearchCategoryTypes = researchCategoryTypes;
        TransactionTypes = transactionTypes;
        VisitStates = visitStates;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        if (_context.ChangeTracker.HasChanges() || _context.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("The operation requires a clean, independent UnitOfWork scope.");
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            finally { _context.ChangeTracker.Clear(); }
            throw;
        }
    }

    // The scoped DI container owns the injected context and disposes it once.
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
