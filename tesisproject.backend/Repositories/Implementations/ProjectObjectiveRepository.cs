using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectObjectiveRepository
        : GenericRepository<ProjectObjective>, IProjectObjectiveRepository
    {
        public ProjectObjectiveRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<ProjectObjective?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectObjective>()
                .Include(o => o.Project)
                .Include(o => o.ObjectiveType)
                .Include(o => o.Activities)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        public async Task<List<ProjectObjective>> GetByProjectWithActivitiesAsync(
    int projectId,
    CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectObjective>()
                .AsNoTracking()
                .Where(o => o.ProjectId == projectId)
                .Include(o => o.Activities)
                .OrderBy(o => o.Id)
                .ToListAsync(ct);
        }

        public async Task<List<ProjectObjective>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectObjective>()
                .Where(o => o.ProjectId == projectId)
                .Include(o => o.Project)
                .Include(o => o.ObjectiveType)
                .Include(o => o.Activities)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // Implementación
        public async Task<IReadOnlyList<ProjectObjective>> ListByProjectWithActivitiesAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectObjective>()
                .Where(o => o.ProjectId == projectId)
                .Include(o => o.ObjectiveType)
                .Include(o => o.Activities)
                    .ThenInclude(a => a.ResponsibleUsers)
                .ToListAsync(ct);
        }

        public IQueryable<ProjectObjective> QueryWithRefs(
            bool asNoTracking = true)
        {
            var q = _ctx.Set<ProjectObjective>()
                .Include(o => o.Project)
                .Include(o => o.ObjectiveType)
                .Include(o => o.Activities);

            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<Dictionary<int, int>> GetLatestProgressByVisitAndActivityIdsAsync(
    int visitId,
    IEnumerable<int> activityIds,
    CancellationToken ct = default)
        {
            var ids = activityIds.Distinct().ToList();

            var rows = await _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x => x.VisitId == visitId && ids.Contains(x.ObjectiveActivityId))
                .GroupBy(x => x.ObjectiveActivityId)
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt) // o x.Id si es autoincremental
                    .Select(x => new { ActivityId = g.Key, x.ProgressPercentage })
                    .FirstOrDefault())
                .ToListAsync(ct);

            return rows
                .Where(x => x is not null)
                .ToDictionary(x => x!.ActivityId, x => Math.Min(100, Math.Max(0, x!.ProgressPercentage)));
        }
    }
}
