using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IExternalResearcherRepository : IGenericRepository<ExternalResearcher>
    {
        Task<ExternalResearcher?> GetByIdWithRefsAsync(int externalResearcherId, CancellationToken ct = default);

        IQueryable<ExternalResearcher> QueryWithRefs(bool asNoTracking = true);
    }
}
