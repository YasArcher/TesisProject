using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IArticlesRepository
    {
        Task<List<Article>> GetAllAsync(CancellationToken ct);
        Task<Article?> GetByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Article entity, CancellationToken ct);
        void Remove(Article entity);
    }
}
