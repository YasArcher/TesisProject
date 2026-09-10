using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProductAttributeDefinitionService
    {
        // Lista las definiciones para un tipo de producto (para construir el formulario)
        Task<ServiceResult<IReadOnlyList<ProductAttributeDefinitionListItemDTO>>> ListByProductTypeAsync(
            int productTypeId,
            CancellationToken ct = default);

        // CRUD básico
        Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> CreateAsync(
            AddProductAttributeDefinitionRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> UpdateAsync(
            UpdateProductAttributeDefinitionRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}