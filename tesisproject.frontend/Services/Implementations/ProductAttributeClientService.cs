using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProductAttributeClientService : IProductAttributeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/productattributes";

        public ProductAttributeClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<ProductAttributeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/productattributes?onlyActives=true
            return _api.GetAsync<List<ProductAttributeListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/productattributes/{id}
            return _api.GetAsync<ProductAttributeDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> CreateAsync(
            AddProductAttributeRequestDTO dto,
            CancellationToken ct = default)
        {
            // POST: api/productattributes
            return _api.PostAsync<AddProductAttributeRequestDTO, ProductAttributeDetailDTO>(
                _baseUrl,
                dto,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ProductAttributeDetailDTO?>> UpdateAsync(
            int id,
            UpdateProductAttributeRequestDTO dto,
            CancellationToken ct = default)
        {
            // PUT: api/productattributes/{id}
            return _api.PutAsync<UpdateProductAttributeRequestDTO, ProductAttributeDetailDTO>(
                $"{_baseUrl}/{id}",
                dto,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            // DELETE: api/productattributes/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}