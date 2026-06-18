using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProductAttributeDefinitionClientService
    {
        Task<HttpResponseWrapper<List<ProductAttributeDefinitionListItemDTO>?>> GetByProductTypeAsync(
            int productTypeId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> CreateAsync(
            AddProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> UpdateAsync(
            int id,
            UpdateProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);

    }
}