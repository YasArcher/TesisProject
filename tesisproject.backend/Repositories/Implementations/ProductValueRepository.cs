using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProductValueRepository : GenericRepository<ProductValue>, IProductValueRepository
    {
        private readonly AppDbContext _ctx;
        public ProductValueRepository(AppDbContext ctx) : base(ctx) => _ctx = ctx;

        public async Task<List<ProductValue>> GetByProductAsync(int productId, CancellationToken ct = default)
            => await _ctx.Set<ProductValue>()
                .Where(v => v.ProductId == productId)
                .Include(v => v.AttributeDefinition)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<ProductValue?> GetByPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default)
            => await _ctx.Set<ProductValue>()
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.AttributeDefinitionId == attributeDefinitionId, ct);

        public async Task<bool> ExistsForPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default)
            => await _ctx.Set<ProductValue>()
                .AnyAsync(v => v.ProductId == productId && v.AttributeDefinitionId == attributeDefinitionId, ct);

        public IQueryable<ProductValue> QueryWithDefinition(bool asNoTracking = true)
        {
            var q = _ctx.Set<ProductValue>()
                .Include(v => v.AttributeDefinition);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}