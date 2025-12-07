using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Product.Request;
using tesisproject.shared.DTOs.Product.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProductClientService : IProductClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/products";

        public ProductClientService(IApiClient api)
        {
            _api = api;
        }

        public Task<HttpResponseWrapper<List<ProductListItemResponseDTO>?>> ListAsync(
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ProductListItemResponseDTO>>(_baseUrl, ct);
        }

        public Task<HttpResponseWrapper<ProductDetailResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            return _api.GetAsync<ProductDetailResponseDTO>($"{_baseUrl}/{id}", ct);
        }

        public Task<HttpResponseWrapper<List<ProductListItemResponseDTO>?>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                throw new ArgumentException("ProjectId must be a positive value.", nameof(projectId));

            var url = $"{_baseUrl}/by-project/{projectId}";
            return _api.GetAsync<List<ProductListItemResponseDTO>>(url, ct);
        }

        public Task<HttpResponseWrapper<ProductDetailResponseDTO?>> CreateAsync(
            ProductCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return _api.PostAsync<ProductCreateRequestDTO, ProductDetailResponseDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<ProductDetailResponseDTO?>> UpdateAsync(
            ProductUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            var url = $"{_baseUrl}/{request.Id}";
            return _api.PutAsync<ProductUpdateRequestDTO, ProductDetailResponseDTO>(
                url,
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            var url = $"{_baseUrl}/{id}";
            return _api.DeleteAsync(url, ct); // <-- aquí ya sin genérico
        }
    }
}