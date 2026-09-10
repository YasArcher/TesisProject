using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedArticleRegistrationRepository(UnifiedDideDbContext context) : IUnifiedArticleRegistrationRepository
{
    public Task<Author?> FindAuthorAsync(Expression<Func<Author, bool>> predicate, CancellationToken ct) =>
        context.Set<Author>().SingleOrDefaultAsync(predicate, ct);
    public Task<List<FormDefinition>> GetActiveFormsAsync(string entityName, CancellationToken ct) =>
        context.Set<FormDefinition>().AsNoTracking().Where(f => f.IsActive && f.EntityName.Trim() == entityName)
            .Include(f => f.Fields).ThenInclude(f => f.Field).ToListAsync(ct);
    public Task<Venue?> GetVenueAsync(int id, CancellationToken ct) =>
        context.Set<Venue>().AsNoTracking().SingleOrDefaultAsync(v => v.VenueId == id, ct);
}
