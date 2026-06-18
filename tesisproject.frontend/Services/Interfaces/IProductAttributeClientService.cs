using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProductAttributeClientService
    {
        Task<HttpResponseWrapper<List<ProductAttributeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> CreateAsync(
            AddProductAttributeRequestDTO dto,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> UpdateAsync(
            int id,
            UpdateProductAttributeRequestDTO dto,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
    int id,
    CancellationToken ct = default);
    }
}