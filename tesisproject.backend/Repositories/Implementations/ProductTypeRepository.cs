using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProductTypeRepository : GenericRepository<ProductType>, IProductTypeRepository
    {
        private new readonly AppDbContext _ctx;
        public ProductTypeRepository(AppDbContext ctx) : base(ctx) => _ctx = ctx;

        public async Task<ProductType?> GetByIdWithDefinitionsAsync(int id, CancellationToken ct = default)
            => await _ctx.Set<ProductType>()
                .Where(t => t.Id == id)
                .Include(t => _ctx.Set<ProductAttributeDefinition>()
                                  .Where(d => d.ProductTypeId == id)) // Include de tipo Filtered vía query
                .AsSplitQuery()
                .FirstOrDefaultAsync(ct);

        public async Task<bool> ExistsNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
            => await _ctx.Set<ProductType>()
                .AnyAsync(t => t.Name == name && (excludeId == null || t.Id != excludeId), ct);

        public async Task<List<ProductAttributeDefinition>> GetDefinitionsByTypeAsync(int productTypeId, CancellationToken ct = default)
            => await _ctx.Set<ProductAttributeDefinition>()
                .Where(d => d.ProductTypeId == productTypeId)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync(ct);

        public IQueryable<ProductType> QueryWithDefinitions(bool asNoTracking = true)
        {
            var q = _ctx.Set<ProductType>()
                .Include(t => _ctx.Set<ProductAttributeDefinition>()
                                  .Where(d => d.ProductTypeId == t.Id))
                .AsSplitQuery();

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
