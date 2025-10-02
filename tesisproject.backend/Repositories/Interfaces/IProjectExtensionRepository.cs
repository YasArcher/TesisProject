using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectExtensionRepository : IGenericRepository<ProjectExtension>
    {
        Task<ProjectExtension?> GetByIdWithRefsAsync(int projectExtensionId, CancellationToken ct = default);
        Task<List<ProjectExtension>> GetByProjectAsync(int projectId, CancellationToken ct = default);
        IQueryable<ProjectExtension> QueryWithRefs(bool asNoTracking = true);
    }
}
