using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedExternalResearcherRepository : IGenericRepository<ExternalResearcher>
    {
        Task<ExternalResearcher?> GetByIdWithRefsAsync(int externalResearcherId, CancellationToken ct = default);

        IQueryable<ExternalResearcher> QueryWithRefs(bool asNoTracking = true);
    }
}
