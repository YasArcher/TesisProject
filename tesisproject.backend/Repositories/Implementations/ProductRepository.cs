using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly AppDbContext _ctx;
        public ProductRepository(AppDbContext ctx) : base(ctx) => _ctx = ctx;

        public async Task<Product?> GetByIdWithRefsAsync(int productId, CancellationToken ct = default)
            => await _ctx.Set<Product>()
                .Where(p => p.Id == productId)
                .Include(p => p.ProductType)
                .Include(p => p.Values!).ThenInclude(v => v.AttributeDefinition)
                .Include(p => p.Authors!)
                .AsSplitQuery()
                .FirstOrDefaultAsync(ct);

        public async Task<List<Product>> GetByProjectAsync(int projectId, CancellationToken ct = default)
            => await _ctx.Set<Product>()
                .Where(p => p.ProjectId == projectId)
                .Include(p => p.ProductType)
                .AsNoTracking()
                .ToListAsync(ct);

        public IQueryable<Product> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Product>()
                .Include(p => p.ProductType)
                .Include(p => p.Values!).ThenInclude(v => v.AttributeDefinition)
                .Include(p => p.Authors!)
                .AsSplitQuery();

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
