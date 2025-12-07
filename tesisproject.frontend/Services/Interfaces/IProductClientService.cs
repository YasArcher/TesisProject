using tesisproject.shared.DTOs.Product.Request;
using tesisproject.shared.DTOs.Product.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProductClientService
    {
        Task<HttpResponseWrapper<List<ProductListItemResponseDTO>?>> ListAsync(
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductDetailResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ProductListItemResponseDTO>?>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductDetailResponseDTO?>> CreateAsync(
            ProductCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProductDetailResponseDTO?>> UpdateAsync(
            ProductUpdateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}