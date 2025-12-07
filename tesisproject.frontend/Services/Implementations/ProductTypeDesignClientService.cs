using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProductTypeDesignClientService : IProductTypeDesignClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/producttypedesign";

        public ProductTypeDesignClientService(IApiClient api)
        {
            _api = api;
        }

        // ==========================================
        //             GET TEMPLATE (NEW)
        // ==========================================

        public Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> GetTemplateAsync(
            CancellationToken ct = default)
        {
            // GET: api/producttypedesign
            return _api.GetAsync<ProductTypeDesignDetailDTO>(_baseUrl, ct);
        }

        // ==========================================
        //         GET BY PRODUCT TYPE ID
        // ==========================================

        public Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> GetByProductTypeIdAsync(
            int productTypeId,
            CancellationToken ct = default)
        {
            if (productTypeId <= 0)
                throw new ArgumentException("ProductTypeId must be a positive value.", nameof(productTypeId));

            // GET: api/producttypedesign/{productTypeId}
            var url = $"{_baseUrl}/{productTypeId}";
            return _api.GetAsync<ProductTypeDesignDetailDTO>(url, ct);
        }

        // ==========================================
        //                 SAVE (UPSERT)
        // ==========================================

        public Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> SaveAsync(
            SaveProductTypeDesignRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/producttypedesign
            // Backend decide: Id == 0 -> create, Id > 0 -> update
            return _api.PostAsync<SaveProductTypeDesignRequestDTO, ProductTypeDesignDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }
    }
}