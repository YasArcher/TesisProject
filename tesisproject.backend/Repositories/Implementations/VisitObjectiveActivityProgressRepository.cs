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

    }
}