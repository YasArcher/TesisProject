using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Project>> GetByTypeAsync(Guid projectTypeId)
        {
            return await _db.AsNoTracking()
                            .Where(p => p.ProjectTypeId == projectTypeId)
                            .ToListAsync();
        }
    }
}
