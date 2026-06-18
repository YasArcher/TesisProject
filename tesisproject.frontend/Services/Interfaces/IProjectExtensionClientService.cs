using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectExtensionClientService
    {
        Task<HttpResponseWrapper<ProjectExtensionListResponseDTO?>> CreateAsync(
            AddProjectExtensionRequestDTO request, CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ProjectExtensionListResponseDTO>?>> ListByProjectAsync(
            int projectId, CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int projectExtensionId, CancellationToken ct = default);

        Task<HttpResponseWrapper<ProjectExtensionListResponseDTO?>> UpdateAsync(
            UpdateProjectExtensionRequestDTO request, CancellationToken ct = default);
    }
}
