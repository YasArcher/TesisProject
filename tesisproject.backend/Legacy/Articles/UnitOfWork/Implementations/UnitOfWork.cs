using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;

namespace tesisproject.backend.UnitOfWork.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public IArticlesRepository Articles { get; }

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            Articles = new ArticlesRepository(db);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }
}
