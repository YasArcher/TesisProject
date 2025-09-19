using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class BudgetRepository : IBudgetRepository
    {
        private readonly AppDbContext _ctx;
        private readonly DbSet<Budget> _set;

        public BudgetRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _set = _ctx.Set<Budget>();
        }

        public async Task<Budget?> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
            => await _set.FindAsync(keyValues, ct);

        public IQueryable<Budget> Query(bool asNoTracking = true)
            => asNoTracking ? _set.AsNoTracking() : _set.AsQueryable();

        public async Task<List<Budget>> GetAllAsync(System.Linq.Expressions.Expression<Func<Budget, bool>>? filter = null, CancellationToken ct = default)
        {
            var q = Query(true);
            if (filter is not null) q = q.Where(filter);
            return await q.ToListAsync(ct);
        }

        public async Task<Budget?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Budget, bool>> predicate, CancellationToken ct = default)
            => await Query(true).FirstOrDefaultAsync(predicate, ct);

        public async Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<Budget, bool>> predicate, CancellationToken ct = default)
            => await _set.AnyAsync(predicate, ct);

        public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Budget, bool>>? predicate = null, CancellationToken ct = default)
        {
            var q = _set.AsQueryable();
            if (predicate is not null) q = q.Where(predicate);
            return await q.CountAsync(ct);
        }

        public async Task AddAsync(Budget entity, CancellationToken ct = default)
            => await _set.AddAsync(entity, ct);

        public async Task AddRangeAsync(IEnumerable<Budget> entities, CancellationToken ct = default)
            => await _set.AddRangeAsync(entities, ct);

        public void Update(Budget entity) => _set.Update(entity);
        public void Remove(Budget entity) => _set.Remove(entity);
        public void RemoveRange(IEnumerable<Budget> entities) => _set.RemoveRange(entities);

        // Específicos
        public async Task<Budget?> GetDetailAsync(int budgetId, bool includeTransactions = false, CancellationToken ct = default)
        {
            var q = _set.AsQueryable();
            if (includeTransactions) q = q.Include(b => b.Transactions);
            return await q.AsNoTracking().FirstOrDefaultAsync(b => b.BudgetId == budgetId, ct);
        }

        public async Task<Budget?> GetByProjectIdAsync(int projectId, bool includeTransactions = false, CancellationToken ct = default)
        {
            var q = _set.AsQueryable();
            if (includeTransactions) q = q.Include(b => b.Transactions);
            return await q.AsNoTracking().FirstOrDefaultAsync(b => b.ProjectId == projectId, ct);
        }

        public async Task<bool> ExistsForProjectAsync(int projectId, CancellationToken ct = default)
            => await _set.AnyAsync(b => b.ProjectId == projectId, ct);
    }
}