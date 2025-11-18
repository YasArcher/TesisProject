using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProductValueRepository : IGenericRepository<ProductValue>
    {
        Task<List<ProductValue>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<ProductValue?> GetByPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default);
        Task<bool> ExistsForPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default);

        IQueryable<ProductValue> QueryWithDefinition(bool asNoTracking = true);
    }
}