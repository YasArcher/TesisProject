using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ObjectiveActivityUserRepository
        : GenericRepository<ObjectiveActivityUser>, IObjectiveActivityUserRepository
    {
        public ObjectiveActivityUserRepository(AppDbContext ctx) : base(ctx) { }

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