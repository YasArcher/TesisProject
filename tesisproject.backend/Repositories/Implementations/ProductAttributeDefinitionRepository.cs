using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProductAttributeDefinitionRepository
        : GenericRepository<ProductAttributeDefinition>, IProductAttributeDefinitionRepository
    {
        private readonly AppDbContext _ctx;
        public ProductAttributeDefinitionRepository(AppDbContext ctx) : base(ctx) => _ctx = ctx;

        public async Task<List<ProductAttributeDefinition>> GetByTypeAsync(int productTypeId, CancellationToken ct = default)
            => await _ctx.Set<ProductAttributeDefinition>()
                .Include(d => d.ProductAttribute)
                .Where(d => d.ProductTypeId == productTypeId)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync(ct);

        public async Task<bool> AnyValuesUsingDefinitionAsync(int definitionId, CancellationToken ct = default)
            => await _ctx.Set<ProductValue>()
                .AnyAsync(v => v.AttributeDefinitionId == definitionId, ct);

        public IQueryable<ProductAttributeDefinition> QueryByType(int productTypeId, bool asNoTracking = true)
        {
            var q = _ctx.Set<ProductAttributeDefinition>()
                .Where(d => d.ProductTypeId == productTypeId);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}