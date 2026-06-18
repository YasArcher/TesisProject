using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ExternalResearcherRepository
        : GenericRepository<ExternalResearcher>, IExternalResearcherRepository
    {
        public ExternalResearcherRepository(AppDbContext ctx) : base(ctx) { }

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
