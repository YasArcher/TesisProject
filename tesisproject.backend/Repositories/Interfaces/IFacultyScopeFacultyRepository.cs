using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IFacultyScopeFacultyRepository : IGenericRepository<FacultyScopeFaculty>
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