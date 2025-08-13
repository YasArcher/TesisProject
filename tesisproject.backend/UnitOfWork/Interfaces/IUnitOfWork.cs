using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IProjectRepository Projects { get; }  // agrega aquí más repos a futuro
        Task<int> SaveChangesAsync();
    }
}
