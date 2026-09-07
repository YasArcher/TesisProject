using tesisproject.shared.DTOs.ProjectResearchCategory.Request;
using tesisproject.shared.DTOs.ProjectResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProjectResearchCategoryService
    {
        Task<ServiceResult<List<ProjectResearchCategoryListItemDTO>>> ListAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<ProjectResearchCategoryDetailDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<ProjectResearchCategoryDetailDTO>> CreateAsync(AddProjectResearchCategoryRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProjectResearchCategoryDetailDTO>> UpdateAsync(UpdateProjectResearchCategoryRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
    }
}