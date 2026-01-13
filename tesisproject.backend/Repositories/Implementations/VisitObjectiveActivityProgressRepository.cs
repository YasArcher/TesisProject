using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class VisitObjectiveActivityProgressRepository
        : GenericRepository<VisitObjectiveActivityProgress>, IVisitObjectiveActivityProgressRepository
    {
        public VisitObjectiveActivityProgressRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<VisitObjectiveActivityProgress>> GetByVisitIdAsync(int visitId, CancellationToken ct = default)
        {
            return await _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x => x.VisitId == visitId)
                .Include(x => x.ObjectiveActivity) // opcional, útil si necesitas ActionText/ActivityResult
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Dictionary<int, int>> GetCumulativeProgressByProjectUpToVisitAndActivityIdsAsync(
    int projectId,
    int visitId,
    IEnumerable<int> activityIds,
    CancellationToken ct = default)
        {
            var ids = activityIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return new Dictionary<int, int>();

            // 1) Tomar el ÚLTIMO progreso por (VisitId, ObjectiveActivityId) dentro del proyecto
            //    y hasta la visita solicitada (VisitId <= visitId)
            var latestPerVisitActivity = await _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x =>
                    x.Visit.ProjectId == projectId &&
                    x.VisitId <= visitId &&
                    ids.Contains(x.ObjectiveActivityId))
                .GroupBy(x => new { x.VisitId, x.ObjectiveActivityId })
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt)   // o x.Id si prefieres
                    .Select(x => new { x.ObjectiveActivityId, x.ProgressPercentage })
                    .FirstOrDefault())
                .AsNoTracking()
                .ToListAsync(ct);

            // 2) Sumar por actividad entre visitas
            var sums = latestPerVisitActivity
                .Where(x => x != null)
                .GroupBy(x => x!.ObjectiveActivityId)
                .Select(g => new
                {
                    ActivityId = g.Key,
                    Total = g.Sum(x => x!.ProgressPercentage)
                })
                .ToList();

            // 3) Clamp 0..100
            return sums.ToDictionary(
                x => x.ActivityId,
                x => Math.Min(100, Math.Max(0, x.Total))
            );
        }

        public async Task<decimal?> GetProjectProgressByVisitAsync(int projectId, int visitId, CancellationToken ct = default)
        {
            // ✅ Progreso del proyecto en una visita específica (snapshot)
            // Promedio simple de los ProgressPercentage registrados en esa visita
            return await _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x => x.VisitId == visitId && x.Visit.ProjectId == projectId)
                .Select(x => (decimal)x.ProgressPercentage)
                .DefaultIfEmpty()
                .AverageAsync(ct);
        }

        public async Task<decimal?> GetCurrentProjectProgressAsync(int projectId, CancellationToken ct = default)
        {
            // ✅ Progreso "actual" del proyecto:
            // tomar el último snapshot por ObjectiveActivityId (por CreatedAt) dentro del proyecto
            var latestPerActivity = _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x => x.Visit.ProjectId == projectId)
                .GroupBy(x => x.ObjectiveActivityId)
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => x.ProgressPercentage)
                    .FirstOrDefault());

            return await latestPerActivity
                .Select(x => (decimal)x)
                .DefaultIfEmpty()
                .AverageAsync(ct);
        }

        public async Task<Dictionary<int, int>> GetLatestProgressByActivityIdsAsync(
    IEnumerable<int> objectiveActivityIds,
    CancellationToken ct = default)
        {
            var ids = objectiveActivityIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return new Dictionary<int, int>();

            var rows = await _ctx.Set<VisitObjectiveActivityProgress>()
                .Where(x => ids.Contains(x.ObjectiveActivityId))
                .GroupBy(x => x.ObjectiveActivityId)
                .Select(g => new
                {
                    ObjectiveActivityId = g.Key,
                    Progress = g.OrderByDescending(x => x.CreatedAt)
                                .Select(x => x.ProgressPercentage)
                                .FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync(ct);

            return rows.ToDictionary(x => x.ObjectiveActivityId, x => x.Progress);
        }

        public async Task<Dictionary<int, int>> GetLatestProgressByVisitAndActivityIdsAsync(
    int visitId,
    IEnumerable<int> activityIds,
    CancellationToken ct = default)
        {
            var ids = activityIds.Distinct().ToList();

            var rows = await _ctx.VisitObjectiveActivityProgresses
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


        public async Task<Dictionary<int, int>> GetTotalProgressByActivityIdsAsync(
    IEnumerable<int> activityIds,
    CancellationToken ct = default)
        {
            var ids = activityIds.Distinct().ToList();

            var sums = await _ctx.VisitObjectiveActivityProgresses
                .Where(x => ids.Contains(x.ObjectiveActivityId))
                .GroupBy(x => x.ObjectiveActivityId)
                .Select(g => new { ActivityId = g.Key, Total = g.Sum(x => x.ProgressPercentage) })
                .ToListAsync(ct);

            return sums.ToDictionary(
                x => x.ActivityId,
                x => Math.Min(100, Math.Max(0, x.Total))
            );
        }
    }
}