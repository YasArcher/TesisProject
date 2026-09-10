using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Services.Unified.Contracts;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;
using static tesisproject.backend.Services.Unified.Implementations.UnifiedCatalogSynchronizationErrors;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedFacultySynchronizationService(IUnifiedUnitOfWork uow,
    IUnifiedAcademicCatalogSnapshotClient client, UnifiedDideDbContext context)
    : IUnifiedFacultySynchronizationService
{
    public async Task<ServiceResult<CatalogSynchronizationResult>> SynchronizeAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (context.ChangeTracker.HasChanges())
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.PendingChanges,
                ErrorType.Conflict, ErrorCodes.CatalogSynchronization.PendingChanges);
        var response = await client.GetFacultiesAsync(ct);
        ct.ThrowIfCancellationRequested();
        if (!response.Success) return Relay(response);
        if (response.Data is null) return Invalid("Snapshot");
        var raw = response.Data;
        if (raw.Any(x => x is null || x.Id <= 0 || x.ParentId <= 0)) return Invalid("ExternalFacultyId");
        if (raw.GroupBy(x => x.Id).Any(g => g.Count() > 1)) return Invalid("ExternalFacultyId", duplicate: true);
        if (raw.Any(x => x.Id == x.ParentId)) return Invalid("ParentId");
        var selected = raw;
        if (selected.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Trim().Length > 200 || x.Acronym?.Trim().Length > 40))
            return Invalid("Faculties");
        var rows = selected.Select(x => new { ExternalId = x.Id, x.ParentId, Name = x.Name.Trim(), Acronym = string.IsNullOrWhiteSpace(x.Acronym) ? null : x.Acronym.Trim() }).ToList();

        var now = DateTime.UtcNow;
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        List<Faculty> local = [];
        var originals = new Dictionary<Faculty, Faculty?>();
        var added = new List<Faculty>();
        try
        {
            // Load the catalog once: omitted local parents can have ancestors outside the snapshot.
            // Those edges must participate in validation without an ancestor query per node.
            local = await uow.Faculties.Query(asNoTracking: false).ToListAsync(ct);
            if (local.Where(x => x.ExternalFacultyId.HasValue).GroupBy(x => x.ExternalFacultyId).Any(g => g.Count() > 1))
                return Invalid("ExternalFacultyId", duplicate: true);
            var byLocalId = local.ToDictionary(x => x.FacultyId);
            var byExternalId = local.Where(x => x.ExternalFacultyId.HasValue).ToDictionary(x => x.ExternalFacultyId!.Value);
            foreach (var entity in local)
            {
                Faculty? parent = null;
                if (entity.ParentFacultyId is int parentId && !byLocalId.TryGetValue(parentId, out parent))
                    return Invalid("ParentFacultyId");
                originals.Add(entity, parent);
            }
            var parents = new Dictionary<Faculty, Faculty?>(originals);
            foreach (var row in rows)
                if (!byExternalId.ContainsKey(row.ExternalId))
                {
                    var entity = new Faculty { ExternalFacultyId = row.ExternalId };
                    byExternalId.Add(row.ExternalId, entity);
                    added.Add(entity); // Untracked until the entire proposed hierarchy is valid.
                }
            foreach (var row in rows)
            {
                Faculty? parent = null;
                if (row.ParentId is int externalParentId && !byExternalId.TryGetValue(externalParentId, out parent))
                    return Invalid("ParentId");
                parents[byExternalId[row.ExternalId]] = parent;
            }
            var visited = new HashSet<Faculty>();
            foreach (var start in parents.Keys)
            {
                var path = new HashSet<Faculty>();
                for (var node = start; node is not null && !visited.Contains(node); node = parents[node])
                {
                    ct.ThrowIfCancellationRequested();
                    if (!path.Add(node)) return Invalid("Hierarchy.Cycle");
                }
                visited.UnionWith(path);
            }

            foreach (var entity in added) await uow.Faculties.AddAsync(entity, ct);
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                var entity = byExternalId[row.ExternalId];
                var parent = parents[entity];
                var exists = originals.TryGetValue(entity, out var originalParent);
                var changed = entity.Name != row.Name || entity.Acronym != row.Acronym || originalParent != parent;
                if (!exists) inserted++;
                else if (changed) updated++;
                else unchanged++;
                entity.Name = row.Name;
                entity.Acronym = row.Acronym;
                // Navigation supports generated local keys and any snapshot ordering in one save.
                entity.Parent = parent;
                if (parent is null) entity.ParentFacultyId = null;
                if (!exists || changed || entity.LastSyncedAt is null) entity.LastSyncedAt = now;
            }
            ct.ThrowIfCancellationRequested();
            if (context.ChangeTracker.HasChanges()) await uow.SaveChangesAsync(ct);
            return ServiceResult<CatalogSynchronizationResult>.Ok(new(raw.Count, inserted, updated,
                unchanged, 0, 0, now));
        }
        catch (OperationCanceledException) { RestoreHierarchy(); throw; }
        catch (DbUpdateException)
        {
            RestoreHierarchy();
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.PersistenceFailed,
                ErrorType.Conflict, ErrorCodes.CatalogSynchronization.PersistenceFailed);
        }
        catch (Exception)
        {
            RestoreHierarchy();
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
        }

        void RestoreHierarchy()
        {
            // Restore scalar values AND navigation collections, preventing a later save from
            // rediscovering a failed insert through an existing parent's Children navigation.
            var autoDetect = context.ChangeTracker.AutoDetectChangesEnabled;
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                foreach (var entity in added)
                {
                    entity.Parent = null;
                    entity.Children.Clear();
                    context.Entry(entity).State = EntityState.Detached;
                }
                foreach (var entity in local)
                {
                    var entry = context.Entry(entity);
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entity.Children.Clear();
                    entity.Parent = originals.GetValueOrDefault(entity);
                }
                foreach (var (entity, parent) in originals)
                    if (parent is not null && !parent.Children.Contains(entity)) parent.Children.Add(entity);
                foreach (var entity in local) context.Entry(entity).State = EntityState.Unchanged;
            }
            finally { context.ChangeTracker.AutoDetectChangesEnabled = autoDetect; }
        }
    }
}
