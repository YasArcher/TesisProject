using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IGroupMemberRepository
    {
        Task AddAsync(GroupMember entity, CancellationToken ct = default);
        Task RemoveAsync(GroupMember entity, CancellationToken ct = default);
        Task<GroupMember?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid groupId, int externalUserId, CancellationToken ct = default);
        IQueryable<GroupMember> QueryByGroup(Guid groupId);
    }
}
