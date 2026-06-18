using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectObjectiveClientService
    {
        Task<HttpResponseWrapper<List<ProjectObjectiveListItemDTO>?>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByVisitWithActivitiesAsync(
            int projectId,
            int visitId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> CreateAsync(
            AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByProjectWithActivitiesAsync(
    int projectId,
    CancellationToken ct = default);

    }
}