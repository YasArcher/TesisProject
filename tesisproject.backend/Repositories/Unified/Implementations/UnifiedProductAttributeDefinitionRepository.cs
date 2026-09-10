using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedProductAttributeDefinitionRepository
        : GenericRepository<ProductAttributeDefinition>, IUnifiedProductAttributeDefinitionRepository
    {
        public UnifiedProductAttributeDefinitionRepository(UnifiedDideDbContext ctx) : base(ctx) { }

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
