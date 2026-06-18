using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IUserFacultyScopeAssignmentRepository : IGenericRepository<UserFacultyScopeAssignment>
    {
        IQueryable<UserFacultyScopeAssignment> QueryWithRefs(bool asNoTracking = true);

        Task<List<int>> GetActiveScopeIdsByUserAsync(int identityUserId, CancellationToken ct = default);

        Task<List<int>> GetActiveFacultyIdsByUserAsync(int identityUserId, CancellationToken ct = default);
    }
}