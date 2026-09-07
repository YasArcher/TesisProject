using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedProductAuthorRepository
    : GenericRepository<ProductAuthor>, IUnifiedProductAuthorRepository
{
    public UnifiedProductAuthorRepository(UnifiedDideDbContext context) : base(context) { }

    public async Task<List<ProductAuthor>> GetByProductAsync(
        int productId,
        CancellationToken ct = default) =>
        await _db
            .Where(productAuthor => productAuthor.ProductId == productId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> ExistsForAuthorAsync(
        int productId,
        int authorId,
        CancellationToken ct = default) =>
        await _db.AnyAsync(
            productAuthor => productAuthor.ProductId == productId &&
                             productAuthor.AuthorId == authorId,
            ct);

    public IQueryable<ProductAuthor> QueryByProduct(
        int productId,
        bool asNoTracking = true)
    {
        var query = _db.Where(productAuthor => productAuthor.ProductId == productId);
        return asNoTracking ? query.AsNoTracking() : query;
    }
}
