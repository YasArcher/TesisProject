using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedFacultyRepository(UnifiedDideDbContext context)
    : GenericRepository<Faculty>(context), IUnifiedFacultyRepository
{
    public Task<Faculty?> GetByExternalFacultyIdAsync(int externalId, CancellationToken ct = default) =>
        _db.AsNoTracking().SingleOrDefaultAsync(x => x.ExternalFacultyId == externalId, ct);
}
