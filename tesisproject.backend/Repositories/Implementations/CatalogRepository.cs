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

        public async Task<List<KeyValueItemDTO>> GetKeyValuesAsync(string? term = null, int? take = null, CancellationToken ct = default)
        {
            var q = _db.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));

            if (take.HasValue && take.Value > 0)
                q = q.Take(take.Value);

            return await q
                .OrderBy(x => x.Name)
                .Select(x => new KeyValueItemDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }
    }
}
