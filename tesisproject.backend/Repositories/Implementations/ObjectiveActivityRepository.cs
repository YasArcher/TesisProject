using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ObjectiveActivityRepository
        : GenericRepository<ObjectiveActivity>, IObjectiveActivityRepository
    {
        public ObjectiveActivityRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<ObjectiveActivity?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ObjectiveActivity>()
                .Include(a => a.Objective)
                .ThenInclude(o => o.ObjectiveType)
                .Include(a => a.ResponsibleUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ObjectiveActivityId == id, ct);
        }

        public async Task<List<ObjectiveActivity>> GetByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ObjectiveActivity>()
                .Where(a => a.ObjectiveId == objectiveId)
                .Include(a => a.Objective)
                .ThenInclude(o => o.ObjectiveType)
                .Include(a => a.ResponsibleUsers)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<ObjectiveActivity> QueryWithRefs(
            bool asNoTracking = true)
        {
            var q = _ctx.Set<ObjectiveActivity>()
                .Include(a => a.Objective)
                .ThenInclude(o => o.ObjectiveType)
                .Include(a => a.ResponsibleUsers);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}