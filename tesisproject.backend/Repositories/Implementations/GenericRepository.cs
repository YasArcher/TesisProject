using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _ctx;
        protected readonly DbSet<T> _db;

        public GenericRepository(AppDbContext ctx) : this((DbContext)ctx) { }

        protected GenericRepository(DbContext ctx)
        {
            _ctx = ctx;
            _db = _ctx.Set<T>();
        }

        public async Task<T?> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
            => await _db.FindAsync(keyValues, ct);

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
        {
            IQueryable<T> q = _db;
            if (filter is not null) q = q.Where(filter);
            return await q.AsNoTracking().ToListAsync(ct);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await _db.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await _db.AsNoTracking().AnyAsync(predicate, ct);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => predicate is null
                ? await _db.AsNoTracking().CountAsync(ct)
                : await _db.AsNoTracking().CountAsync(predicate, ct);

        public async Task AddAsync(T entity, CancellationToken ct = default)
            => await _db.AddAsync(entity, ct);

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
            => await _db.AddRangeAsync(entities, ct);

        public void Update(T entity) => _db.Update(entity);
        public void Remove(T entity) => _db.Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) => _db.RemoveRange(entities);

        public IQueryable<T> Query(bool asNoTracking = true)
            => asNoTracking ? _db.AsNoTracking() : _db;
    }
}
