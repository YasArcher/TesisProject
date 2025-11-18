using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProductAuthorRepository : GenericRepository<ProductAuthor>, IProductAuthorRepository
    {
        private readonly AppDbContext _ctx;
        public ProductAuthorRepository(AppDbContext ctx) : base(ctx) => _ctx = ctx;

        public async Task<List<ProductAuthor>> GetByProductAsync(int productId, CancellationToken ct = default)
            => await _ctx.Set<ProductAuthor>()
                .Where(a => a.ProductId == productId)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<bool> ExistsForUserAsync(int productId, int userId, CancellationToken ct = default)
            => await _ctx.Set<ProductAuthor>()
                .AnyAsync(a => a.ProductId == productId && a.UserId == userId, ct);

        public IQueryable<ProductAuthor> QueryByProduct(int productId, bool asNoTracking = true)
        {
            var q = _ctx.Set<ProductAuthor>().Where(a => a.ProductId == productId);
            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}