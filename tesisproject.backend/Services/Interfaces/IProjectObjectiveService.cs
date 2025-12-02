using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectObjectiveService
    {
        /// <summary>
        /// List all objectives for a given ProjectId.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        /// <summary>
        /// Get a single objective by id (with basic references).
        /// </summary>
        Task<ServiceResult<ProjectObjectiveDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        /// <summary>
        /// Create a new objective for a project.
        /// </summary>
        Task<ServiceResult<ProjectObjectiveDetailDTO>> CreateAsync(
            AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Update an existing objective.
        /// </summary>
        Task<ServiceResult<ProjectObjectiveDetailDTO>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete an objective (and its activities, if cascade is configured).
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>> ListByProjectWithActivitiesAsync(
            int projectId,
            CancellationToken ct = default);
    }
}