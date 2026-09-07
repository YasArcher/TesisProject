using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedFacultyRepository : IGenericRepository<Faculty>
{
    Task<Faculty?> GetByExternalFacultyIdAsync(int externalId, CancellationToken ct = default);
}
