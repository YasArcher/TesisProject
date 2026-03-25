using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IArticlesRepository Articles { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
