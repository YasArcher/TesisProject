using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProductTypeRepository : IGenericRepository<ProductType>
    {
        Task<ProductType?> GetByIdWithDefinitionsAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
        Task<List<ProductAttributeDefinition>> GetDefinitionsByTypeAsync(int productTypeId, CancellationToken ct = default);

        IQueryable<ProductType> QueryWithDefinitions(bool asNoTracking = true);
    }
}