using tesisproject.shared.DTOs.ObjectiveActivityUser.Request;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IObjectiveActivityUserService
    {
        /// <summary>
        /// List all user assignments for a given ObjectiveActivity.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>> ListByActivityAsync(
            int objectiveActivityId,
            CancellationToken ct = default);

        /// <summary>
        /// Assign a user to an ObjectiveActivity in a given Visit.
        /// </summary>
        Task<ServiceResult<ObjectiveActivityUserDTO>> AssignAsync(
            AssignObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Update an existing assignment.
        /// </summary>
        Task<ServiceResult<ObjectiveActivityUserDTO>> UpdateAsync(
            UpdateObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// Remove an assignment.
        /// </summary>
        Task<ServiceResult<bool>> UnassignAsync(
            int id,
            CancellationToken ct = default);
    }
}