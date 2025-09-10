using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IGroupMemberRepository : IGenericRepository<GroupMember>
    {
        Task<GroupMember?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsAsync(int groupId, int externalUserId, CancellationToken ct = default);
        Task<List<GroupMember>> GetMembersByGroupAsync(int groupId, CancellationToken ct = default);

        /// <summary>
        /// Base query filtered by GroupId. Services pueden proyectar/ordenar/paginar.
        /// </summary>
        IQueryable<GroupMember> QueryByGroup(int groupId, bool asNoTracking = true);
    }
}
