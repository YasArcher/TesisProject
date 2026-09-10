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

public sealed class UnifiedAcademicTermSynchronizationService(IUnifiedUnitOfWork uow,
    IUnifiedAcademicCatalogSnapshotClient client, UnifiedDideDbContext context)
    : IUnifiedAcademicTermSynchronizationService
{
    public async Task<ServiceResult<CatalogSynchronizationResult>> SynchronizeAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (context.ChangeTracker.HasChanges())
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.PendingChanges,
                ErrorType.Conflict, ErrorCodes.CatalogSynchronization.PendingChanges);
        var response = await client.GetAcademicTermsAsync(ct);
        ct.ThrowIfCancellationRequested();
        if (!response.Success) return Relay(response);
        if (response.Data is null) return Invalid("Snapshot");
        var raw = response.Data;
        if (raw.Any(x => x is null || x.PeriodId <= 0)) return Invalid("ExternalPeriodId");
        if (raw.GroupBy(x => x.PeriodId).Any(g => g.Count() > 1)) return Invalid("ExternalPeriodId", duplicate: true);
        if (raw.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Trim().Length > 100 || x.StartDate == default || x.EndDate == default || x.StartDate.Date > x.EndDate.Date))
            return Invalid("AcademicTerms");
        var rows = raw.Select(x => new { ExternalId = x.PeriodId, Name = x.Name.Trim(), StartDate = x.StartDate.Date, EndDate = x.EndDate.Date }).ToList();

        var now = DateTime.UtcNow;
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        try
        {
            var externalIds = rows.Select(x => x.ExternalId).ToArray();
            var local = await uow.AcademicTerms.Query(asNoTracking: false)
                .Where(x => x.ExternalPeriodId.HasValue && externalIds.Contains(x.ExternalPeriodId.Value))
                .ToListAsync(ct);
            if (local.GroupBy(x => x.ExternalPeriodId).Any(g => g.Count() > 1))
                return Invalid("ExternalPeriodId", duplicate: true);
            var byExternalId = local.ToDictionary(x => x.ExternalPeriodId!.Value);
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                if (!byExternalId.TryGetValue(row.ExternalId, out var entity))
                {
                    entity = new AcademicTerm { ExternalPeriodId = row.ExternalId, Name = row.Name, StartDate = row.StartDate, EndDate = row.EndDate, LastSyncedAt = now };
                    await uow.AcademicTerms.AddAsync(entity, ct);
                    inserted++;
                }
                else if (entity.Name != row.Name || entity.StartDate != row.StartDate || entity.EndDate != row.EndDate)
                {
                    entity.Name = row.Name; entity.StartDate = row.StartDate; entity.EndDate = row.EndDate;
                    entity.LastSyncedAt = now;
                    updated++;
                }
                else
                {
                    // LastSyncedAt records the last content sync, not every observation.
                    entity.LastSyncedAt ??= now;
                    unchanged++;
                }
            }
            ct.ThrowIfCancellationRequested();
            if (context.ChangeTracker.HasChanges()) await uow.SaveChangesAsync(ct);
            return ServiceResult<CatalogSynchronizationResult>.Ok(new(raw.Count, inserted, updated,
                unchanged, raw.Count - rows.Count, 0, now));
        }
        catch (OperationCanceledException) { Restore<AcademicTerm>(context); throw; }
        catch (DbUpdateException)
        {
            Restore<AcademicTerm>(context);
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.PersistenceFailed,
                ErrorType.Conflict, ErrorCodes.CatalogSynchronization.PersistenceFailed);
        }
        catch (Exception)
        {
            Restore<AcademicTerm>(context);
            return ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
        }
    }
}
