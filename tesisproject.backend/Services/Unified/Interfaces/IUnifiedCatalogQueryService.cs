using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedCatalogQueryService
    {
        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync<TCatalog>(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
            where TCatalog : CatalogEntityBase;
    }
}
