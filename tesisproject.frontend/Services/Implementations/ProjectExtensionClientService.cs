using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class ProjectExtensionClientService : IProjectExtensionClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/projectextensions";

        public ProjectExtensionClientService(IApiClient api) => _api = api;

        public Task<HttpResponseWrapper<ProjectExtensionListResponseDTO?>> CreateAsync(
            AddProjectExtensionRequestDTO request, CancellationToken ct = default)
            => _api.PostAsync<AddProjectExtensionRequestDTO, ProjectExtensionListResponseDTO>(
                BaseUrl, request, ct);

        public Task<HttpResponseWrapper<List<ProjectExtensionListResponseDTO>?>> ListByProjectAsync(
            int projectId, CancellationToken ct = default)
            => _api.GetAsync<List<ProjectExtensionListResponseDTO>>(
                $"{BaseUrl}/by-project/{projectId}", ct);

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int projectExtensionId, CancellationToken ct = default)
            => _api.DeleteAsync($"{BaseUrl}/{projectExtensionId}", ct);

        public Task<HttpResponseWrapper<ProjectExtensionListResponseDTO?>> UpdateAsync(
            UpdateProjectExtensionRequestDTO request, CancellationToken ct = default)
            => _api.PutAsync<UpdateProjectExtensionRequestDTO, ProjectExtensionListResponseDTO>(
                $"{BaseUrl}/{request.ProjectExtensionId}", request, ct);
    }
}
