using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedDocumentRepository : GenericRepository<Document>, IUnifiedDocumentRepository
    {
        public UnifiedDocumentRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<Document?> GetByIdWithRefsAsync(int documentId, CancellationToken ct = default)
        {
            return await _ctx.Set<Document>()
                .Include(d => d.DocumentType)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);
        }

        public IQueryable<Document> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Document>()
                .Include(d => d.DocumentType);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
