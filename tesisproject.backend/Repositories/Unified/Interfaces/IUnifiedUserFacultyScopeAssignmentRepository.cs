using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedUserFacultyScopeAssignmentRepository : IGenericRepository<UserFacultyScopeAssignment>
    {
        IQueryable<UserFacultyScopeAssignment> QueryWithRefs(bool asNoTracking = true);

        Task<List<int>> GetActiveScopeIdsByUserAsync(int identityUserId, CancellationToken ct = default);

        Task<List<int>> GetActiveFacultyIdsByUserAsync(int identityUserId, CancellationToken ct = default);
    }
}