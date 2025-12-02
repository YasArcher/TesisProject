using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

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
        IProductTypeRepository ProductTypes { get; }
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

        // 🔹 Nuevo repositorio agregado:
        IAspNetUserRepository AspNetUsers { get; }
        IAppUserRepository AppUsers { get; }


        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
