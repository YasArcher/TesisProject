using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class FacultyScopeRepository : GenericRepository<FacultyScope>, IFacultyScopeRepository
    {
        public FacultyScopeRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<FacultyScope?> GetByIdWithRefsAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            return await QueryWithRefs(includeAssignments, asNoTracking: true)
                .FirstOrDefaultAsync(x => x.FacultyScopeId == facultyScopeId, ct);
        }

        public IQueryable<FacultyScope> QueryWithRefs(
            bool includeAssignments = false,
            bool asNoTracking = true)
        {
            IQueryable<FacultyScope> q = _ctx.Set<FacultyScope>()
                .Include(s => s.Faculties);

            if (includeAssignments)
            {
                q = q.Include(s => s.UserAssignments)
                     .ThenInclude(a => a.User); // AppUser (sin navegación inversa, está ok)
            }

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}