using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProductValueRepository : IGenericRepository<ProductValue>
    {
        Task<List<ProductValue>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<ProductValue?> GetByPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default);
        Task<bool> ExistsForPairAsync(int productId, int attributeDefinitionId, CancellationToken ct = default);

        IQueryable<ProductValue> QueryWithDefinition(bool asNoTracking = true);
    }
}