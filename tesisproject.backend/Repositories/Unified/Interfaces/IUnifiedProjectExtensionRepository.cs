using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProjectExtensionRepository : IGenericRepository<ProjectExtension>
    {
        Task<ProjectExtension?> GetByIdWithRefsAsync(int projectExtensionId, CancellationToken ct = default);
        Task<List<ProjectExtension>> GetByProjectAsync(int projectId, CancellationToken ct = default);
        IQueryable<ProjectExtension> QueryWithRefs(bool asNoTracking = true);
    }
}
