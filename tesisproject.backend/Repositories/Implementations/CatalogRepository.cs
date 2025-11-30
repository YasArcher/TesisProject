using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Base;

namespace tesisproject.backend.Repositories.Implementations
{
    public class CatalogRepository<T> : GenericRepository<T>, ICatalogRepository<T>
        where T : CatalogEntityBase
    {
        public CatalogRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<IReadOnlyList<T>> ListAsync(
            bool onlyActives = true,
            Expression<Func<T, bool>>? where = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            IQueryable<T> q = _db;

            if (onlyActives)
                q = q.Where(e => e.IsActive);

            if (where is not null)
                q = q.Where(where);

            if (include is not null)
                q = include(q);

            return await q.AsNoTracking()
                          .OrderBy(e => e.Name)
                          .ToListAsync();
        }

        public async Task<Dictionary<int, T>> GetByIdsAsync(
            IEnumerable<int> ids,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            var keys = ids?.Distinct().ToList() ?? [];
            if (keys.Count == 0) return new();

            IQueryable<T> q = _db.Where(e => keys.Contains(e.Id));

            if (include is not null)
                q = include(q);

            var list = await q.AsNoTracking().ToListAsync(ct);
            return list.ToDictionary(e => e.Id, e => e);
        }

        public async Task<T?> GetByNameAsync(
            string name,
            bool onlyActives = true,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            var n = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n)) return null;

            IQueryable<T> q = _db;

            if (onlyActives)
                q = q.Where(e => e.IsActive);

            if (include is not null)
                q = include(q);

            return await q.AsNoTracking()
                          .FirstOrDefaultAsync(e => e.Name == n, ct);
        }

        public async Task<bool> NameExistsAsync(
            string name,
            int? excludeId = null,
            CancellationToken ct = default)
        {
            var n = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n)) return false;

            var q = _db.Where(e => e.Name == n);

            if (excludeId.HasValue)
                q = q.Where(e => e.Id != excludeId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<List<KeyValueItemDTO>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var q = _db.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));

            if (take is > 0)
                q = q.Take(take.Value);

            return await q.OrderBy(x => x.Name)
                          .Select(x => new KeyValueItemDTO { Id = x.Id, Name = x.Name })
                          .ToListAsync(ct);
        }
    }
}
