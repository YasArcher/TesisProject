using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedAuthorRepository
    : GenericRepository<Author>, IUnifiedAuthorRepository
{
    public UnifiedAuthorRepository(UnifiedDideDbContext context) : base(context) { }

    public async Task<Author?> GetByAppUserIdAsync(
        int appUserId,
        CancellationToken ct = default) =>
        await _db
            .AsNoTracking()
            .FirstOrDefaultAsync(author => author.AppUserId == appUserId, ct);

    public async Task<Author?> GetByExternalResearcherIdAsync(
        int externalResearcherId,
        CancellationToken ct = default) =>
        await _db
            .AsNoTracking()
            .FirstOrDefaultAsync(
                author => author.ExternalResearcherId == externalResearcherId,
                ct);
}
