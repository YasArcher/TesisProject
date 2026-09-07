using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetByIdWithRefsAsync(int productId, CancellationToken ct = default);
    Task<List<Product>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    IQueryable<Product> QueryWithRefs(bool asNoTracking = true);
}
