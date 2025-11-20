using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;

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
        IObjectiveTypeRepository ObjectiveTypes { get; }
        IProjectObjectiveRepository ProjectObjectives { get; }
        IObjectiveActivityRepository ObjectiveActivities { get; }
        IObjectiveActivityUserRepository ObjectiveActivityUsers { get; }
        IDocumentRepository Documents { get; }
        IMemberRoleTypeRepository MemberRoleTypeRepository { get; }
        IProjectTypeRepository ProjectTypeRepository { get; }
        IDocumentTypeRepository DocumentTypes { get; }

        // 🔹 Nuevo repositorio agregado:
        IAspNetUserRepository AspNetUsers { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
