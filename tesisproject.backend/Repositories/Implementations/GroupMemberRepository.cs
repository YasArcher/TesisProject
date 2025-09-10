using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GroupMemberRepository
        : GenericRepository<GroupMember>, IGroupMemberRepository
    {
        public GroupMemberRepository(AppDbContext ctx) : base(ctx) { }

        public Task<GroupMember?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.AsNoTracking().FirstOrDefaultAsync(m => m.GroupMemberId == id, ct);

        public Task<bool> ExistsAsync(int groupId, int externalUserId, CancellationToken ct = default)
            => _db.AsNoTracking().AnyAsync(m => m.GroupId == groupId && m.UserId == externalUserId, ct);

        public IQueryable<GroupMember> QueryByGroup(int groupId, bool asNoTracking = true)
            => (asNoTracking ? _db.AsNoTracking() : _db)
                .Where(m => m.GroupId == groupId);

        public Task<List<GroupMember>> GetMembersByGroupAsync(int groupId, CancellationToken ct = default)
            => _db.AsNoTracking()
                  .Where(m => m.GroupId == groupId)
                  .ToListAsync(ct);
    }
}
