using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedProductRepository
    : GenericRepository<Product>, IUnifiedProductRepository
{
    public UnifiedProductRepository(UnifiedDideDbContext context) : base(context) { }

    public async Task<Product?> GetByIdWithRefsAsync(
        int productId,
        CancellationToken ct = default) =>
        await BuildQueryWithRefs()
            .FirstOrDefaultAsync(product => product.Id == productId, ct);

    public async Task<List<Product>> GetByProjectAsync(
        int projectId,
        CancellationToken ct = default) =>
        await _db
            .Where(product => product.ProjectId == projectId)
            .Include(product => product.ProductType)
            .AsNoTracking()
            .ToListAsync(ct);

    public IQueryable<Product> QueryWithRefs(bool asNoTracking = true)
    {
        var query = BuildQueryWithRefs();
        return asNoTracking ? query.AsNoTracking() : query;
    }

    private IQueryable<Product> BuildQueryWithRefs() =>
        _db
            .Include(product => product.ProductType)
            .Include(product => product.Values!)
                .ThenInclude(value => value.AttributeDefinition!)
                .ThenInclude(definition => definition.ProductAttribute)
            .Include(product => product.Authors!)
                .ThenInclude(productAuthor => productAuthor.Author)
                .ThenInclude(author => author.AppUser)
            .Include(product => product.Authors!)
                .ThenInclude(productAuthor => productAuthor.Author)
                .ThenInclude(author => author.ExternalResearcher)
            .AsSplitQuery();
}
