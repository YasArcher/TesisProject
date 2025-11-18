using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<Document?> GetByIdWithRefsAsync(int documentId, CancellationToken ct = default)
        {
            return await _ctx.Set<Document>()
                .Include(d => d.DocumentType)
                .Include(d => d.RelatedDocument)
                .Include(d => d.ReverseRelation)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);
        }

        public IQueryable<Document> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Document>()
                .Include(d => d.DocumentType)
                .Include(d => d.RelatedDocument)
                .Include(d => d.ReverseRelation);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
