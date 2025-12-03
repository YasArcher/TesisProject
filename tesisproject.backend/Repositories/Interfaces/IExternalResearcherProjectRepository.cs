using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IExternalResearcherProjectRepository : IGenericRepository<ExternalResearcherProject>
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