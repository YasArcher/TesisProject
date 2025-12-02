using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectObjectiveClientService
    {
        // LISTS (simple)
        Task<HttpResponseWrapper<List<ProjectObjectiveListItemDTO>?>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        // LISTS (objective + activities)
        Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByProjectWithActivitiesAsync(
            int projectId,
            CancellationToken ct = default);

        // DETAIL (single objective)
        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> CreateAsync(
            AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        // DELETE
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}