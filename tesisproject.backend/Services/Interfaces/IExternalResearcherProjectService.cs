using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalResearcherProjectService
    {
        Task<ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalResearcherProjectDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalResearcherProjectDetailDTO>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            int currentUserId,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalResearcherProjectDetailDTO>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}