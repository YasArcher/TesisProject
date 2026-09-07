using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedExternalResearcherProjectRepository : IGenericRepository<ExternalResearcherProject>
    {
        Task<ExternalResearcherProject?> GetByIdWithRefsAsync(
            int id,
            CancellationToken ct = default);

        IQueryable<ExternalResearcherProject> QueryWithRefs(bool asNoTracking = true);

        Task<IReadOnlyList<ExternalResearcherProject>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default);
    }
}