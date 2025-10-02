using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Base;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface ICatalogRepository<T> : IGenericRepository<T> where T : CatalogEntityBase
    {
        Task<List<KeyValueItemDTO>> GetKeyValuesAsync(string? term = null, int? take = null, CancellationToken ct = default);
    }
}
