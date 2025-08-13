using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _ctx;
        protected readonly DbSet<T> _db;

        public GenericRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _db = _ctx.Set<T>(); // << Aquí EF resuelve el DbSet del T concreto
        }

        public async Task<T?> GetByIdAsync(object id) => await _db.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> q = _db;
            if (filter != null) q = q.Where(filter);
            return await q.AsNoTracking().ToListAsync();
        }

        public async Task AddAsync(T entity) => await _db.AddAsync(entity);
        public void Update(T entity) => _db.Update(entity);
        public void Remove(T entity) => _db.Remove(entity);
    }
}
