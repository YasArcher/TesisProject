using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedFacultyScopeFacultyRepository : IGenericRepository<FacultyScopeFaculty>
    {
        Task<List<FacultyScopeFaculty>> GetByScopeIdAsync(
            int facultyScopeId,
            bool onlyActive = true,
            CancellationToken ct = default);

        IQueryable<FacultyScopeFaculty> QueryByScopeId(
            int facultyScopeId,
            bool onlyActive = true,
            bool asNoTracking = true);
    }
}