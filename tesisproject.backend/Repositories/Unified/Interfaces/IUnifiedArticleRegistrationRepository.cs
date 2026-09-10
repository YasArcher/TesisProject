using tesisproject.backend.Data.UnifiedEntities.Articles;
using System.Linq.Expressions;
using tesisproject.backend.Data.UnifiedEntities.Authors;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedArticleRegistrationRepository
{
    Task<Author?> FindAuthorAsync(Expression<Func<Author, bool>> predicate, CancellationToken ct);
    Task<List<FormDefinition>> GetActiveFormsAsync(string entityName, CancellationToken ct);
    Task<Venue?> GetVenueAsync(int id, CancellationToken ct);
}
