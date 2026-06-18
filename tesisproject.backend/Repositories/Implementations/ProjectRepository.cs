using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default)
        {
            return await _db
                .AsNoTracking()
                .Where(p => p.ProjectTypeId == projectTypeId)
                .ToListAsync(ct);
        }
    }
}
