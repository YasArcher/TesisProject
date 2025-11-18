using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IObjectiveActivityRepository : IGenericRepository<ObjectiveActivity>
    {
        /// <summary>
        /// Get a single ObjectiveActivity by id including navigations
        /// (Objective, ResponsibleUsers).
        /// </summary>
        Task<ObjectiveActivity?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default);

        /// <summary>
        /// List activities for a given ObjectiveId (no tracking) including navigations.
        /// </summary>
        Task<List<ObjectiveActivity>> GetByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default);

        /// <summary>
        /// Exposes a queryable including navigations for advanced filtering in Services.
        /// </summary>
        IQueryable<ObjectiveActivity> QueryWithRefs(
            bool asNoTracking = true);
    }
}