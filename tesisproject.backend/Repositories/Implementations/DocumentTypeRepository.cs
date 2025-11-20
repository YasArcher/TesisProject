using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for DocumentType catalog.
    /// Delegates common behaviors to CatalogRepository&lt;DocumentType&gt;.
    /// </summary>
    public class DocumentTypeRepository : CatalogRepository<DocumentType>, IDocumentTypeRepository
    {
        public DocumentTypeRepository(AppDbContext ctx) : base(ctx) { }
    }
}