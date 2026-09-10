using tesisproject.shared.DTOs.Products.Product.Request;
using tesisproject.shared.DTOs.Products.Product.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProductService
    {
        Task<ServiceResult<ProductDetailResponseDTO>> CreateAsync(ProductCreateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProductDetailResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<ProductDetailResponseDTO>> UpdateAsync(ProductUpdateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
