using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedProductAuthorRepository : IGenericRepository<ProductAuthor>
{
    Task<List<ProductAuthor>> GetByProductAsync(int productId, CancellationToken ct = default);
    Task<bool> ExistsForAuthorAsync(int productId, int authorId, CancellationToken ct = default);
    IQueryable<ProductAuthor> QueryByProduct(int productId, bool asNoTracking = true);
}
