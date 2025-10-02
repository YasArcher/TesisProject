using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface ICatalogQueryService
    {
        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync<TCatalog>(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
            where TCatalog : CatalogEntityBase;
    }
}
