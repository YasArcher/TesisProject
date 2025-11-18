using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class VisitIssueRepository : GenericRepository<VisitIssue>, IVisitIssueRepository
    {
        public VisitIssueRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<VisitIssue?> GetByIdWithRefsAsync(int visitIssueId, CancellationToken ct = default)
        {
            return await _ctx.Set<VisitIssue>()
                .Include(vi => vi.Visit)
                .AsNoTracking()
                .FirstOrDefaultAsync(vi => vi.VisitIssueId == visitIssueId, ct);
        }

        public async Task<List<VisitIssue>> GetByVisitAsync(int visitId, CancellationToken ct = default)
        {
            return await _ctx.Set<VisitIssue>()
                .Where(vi => vi.VisitId == visitId)
                .Include(vi => vi.Visit)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<VisitIssue> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<VisitIssue>()
                .Include(vi => vi.Visit);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
