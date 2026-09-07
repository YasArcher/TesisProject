using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedProjectDocumentRepository
        : GenericRepository<ProjectDocument>, IUnifiedProjectDocumentRepository
    {
        private readonly UnifiedDideDbContext _context;

        public UnifiedProjectDocumentRepository(UnifiedDideDbContext context)
            : base(context)
        {
            _context = context;
        }

        // ============================
        //        Custom Methods
        // ============================

        public Task<List<ProjectDocument>> GetByProjectIdAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return _context.ProjectDocuments
                .Where(x => x.ProjectId == projectId)
                .ToListAsync(ct);
        }

        public Task<ProjectDocument?> GetWithDocumentAsync(
            int id,
            CancellationToken ct = default)
        {
            return _context.ProjectDocuments
                .Include(x => x.Document)
                .FirstOrDefaultAsync(x => x.ProjectDocumentId == id, ct);
        }
    }
}