using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GroupMemberRepository : IGenericRepository<GroupMember>, IGroupMemberRepository
    {
        private readonly AppDbContext _ctx;
        private readonly DbSet<GroupMember> _db;

        public GroupMemberRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _db = _ctx.Set<GroupMember>();
        }

        // IGenericRepository<GroupMember>
        public async Task AddAsync(GroupMember entity, CancellationToken ct = default)
            => await _db.AddAsync(entity, ct);

        // (si tu interfaz también expone esta sobrecarga sin token)
        public Task AddAsync(GroupMember entity)
            => _db.AddAsync(entity).AsTask();

        public async Task<IEnumerable<GroupMember>> GetAllAsync(Expression<Func<GroupMember, bool>>? filter = null)
        {
            IQueryable<GroupMember> q = _db.AsNoTracking();
            if (filter is not null) q = q.Where(filter);
            return await q.ToListAsync();
        }

        public async Task<GroupMember?> GetByIdAsync(object id)
        {
            if (id is Guid guid)
                return await _db.AsNoTracking().FirstOrDefaultAsync(m => m.GroupMemberId == guid);

            return null; // o lanza InvalidOperationException si prefieres
        }

        public Task<GroupMember?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _db.AsNoTracking().FirstOrDefaultAsync(m => m.GroupMemberId == id, ct);

        public void Update(GroupMember entity) => _db.Update(entity);

        public void Remove(GroupMember entity) => _db.Remove(entity);

        public Task RemoveAsync(GroupMember entity, CancellationToken ct = default)
        {
            _db.Remove(entity);
            return Task.CompletedTask;
        }

        // IGroupMemberRepository
        public Task<bool> ExistsAsync(Guid groupId, int externalUserId, CancellationToken ct = default)
            => _db.AnyAsync(m => m.GroupId == groupId && m.UserId == externalUserId, ct);

        public IQueryable<GroupMember> QueryByGroup(Guid groupId)
            => _db.AsNoTracking().Where(m => m.GroupId == groupId);
    }
}
