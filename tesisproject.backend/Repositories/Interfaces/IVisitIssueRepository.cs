using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IVisitIssueRepository : IGenericRepository<VisitIssue>
    {
        /// <summary>
        /// Get a single VisitIssue by id including navigations (Visit).
        /// </summary>
        Task<VisitIssue?> GetByIdWithRefsAsync(int visitIssueId, CancellationToken ct = default);

        /// <summary>
        /// List issues for a given VisitId (no tracking) including navigations.
        /// </summary>
        Task<List<VisitIssue>> GetByVisitAsync(int visitId, CancellationToken ct = default);

        /// <summary>
        /// Exposes a queryable including navigations for advanced filtering in Services.
        /// </summary>
        IQueryable<VisitIssue> QueryWithRefs(bool asNoTracking = true);
    }
}