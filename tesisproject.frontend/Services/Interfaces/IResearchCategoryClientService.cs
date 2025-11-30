using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IResearchCategoryClientService
    {
        // ================ READS ================

        Task<HttpResponseWrapper<IReadOnlyList<ResearchCategoryListItemDTO>?>> GetAllAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ResearchCategoryTreeItemDTO>?>> GetTreeAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // ================ WRITES ================

        Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> CreateAsync(
            AddResearchCategoryRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> UpdateAsync(
            UpdateResearchCategoryRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}