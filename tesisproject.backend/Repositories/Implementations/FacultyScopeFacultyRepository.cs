using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class FacultyScopeFacultyRepository : GenericRepository<FacultyScopeFaculty>, IFacultyScopeFacultyRepository
    {
        public FacultyScopeFacultyRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<FacultyScopeFaculty>> GetByScopeIdAsync(
            int facultyScopeId,
            bool onlyActive = true,
            CancellationToken ct = default)
            => await QueryByScopeId(facultyScopeId, onlyActive, asNoTracking: true).ToListAsync(ct);

        public IQueryable<FacultyScopeFaculty> QueryByScopeId(
            int facultyScopeId,
            bool onlyActive = true,
            bool asNoTracking = true)
        {
            IQueryable<FacultyScopeFaculty> q = _ctx.Set<FacultyScopeFaculty>()
                .Where(x => x.FacultyScopeId == facultyScopeId);

            if (onlyActive) q = q.Where(x => x.IsActive);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}