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

        public IQueryable<ProjectObjective> QueryWithRefs(
            bool asNoTracking = true)
        {
            var q = _ctx.Set<ProjectObjective>()
                .Include(o => o.Project)
                .Include(o => o.ObjectiveType)
                .Include(o => o.Activities);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}