using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedVisitRepository : IGenericRepository<Visit>
    {
        /// <summary>
        /// Get a single Visit by id including navigations (Project, VisitState, Document).
        /// </summary>
        Task<Visit?> GetByIdWithRefsAsync(int visitId, CancellationToken ct = default);

        /// <summary>
        /// List visits for a given ProjectId (no tracking) including navigations.
        /// </summary>
        Task<List<Visit>> GetByProjectAsync(int projectId, CancellationToken ct = default);

        Task<(bool Success, string? Error)> BulkScheduleAsync(
    IReadOnlyList<int> visitIds,
    DateTime scheduledDate,
    int visitStateId,
    CancellationToken ct = default);


        /// <summary>
        /// Exposes a queryable including navigations for advanced filtering in Services.
        /// </summary>
        IQueryable<Visit> QueryWithRefs(bool asNoTracking = true);
    }
}
