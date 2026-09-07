using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProductAttributeService
    {
        Task<ServiceResult<IReadOnlyList<ProductAttributeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<ProductAttributeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ProductAttributeDetailDTO>> CreateAsync(
            AddProductAttributeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ProductAttributeDetailDTO>> UpdateAsync(
            UpdateProductAttributeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}