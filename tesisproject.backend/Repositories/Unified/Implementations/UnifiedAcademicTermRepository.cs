using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedAcademicTermRepository(UnifiedDideDbContext context)
    : GenericRepository<AcademicTerm>(context), IUnifiedAcademicTermRepository
{
    public Task<AcademicTerm?> GetByExternalPeriodIdAsync(int externalId, CancellationToken ct = default) =>
        _db.AsNoTracking().SingleOrDefaultAsync(x => x.ExternalPeriodId == externalId, ct);
}
