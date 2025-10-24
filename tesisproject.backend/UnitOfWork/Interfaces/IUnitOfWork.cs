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

        // 🔹 Nuevo repositorio agregado:
        IAspNetUserRepository AspNetUsers { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
