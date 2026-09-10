using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedObjectiveActivityUserRepository
        : GenericRepository<ObjectiveActivityUser>, IUnifiedObjectiveActivityUserRepository
    {
        public UnifiedObjectiveActivityUserRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<List<ObjectiveActivityUser>> GetByActivityAsync(
            int objectiveActivityId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ObjectiveActivityUser>()
                .Where(x => x.ObjectiveActivityId == objectiveActivityId)
                .Include(x => x.ObjectiveActivity)
                .Include(x => x.Visit)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsAssignmentAsync(
            int objectiveActivityId,
            int userId,
            int visitId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ObjectiveActivityUser>()
                .AnyAsync(x =>
                    x.ObjectiveActivityId == objectiveActivityId &&
                    x.UserId == userId &&
                    x.VisitId == visitId,
                    ct);
        }

        public IQueryable<ObjectiveActivityUser> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<ObjectiveActivityUser>()
                .Include(x => x.ObjectiveActivity)
                .Include(x => x.Visit);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}