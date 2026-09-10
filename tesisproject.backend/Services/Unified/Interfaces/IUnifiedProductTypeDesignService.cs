using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProductTypeDesignService
    {
        /// <summary>
        /// Returns all data needed to design a product type.
        /// If productTypeId is null, returns a template for creation.
        /// </summary>
        Task<ServiceResult<ProductTypeDesignDetailDTO>> GetDesignAsync(
            int? productTypeId,
            CancellationToken ct = default);

        /// <summary>
        /// Creates or updates a product type together with its attribute definitions.
        /// </summary>
        Task<ServiceResult<ProductTypeDesignDetailDTO>> SaveDesignAsync(
            SaveProductTypeDesignRequestDTO request,
            CancellationToken ct = default);
    }

}
