using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IObjectiveActivityUserRepository : IGenericRepository<ObjectiveActivityUser>
    {
        /// <summary>
        /// List all assignments for a given ObjectiveActivity.
        /// </summary>
        Task<List<ObjectiveActivityUser>> GetByActivityAsync(
            int objectiveActivityId,
            CancellationToken ct = default);

        /// <summary>
        /// Check if an assignment already exists for the given activity, user and visit.
        /// </summary>
        Task<bool> ExistsAssignmentAsync(
            int objectiveActivityId,
            int userId,
            int visitId,
            CancellationToken ct = default);

        /// <summary>
        /// Exposes a queryable including navigations (ObjectiveActivity, Visit).
        /// </summary>
        IQueryable<ObjectiveActivityUser> QueryWithRefs(
            bool asNoTracking = true);
    }
}