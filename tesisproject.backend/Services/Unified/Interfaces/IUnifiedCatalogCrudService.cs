using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    /// <summary>
    /// Generic CRUD service for catalog entities (CatalogEntityBase).
    /// Uses Common Catalog DTOs (AddCatalogRequestDTO, UpdateCatalogRequestDTO,
    /// CatalogListItemDTO and CatalogDetailDTO).
    /// </summary>
    public interface IUnifiedCatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase
    {
        Task<ServiceResult<IReadOnlyList<CatalogListItemDTO>>> ListAsync(
            CancellationToken ct = default);

        Task<ServiceResult<CatalogDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<CatalogDetailDTO>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<CatalogDetailDTO>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}
