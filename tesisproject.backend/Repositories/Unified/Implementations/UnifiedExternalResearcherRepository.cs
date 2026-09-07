using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedExternalResearcherRepository
        : GenericRepository<ExternalResearcher>, IUnifiedExternalResearcherRepository
    {
        public UnifiedExternalResearcherRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<ExternalResearcher?> GetByIdWithRefsAsync(
            int externalResearcherId,
            CancellationToken ct = default)
        {
            return await _ctx.Set<ExternalResearcher>()
                .Include(er => er.Institution)
                .Include(er => er.ExternalResearcherProjects)
                .AsNoTracking()
                .FirstOrDefaultAsync(er => er.ExternalResearcherId == externalResearcherId, ct);
        }

        public IQueryable<ExternalResearcher> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<ExternalResearcher>()
                .Include(er => er.Institution)
                .Include(er => er.ExternalResearcherProjects);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
