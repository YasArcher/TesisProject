using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProductAttributeDefinitionClientService : IProductAttributeDefinitionClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/productattributedefinitions";

        public ProductAttributeDefinitionClientService(IApiClient api)
            => _api = api;

        // =========================
        //     LIST BY PRODUCT TYPE
        // =========================

        // GET: api/productattributedefinitions/by-product-type/{productTypeId}
        public Task<HttpResponseWrapper<List<ProductAttributeDefinitionListItemDTO>?>> GetByProductTypeAsync(
            int productTypeId,
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ProductAttributeDefinitionListItemDTO>>(
                $"{_baseUrl}/by-product-type/{productTypeId}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        // GET: api/productattributedefinitions/{id}
        public Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            return _api.GetAsync<ProductAttributeDefinitionDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        // POST: api/productattributedefinitions
        public Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> CreateAsync(
            AddProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct = default)
        {
            return _api.PostAsync<AddProductAttributeDefinitionRequestDTO, ProductAttributeDefinitionDetailDTO>(
                _baseUrl,
                dto,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        // PUT: api/productattributedefinitions/{id}
        public Task<HttpResponseWrapper<ProductAttributeDefinitionDetailDTO?>> UpdateAsync(
            int id,
            UpdateProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct = default)
        {
            return _api.PutAsync<UpdateProductAttributeDefinitionRequestDTO, ProductAttributeDefinitionDetailDTO>(
                $"{_baseUrl}/{id}",
                dto,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================

        // DELETE: api/productattributedefinitions/{id}
        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}