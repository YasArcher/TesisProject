using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IObjectiveActivityService
    {
        /// <summary>
        /// List all activities for a given ProjectObjective.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>> ListByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default);

        /// <summary>
        /// Get a single activity by id.
        /// </summary>
        Task<ServiceResult<ObjectiveActivityDetailDTO>> GetByIdAsync(
            int activityId,
            CancellationToken ct = default);

        /// <summary>
        /// Create a new activity for a ProjectObjective.
        /// </summary>
        Task<ServiceResult<ObjectiveActivityDetailDTO>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Update an existing activity.
        /// </summary>
        Task<ServiceResult<ObjectiveActivityDetailDTO>> UpdateAsync(
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete an activity.
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(
            int activityId,
            CancellationToken ct = default);

        /// <summary>
        /// Set completion state for an activity.
        /// </summary>
        Task<ServiceResult<bool>> SetCompletedAsync(
            int activityId,
            bool isCompleted,
            CancellationToken ct = default);
    }
}