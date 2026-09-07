using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedAuthorRepository : IGenericRepository<Author>
{
    Task<Author?> GetByAppUserIdAsync(int appUserId, CancellationToken ct = default);
    Task<Author?> GetByExternalResearcherIdAsync(
        int externalResearcherId,
        CancellationToken ct = default);
}
