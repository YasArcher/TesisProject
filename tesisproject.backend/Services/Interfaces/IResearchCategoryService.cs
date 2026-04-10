using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IResearchCategoryService
    {
        Task<ServiceResult<IReadOnlyList<ResearchCategoryListItemDTO>>> ListAsync(bool onlyActives = true, CancellationToken ct = default);
        Task<ServiceResult<List<ResearchCategoryTreeItemDTO>>> GetTreeAsync(bool onlyActives = true, CancellationToken ct = default);
        Task<ServiceResult<ResearchCategoryDetailDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<ResearchCategoryDetailDTO>> CreateAsync(AddResearchCategoryRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ResearchCategoryDetailDTO>> UpdateAsync(UpdateResearchCategoryRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
    }
}