using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class VisitRepository : GenericRepository<Visit>, IVisitRepository
    {
        public VisitRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<Visit?> GetByIdWithRefsAsync(int visitId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VisitId == visitId, ct);
        }

        public async Task<List<Visit>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Where(v => v.ProjectId == projectId)
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<Visit> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document);

            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<(bool Success, string? Error)> BulkScheduleAsync(
    IReadOnlyList<int> visitIds,
    DateTime scheduledDate,
    int visitStateId,
    CancellationToken ct = default)
        {
            var ids = visitIds.Distinct().ToList();

            var existingIds = await _ctx.Visits
                .Where(v => ids.Contains(v.VisitId))
                .Select(v => v.VisitId)
                .ToListAsync(ct);

            var missing = ids.Except(existingIds).ToList();
            if (missing.Count > 0)
                return (false, $"No existen las visitas: {string.Join(", ", missing)}");

            // Actualización masiva (no tracking)
            await _ctx.Visits
                .Where(v => ids.Contains(v.VisitId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.ScheduledDate, scheduledDate)
                    .SetProperty(v => v.VisitStateId, visitStateId),
                    ct);

            return (true, null);
        }

    }
}
