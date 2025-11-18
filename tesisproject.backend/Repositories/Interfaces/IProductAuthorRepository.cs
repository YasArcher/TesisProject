using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProductAuthorRepository : IGenericRepository<ProductAuthor>
    {
        Task<List<ProductAuthor>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<bool> ExistsForUserAsync(int productId, int userId, CancellationToken ct = default);

        IQueryable<ProductAuthor> QueryByProduct(int productId, bool asNoTracking = true);
    }
}