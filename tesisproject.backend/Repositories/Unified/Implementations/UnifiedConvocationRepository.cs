using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    /// <summary>
    /// Repository for Convocation aggregate (Convocation → Rules → AllowedIndexings).
    /// NOTE: This repository returns Entities only. Mapping to DTOs happens in Services.
    /// </summary>
    public class UnifiedConvocationRepository
        : GenericRepository<Convocation>, IUnifiedConvocationRepository
    {
        public UnifiedConvocationRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        // =============================================================
        // ============= Convocation-level operations ==================
        // =============================================================

        /// <summary>
        /// Gets a Convocation by id; optionally includes Rules and their AllowedIndexings.
        /// </summary>
        public async Task<Convocation?> GetByIdAsync(int id, bool includeRules = false, CancellationToken ct = default)
        {
            IQueryable<Convocation> q = _db;

            if (includeRules)
            {
                q = q
                    .Include(c => c.Rules!)
                        .ThenInclude(r => r.AllowedIndexings!)
                            .ThenInclude(ai => ai.IndexingSource);
            }

            return await q.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        /// <summary>
        /// Returns the currently active Convocation (there must be 0 or 1).
        /// </summary>
        public async Task<Convocation?> GetActiveAsync(CancellationToken ct = default)
            => await _db.AsNoTracking().FirstOrDefaultAsync(c => c.IsActive, ct);

        /// <summary>
        /// True if any Convocation is active.
        /// </summary>
        public async Task<bool> AnyActiveAsync(CancellationToken ct = default)
            => await _db.AsNoTracking().AnyAsync(c => c.IsActive, ct);

        /// <summary>
        /// Makes the given Convocation the ONLY active one (deactivates all others).
        /// Does NOT call SaveChanges; UoW must persist afterwards.
        /// </summary>
        public async Task SetActiveExclusiveAsync(int convocationId, CancellationToken ct = default)
        {
            // Tracking query (intencional): queremos modificar y que UoW persista.
            var all = await _db
                .Where(c => c.IsActive || c.Id == convocationId)
                .ToListAsync(ct);

            foreach (var c in all)
                c.IsActive = (c.Id == convocationId);

            // Nota: si quisieras optimizar con ExecuteUpdateAsync (EF Core 7+),
            // se puede, pero depende de versión/provider.
        }

        /// <summary>
        /// Returns list filtered by optional text and active flag.
        /// </summary>
        public async Task<List<Convocation>> SearchAsync(string? text, bool? onlyActive, CancellationToken ct = default)
        {
            var q = _db.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(text))
                q = q.Where(c => c.Name.Contains(text) || (c.Code != null && c.Code.Contains(text)));

            if (onlyActive == true)
                q = q.Where(c => c.IsActive);

            return await q
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Returns paged convocations.
        /// </summary>
        public async Task<(List<Convocation> items, int total)> GetPagedAsync(
            int page, int pageSize, string? search = null, CancellationToken ct = default)
        {
            // Defaults + clamp desde enum (evita números quemados)
            var defaultPage = (int)ConvocationQueryConfig.DefaultPage;
            var defaultPageSize = (int)ConvocationQueryConfig.DefaultPageSize;
            var maxPageSize = (int)ConvocationQueryConfig.MaxPageSize;

            if (page <= 0) page = defaultPage;

            if (pageSize <= 0) pageSize = defaultPageSize;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var q = _db.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.Name.Contains(search) || (c.Code != null && c.Code.Contains(search)));

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        // =============================================================
        // ======== Aggregated child operations (Rules/Indexings) ======
        // =============================================================

        public async Task AddRuleAsync(int convocationId, ConvocationRule rule, CancellationToken ct = default)
        {
            var conv = await _db.Include(c => c.Rules)
                                .FirstOrDefaultAsync(c => c.Id == convocationId, ct);

            if (conv is null) throw new InvalidOperationException("Convocation not found.");

            conv.Rules ??= new List<ConvocationRule>();
            rule.ConvocationId = conv.Id;
            conv.Rules.Add(rule);
        }

        public async Task UpdateRuleAsync(int convocationId, ConvocationRule rule, CancellationToken ct = default)
        {
            var existing = await _ctx.Set<ConvocationRule>()
                .FirstOrDefaultAsync(r => r.Id == rule.Id && r.ConvocationId == convocationId, ct);

            if (existing is null) throw new InvalidOperationException("Rule not found.");

            _ctx.Entry(existing).CurrentValues.SetValues(rule);
        }

        public async Task RemoveRuleAsync(int convocationId, int ruleId, CancellationToken ct = default)
        {
            var existing = await _ctx.Set<ConvocationRule>()
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.ConvocationId == convocationId, ct);

            if (existing is null) return;

            _ctx.Remove(existing);
        }

        public async Task SetRuleAllowedIndexingsAsync(
            int ruleId,
            IEnumerable<int> indexingSourceIds,
            CancellationToken ct = default)
        {
            var dbSet = _ctx.Set<ConvocationRuleIndexing>();

            var current = await dbSet
                .Where(x => x.ConvocationRuleId == ruleId)
                .ToListAsync(ct);

            var desired = indexingSourceIds.Distinct().ToHashSet();

            var toRemove = current
                .Where(c => !desired.Contains(c.IndexingSourceId))
                .ToList();

            var toAdd = desired
                .Except(current.Select(c => c.IndexingSourceId))
                .Select(id => new ConvocationRuleIndexing
                {
                    ConvocationRuleId = ruleId,
                    IndexingSourceId = id
                })
                .ToList();

            if (toRemove.Any()) dbSet.RemoveRange(toRemove);
            if (toAdd.Any()) await dbSet.AddRangeAsync(toAdd, ct);
        }
    }
}
