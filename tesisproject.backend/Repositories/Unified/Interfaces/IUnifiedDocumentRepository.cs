using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedDocumentRepository : IGenericRepository<Document>
    {
        /// <summary>
        /// Get a single Document by id including navigations (DocumentType, self relations).
        /// </summary>
        Task<Document?> GetByIdWithRefsAsync(int documentId, CancellationToken ct = default);

        /// <summary>
        /// Exposes a queryable including navigations for advanced filtering in Services.
        /// </summary>
        IQueryable<Document> QueryWithRefs(bool asNoTracking = true);
    }
}
