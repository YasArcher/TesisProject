using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedGroupRepository : GenericRepository<Group>, IUnifiedGroupRepository
    {
        // _ctx y _db vienen del base
        public UnifiedGroupRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<Group?> GetByIdAsync(int id, bool includeMembers, CancellationToken ct = default)
        {
            IQueryable<Group> q = _db; // del base
            if (includeMembers)
            {
                q = q.Include(g => g.Members);
                // si aplica catálogo:
                // q = q.Include(g => g.GroupType);
            }

            return await q.AsNoTracking().FirstOrDefaultAsync(g => g.GroupId == id, ct);
        }

        public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
        {
            var n = name.Trim();
            // Nota: StringComparison no se traduce a SQL; usa ToUpper/ToLower o collation.
            return _db.AsNoTracking().AnyAsync(g => g.Name.ToUpper() == n.ToUpper(), ct);
            // Alternativa por collation específica si usas SQL Server:
            // return _db.AsNoTracking().AnyAsync(g => EF.Functions.Collate(g.Name, "SQL_Latin1_General_CP1_CI_AS") == n, ct);
        }

        public async Task<List<Group>> GetByProjectIdAsync(int projectId, CancellationToken ct = default)
        {
            var project = await _ctx.Set<Project>()
                .AsNoTracking()
                .Include(p => p.ProjectGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);

            if (project is null) return new List<Group>();

            var list = new List<Group> { project.ProjectGroup };

            // Si quieres devolver ordenado por nombre:
            return list.OrderBy(g => g.Name).ToList();
        }
    }
}
