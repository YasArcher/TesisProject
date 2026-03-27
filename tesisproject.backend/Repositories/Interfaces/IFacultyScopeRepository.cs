using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IFacultyScopeRepository : IGenericRepository<FacultyScope>
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