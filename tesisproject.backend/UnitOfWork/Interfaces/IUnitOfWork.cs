using System.Threading;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IProjectRepository Projects { get; }
        IGroupRepository Groups { get; }
        IGroupMemberRepository GroupMembers { get; }
        IBudgetRepository Budgets { get; }
        IVisitRepository Visits { get; }
        IProjectExtensionRepository ProjectExtensions { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
