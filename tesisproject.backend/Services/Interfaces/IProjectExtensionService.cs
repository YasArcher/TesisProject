using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectExtensionService
    {
        Task<ServiceResult<ProjectExtensionListResponseDTO>> CreateAsync(AddProjectExtensionRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ProjectExtensionListResponseDTO>> GetByIdAsync(int projectExtensionId, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<ProjectExtensionListResponseDTO>> UpdateAsync(UpdateProjectExtensionRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int projectExtensionId, CancellationToken ct = default);
    }
}