using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedProjectRepository : GenericRepository<Project>, IUnifiedProjectRepository
    {
        public UnifiedProjectRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default)
        {
            return await _db
                .AsNoTracking()
                .Where(p => p.ProjectTypeId == projectTypeId)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ProjectIdentifierRow>> ListIdentifiersByCodesAsync(
            IReadOnlyCollection<string> projectCodes, CancellationToken ct = default)
        {
            if (projectCodes.Count == 0) return [];
            return await _db.AsNoTracking()
                .Where(x => projectCodes.Contains(x.ProjectCode))
                .OrderBy(x => x.ProjectId)
                .Select(x => new ProjectIdentifierRow(x.ProjectId, x.ProjectCode, x.ProjectNumber))
                .ToListAsync(ct);
        }
    }
}
