using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedExternalResearcherProjectRepository
        : GenericRepository<ExternalResearcherProject>, IUnifiedExternalResearcherProjectRepository
    {
        public UnifiedExternalResearcherProjectRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<ExternalResearcherProject?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ExternalResearcherProject>()
                .Include(x => x.ExternalResearcher)
                .Include(x => x.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExternalResearcherProjectId == id, ct);
        }

        public IQueryable<ExternalResearcherProject> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<ExternalResearcherProject>()
                .Include(x => x.ExternalResearcher)
                .Include(x => x.Project);

            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<IReadOnlyList<ExternalResearcherProject>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ExternalResearcherProject>()
                .Where(x => x.ProjectId == projectId)
                .Include(x => x.ExternalResearcher)
                .Include(x => x.Project)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}