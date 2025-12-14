using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectExtensionRepository : GenericRepository<ProjectExtension>, IProjectExtensionRepository
    {
        public ProjectExtensionRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<ProjectExtension?> GetByIdWithRefsAsync(int projectExtensionId, CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectExtension>()
                .Include(pe => pe.Project)
                .Include(pe => pe.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<List<ProjectExtension>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            return await _ctx.Set<ProjectExtension>()
                .Where(pe => pe.ProjectId == projectId)
                .Include(pe => pe.Project)
                .Include(pe => pe.Document)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<ProjectExtension> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<ProjectExtension>()
                .Include(pe => pe.Project)
                .Include(pe => pe.Document);

            return asNoTracking ? q.AsNoTracking() : q;
        }
    }
}
