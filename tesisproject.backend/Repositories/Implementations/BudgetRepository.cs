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

        public async Task<List<Budget>> GetByProjectIdAsync(int projectId, bool includeTransactions = false, CancellationToken ct = default)
        {
            var q = _set.AsQueryable();

            if (includeTransactions)
                q = q.Include(b => b.Transactions);

            return await q.AsNoTracking().Where(b => b.ProjectId == projectId).ToListAsync(ct);
        }


        public async Task<bool> ExistsForProjectAsync(int projectId, CancellationToken ct = default)
            => await _set.AnyAsync(b => b.ProjectId == projectId, ct);

        // Transactions
        public async Task AddTransactionAsync(BudgetTransaction tx, CancellationToken ct = default)
    => await _ctx.Set<BudgetTransaction>().AddAsync(tx, ct);

        public async Task<BudgetTransaction?> GetTransactionByIdAsync(int txId, CancellationToken ct = default)
            => await _ctx.Set<BudgetTransaction>()
                         .Include(t => t.TransactionType) // 👈 necesario
                         .FirstOrDefaultAsync(t => t.BudgetTransactionId == txId, ct);


        public async Task<int?> FindBudgetIdByTransactionAsync(int txId, CancellationToken ct = default)
            => await _ctx.Set<BudgetTransaction>()
                         .Where(t => t.BudgetTransactionId == txId)
                         .Select(t => (int?)t.BudgetId)
                         .FirstOrDefaultAsync(ct);


        public async Task<List<BudgetTransaction>> GetTransactionsAsync(int budgetId, CancellationToken ct = default)
            => await _ctx.Set<BudgetTransaction>()
                        .AsNoTracking()
                        .Where(t => t.BudgetId == budgetId)
                        .OrderByDescending(t => t.BudgetTransactionId)
                        .ToListAsync(ct);
        public void UpdateTransaction(BudgetTransaction tx)
    => _ctx.Set<BudgetTransaction>().Update(tx);
    }
}