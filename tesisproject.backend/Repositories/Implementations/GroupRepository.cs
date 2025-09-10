using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        // _ctx y _db vienen del base
        public GroupRepository(AppDbContext ctx) : base(ctx) { }

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

        // No repitas AddAsync/Query/GetAllAsync: ya los heredas del genérico.

        // Si quieres un helper para listados ordenados:
        // public async Task<List<Group>> GetAllOrderedAsync(CancellationToken ct = default)
        //     => await _db.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct);
    }
}
