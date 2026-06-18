using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProductAttributeDefinitionRepository : IGenericRepository<ProductAttributeDefinition>
    {
        Task<List<ProductAttributeDefinition>> GetByTypeAsync(int productTypeId, CancellationToken ct = default);
        Task<bool> AnyValuesUsingDefinitionAsync(int definitionId, CancellationToken ct = default);

        IQueryable<ProductAttributeDefinition> QueryByType(int productTypeId, bool asNoTracking = true);
    }
}