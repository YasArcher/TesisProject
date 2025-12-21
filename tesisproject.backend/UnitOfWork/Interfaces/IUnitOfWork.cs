using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Repositorios específicos del dominio
        IProjectRepository Projects { get; }
        IGroupRepository Groups { get; }
        IGroupMemberRepository GroupMembers { get; }
        IBudgetRepository Budgets { get; }
        IVisitRepository Visits { get; }
        IProjectExtensionRepository ProjectExtensions { get; }
        IVisitIssueRepository VisitIssues { get; }
        IConvocationRepository Convocations { get; }
        IProductAttributeDefinitionRepository ProductAttributeDefinitions { get; }
        IProductValueRepository ProductValues { get; }
        IProductAuthorRepository ProductAuthors { get; }
        IProductRepository Products { get; }
        ICatalogRepository<ObjectiveType> ObjectiveTypes { get; }
        IProjectObjectiveRepository ProjectObjectives { get; }
        IObjectiveActivityRepository ObjectiveActivities { get; }
        IObjectiveActivityUserRepository ObjectiveActivityUsers { get; }
        IDocumentRepository Documents { get; }
        ICatalogRepository<MemberRoleType> MemberRoleTypeRepository { get; }
        ICatalogRepository<ProjectType> ProjectTypeRepository { get; }
        ICatalogRepository<DocumentType> DocumentTypes { get; }
        IProjectResearchCategoryRepository ProjectResearchCategories { get; }
        IResearchCategoryRepository ResearchCategories { get; }
        ICatalogRepository<ResearchCategoryType> ResearchCategoryTypes { get; }
        ICatalogRepository<FundingType> FundingTypes { get; }
        ICatalogRepository<ResearchCategoryGroup> ResearchCategoryGroups { get; }
        ICatalogRepository<TransactionType> TransactionTypes { get; }
        IExternalResearcherRepository ExternalResearchers { get; }
        ICatalogRepository<Country> Countries { get; }
        ICatalogRepository<Institution> Institutions { get; }
        IExternalResearcherProjectRepository ExternalResearcherProjects { get; }
        ICatalogRepository<IndexingSource> IndexingSources { get; }
        ICatalogRepository<ProductType> ProductTypes { get; }
        ICatalogRepository<ProductAttribute> ProductAttributes { get; }
        IProjectDocumentRepository ProjectDocuments { get; }
        ICatalogRepository<ProjectState> ProjectStates { get; }
        ICatalogRepository<VisitState> VisitStates { get; }
        IExportTemplateColumnRepository ExportTemplateColumns { get; }
        IExportTemplateRepository ExportTemplates { get; }
        IExportFieldRepository ExportFields { get; }

        // 🔹 Nuevo repositorio agregado:
        IAspNetUserRepository AspNetUsers { get; }
        IAppUserRepository AppUsers { get; }


        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
