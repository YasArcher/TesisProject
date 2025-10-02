using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class VisitRepository : GenericRepository<Visit>, IVisitRepository
    {
        public VisitRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<Visit?> GetByIdWithRefsAsync(int visitId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VisitId == visitId, ct);
        }

        public async Task<List<Visit>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Where(v => v.ProjectId == projectId)
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<Visit> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
