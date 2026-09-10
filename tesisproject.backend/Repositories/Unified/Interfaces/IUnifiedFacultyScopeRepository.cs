using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedFacultyScopeRepository : IGenericRepository<FacultyScope>
    {
        Task<FacultyScope?> GetByIdWithRefsAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default);

        IQueryable<FacultyScope> QueryWithRefs(
            bool includeAssignments = false,
            bool asNoTracking = true);
    }
}