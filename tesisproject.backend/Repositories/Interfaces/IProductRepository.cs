using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetByIdWithRefsAsync(int productId, CancellationToken ct = default);
        Task<List<Product>> GetByProjectAsync(int projectId, CancellationToken ct = default);

        IQueryable<Product> QueryWithRefs(bool asNoTracking = true);
    }
}
