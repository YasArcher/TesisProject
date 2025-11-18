using tesisproject.shared.DTOs.Catalog.ProductType.Request;
using tesisproject.shared.DTOs.Catalog.ProductType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProductTypeService
    {
        // ProductType (solo tipo)
        Task<ServiceResult<ProductTypeListItemResponseDTO>> CreateAsync(ProductTypeCreateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProductTypeListItemResponseDTO>> UpdateAsync(ProductTypeUpdateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProductTypeListItemResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ProductTypeListItemResponseDTO>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);

        // ProductType + Definitions (metamodelo)
        Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> CreateWithDefinitionsAsync(ProductTypeWithDefinitionsCreateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> GetByIdWithDefinitionsAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> UpdateWithDefinitionsAsync(ProductTypeWithDefinitionsUpdateRequestDTO request, CancellationToken ct = default);
    }
}