using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalResearcherProjectClientService
    {
        Task<HttpResponseWrapper<List<ExternalResearcherProjectListItemDTO>?>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}