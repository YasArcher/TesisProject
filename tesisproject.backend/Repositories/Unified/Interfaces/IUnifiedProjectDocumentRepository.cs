using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProjectDocumentRepository : IGenericRepository<ProjectDocument>
    {
        // Custom query examples (optional):
        Task<List<ProjectDocument>> GetByProjectIdAsync(int projectId, CancellationToken ct = default);

        Task<ProjectDocument?> GetWithDocumentAsync(int id, CancellationToken ct = default);
    }
}
