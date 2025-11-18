using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectObjectiveRepository : IGenericRepository<ProjectObjective>
    {
        /// <summary>
        /// Get a single ProjectObjective by id including navigations
        /// (Project, ObjectiveType, Activities).
        /// </summary>
        Task<ProjectObjective?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default);

        /// <summary>
        /// List objectives for a given ProjectId (no tracking) including navigations.
        /// </summary>
        Task<List<ProjectObjective>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default);

        /// <summary>
        /// Exposes a queryable including navigations for advanced filtering in Services.
        /// </summary>
        IQueryable<ProjectObjective> QueryWithRefs(
            bool asNoTracking = true);
    }
}