using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class UserFacultyScopeAssignmentRepository
        : GenericRepository<UserFacultyScopeAssignment>, IUserFacultyScopeAssignmentRepository
    {
        public UserFacultyScopeAssignmentRepository(AppDbContext ctx) : base(ctx) { }

        public IQueryable<UserFacultyScopeAssignment> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<UserFacultyScopeAssignment>()
                .Include(a => a.User)
                .Include(a => a.FacultyScope);

            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<List<int>> GetActiveScopeIdsByUserAsync(
            int identityUserId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<UserFacultyScopeAssignment>()
                .AsNoTracking()
                .Where(x => x.IdentityUserId == identityUserId && x.IsActive)
                .Select(x => x.FacultyScopeId)
                .Distinct()
                .ToListAsync(ct);
        }

        /// <summary>
        /// Devuelve las FacultyId (int) efectivas para el usuario,
        /// considerando assignments activos + facultades activas dentro del scope.
        /// </summary>
        public async Task<List<int>> GetActiveFacultyIdsByUserAsync(
            int identityUserId,
            CancellationToken ct = default)
        {
            // Join explícito (robusto incluso si cambias navigations)
            return await (
                from a in _ctx.Set<UserFacultyScopeAssignment>().AsNoTracking()
                join sf in _ctx.Set<FacultyScopeFaculty>().AsNoTracking()
                    on a.FacultyScopeId equals sf.FacultyScopeId
                where a.IdentityUserId == identityUserId
                      && a.IsActive
                      && sf.IsActive
                select sf.FacultyId // int
            )
            .Distinct()
            .ToListAsync(ct);
        }
    }
}