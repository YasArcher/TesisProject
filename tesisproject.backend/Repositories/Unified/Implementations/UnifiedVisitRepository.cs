using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedVisitRepository : GenericRepository<Visit>, IUnifiedVisitRepository
    {
        public UnifiedVisitRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<Visit?> GetByIdWithRefsAsync(int visitId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VisitId == visitId, ct);
        }

        public async Task<List<Visit>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            return await _ctx.Set<Visit>()
                .Where(v => v.ProjectId == projectId)
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<Visit> QueryWithRefs(bool asNoTracking = true)
        {
            var q = _ctx.Set<Visit>()
                .Include(v => v.Project)
                .Include(v => v.VisitState)
                .Include(v => v.Document);

            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<(bool Success, string? Error)> BulkScheduleAsync(
            IReadOnlyList<int> projectIds,
            DateTime scheduledDate,
            int visitStateId,
            CancellationToken ct = default)
        {
            var ids = projectIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return (false, "Debes enviar al menos un ProjectId válido.");

            // 1) Validar que existan los proyectos
            var existingProjectIds = await _ctx.Set<Project>()
                .Where(p => ids.Contains(p.ProjectId))
                .Select(p => p.ProjectId)
                .ToListAsync(ct);

            var missing = ids.Except(existingProjectIds).ToList();
            if (missing.Count > 0)
                return (false, $"No existen los proyectos: {string.Join(", ", missing)}");

            // 2) Bloquear si ya hay visita abierta (Planned/Pending/OnHold)
            var blocked = await _ctx.Set<Visit>()
                .Where(v => ids.Contains(v.ProjectId) && VisitStateIds.OpenStates.Contains(v.VisitStateId))
                .Select(v => v.ProjectId)
                .Distinct()
                .ToListAsync(ct);

            if (blocked.Count > 0)
                return (false, $"No se puede planificar porque ya existe una visita abierta para los proyectos: {string.Join(", ", blocked)}");

            // 3) Crear visitas nuevas (bulk insert)
            var entities = ids.Select(pid => new Visit
            {
                ProjectId = pid,
                VisitStateId = visitStateId,
                ScheduledDate = scheduledDate
            }).ToList();

            await _ctx.Set<Visit>().AddRangeAsync(entities, ct);

            return (true, null);
        }
    }
}
