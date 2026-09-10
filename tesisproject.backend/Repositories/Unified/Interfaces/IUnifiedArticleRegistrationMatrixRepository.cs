using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Interfaces;
using System.Linq.Expressions;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedArticleRegistrationMatrixRepository : IGenericRepository<RegistrationMatrix>
{
    /// <summary>Full graph, composable by ID/owner/status before SQL execution. No authorization policy.</summary>
    IQueryable<RegistrationMatrix> QueryWithDetails(bool asNoTracking = true);
    Task<RegistrationMatrix?> FindWithDetailsAsync(Expression<Func<RegistrationMatrix, bool>> predicate, bool asNoTracking, CancellationToken ct);
    Task<List<RegistrationMatrixSummaryDto>> GetSummariesAsync(Expression<Func<RegistrationMatrix, bool>> predicate, int take, CancellationToken ct);
    /// <summary>Stages removal of a full graph loaded by QueryWithDetails, respecting Unified NoAction FKs.</summary>
    void RemoveGraph(RegistrationMatrix matrix);
}
