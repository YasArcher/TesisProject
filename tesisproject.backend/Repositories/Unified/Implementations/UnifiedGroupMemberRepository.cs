using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedGroupMemberRepository
        : GenericRepository<GroupMember>, IUnifiedGroupMemberRepository
    {
        public UnifiedGroupMemberRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public Task<bool> ExistsAsync(int groupId, int externalUserId, CancellationToken ct = default)
            => _db.AsNoTracking().AnyAsync(m =>
                m.GroupId == groupId &&
                m.UserId == externalUserId &&
                m.LeftAt == null, ct);

        public IQueryable<GroupMember> QueryByGroup(int groupId, bool asNoTracking = true)
            => (asNoTracking ? _db.AsNoTracking() : _db)
                .Where(m => m.GroupId == groupId);

        public Task<List<GroupMember>> GetMembersByGroupAsync(
            int groupId,
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            var q = _db.AsNoTracking()
                       .Where(m => m.GroupId == groupId);

            if (!includeInactive)
                q = q.Where(m => m.LeftAt == null);

            return q.ToListAsync(ct);
        }

    }
}
