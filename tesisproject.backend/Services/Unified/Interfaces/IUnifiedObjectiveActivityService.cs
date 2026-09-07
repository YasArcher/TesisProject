using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedObjectiveActivityService
    {
        Task<ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>> ListByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveActivityDetailDTO>> GetByIdAsync(
            int activityId,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveActivityDetailDTO>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveActivityDetailDTO>> UpdateAsync(
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> DeleteAsync(
            int activityId,
            CancellationToken ct = default);

        /// <summary>
        /// Set progress percentage for an activity (0..100).
        /// </summary>
        Task<ServiceResult<bool>> SetProgressAsync(
            int visitId,
            int activityId,
            int progressPercentage,
            CancellationToken ct = default);
    }
}