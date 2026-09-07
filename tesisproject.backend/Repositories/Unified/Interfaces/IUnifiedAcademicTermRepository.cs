using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedAcademicTermRepository : IGenericRepository<AcademicTerm>
{
    Task<AcademicTerm?> GetByExternalPeriodIdAsync(int externalId, CancellationToken ct = default);
}
