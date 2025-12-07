using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProductTypeDesignClientService
    {
        /// <summary>
        /// GET: api/producttypedesign
        /// Devuelve el template para crear un nuevo ProductType (sin Id).
        /// </summary>
        Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> GetTemplateAsync(
            CancellationToken ct = default);

        /// <summary>
        /// GET: api/producttypedesign/{productTypeId}
        /// Devuelve el diseño completo de un ProductType existente.
        /// </summary>
        Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> GetByProductTypeIdAsync(
            int productTypeId,
            CancellationToken ct = default);

        /// <summary>
        /// POST: api/producttypedesign
        /// Upsert:
        ///  - Id == 0 => crea nuevo ProductType + diseño
        ///  - Id > 0  => actualiza ProductType + diseño
        /// </summary>
        Task<HttpResponseWrapper<ProductTypeDesignDetailDTO?>> SaveAsync(
            SaveProductTypeDesignRequestDTO request,
            CancellationToken ct = default);
    }
}