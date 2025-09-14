using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace tesisproject.backend.Repositories.Implementations
{
    public class ArticlesRepository : IArticlesRepository
    {
        private readonly AppDbContext _db;
        public ArticlesRepository(AppDbContext db) => _db = db;

        public Task<List<Article>> GetAllAsync(CancellationToken ct) =>
            _db.Articles.Include(a => a.Participantes.OrderBy(p => p.Index))
                        .OrderByDescending(a => a.CreatedAt)
                        .ToListAsync(ct);

        public Task<Article?> GetByIdAsync(int id, CancellationToken ct) =>
            _db.Articles.Include(a => a.Participantes)
                        .FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task AddAsync(Article entity, CancellationToken ct) =>
            await _db.Articles.AddAsync(entity, ct);

        public void Remove(Article entity) => _db.Articles.Remove(entity);
    }
}
