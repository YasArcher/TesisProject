using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProductAttributeDefinitionRepository : IGenericRepository<ProductAttributeDefinition>
    {
        Task<List<ProductAttributeDefinition>> GetByTypeAsync(int productTypeId, CancellationToken ct = default);
        Task<bool> AnyValuesUsingDefinitionAsync(int definitionId, CancellationToken ct = default);

        IQueryable<ProductAttributeDefinition> QueryByType(int productTypeId, bool asNoTracking = true);
    }
}