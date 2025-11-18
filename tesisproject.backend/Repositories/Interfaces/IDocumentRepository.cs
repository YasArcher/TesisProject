using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document>
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
