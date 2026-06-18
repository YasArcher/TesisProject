using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectDocumentRepository : IGenericRepository<ProjectDocument>
    {
        // Custom query examples (optional):
        Task<List<ProjectDocument>> GetByProjectIdAsync(int projectId, CancellationToken ct = default);

        Task<ProjectDocument?> GetWithDocumentAsync(int id, CancellationToken ct = default);
    }
}
