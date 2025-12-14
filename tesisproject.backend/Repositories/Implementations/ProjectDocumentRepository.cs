using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectDocumentRepository
        : GenericRepository<ProjectDocument>, IProjectDocumentRepository
    {
        private readonly AppDbContext _context;

        public ProjectDocumentRepository(AppDbContext context)
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