using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedGroupMemberRepository : IGenericRepository<GroupMember>
    {
        Task<bool> ExistsAsync(int groupId, int externalUserId, CancellationToken ct = default);
        Task<List<GroupMember>> GetMembersByGroupAsync(
    int groupId,
    bool includeInactive = false,
    CancellationToken ct = default);


        /// <summary>
        /// Base query filtered by GroupId. Services pueden proyectar/ordenar/paginar.
        /// </summary>
        IQueryable<GroupMember> QueryByGroup(int groupId, bool asNoTracking = true);
    }
}
