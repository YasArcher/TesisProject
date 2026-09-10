using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Contracts;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

internal static class UnifiedCatalogSynchronizationErrors
{
    internal static ServiceResult<CatalogSynchronizationResult> Invalid(string field, bool duplicate = false)
        => ServiceResult<CatalogSynchronizationResult>.Fail(
            duplicate ? ErrorMessages.CatalogSynchronization.DuplicateExternalId : ErrorMessages.CatalogSynchronization.InvalidResponse,
            ErrorType.Validation,
            duplicate ? ErrorCodes.CatalogSynchronization.DuplicateExternalId : ErrorCodes.CatalogSynchronization.InvalidResponse,
            new() { [field] = [duplicate ? ErrorMessages.CatalogSynchronization.DuplicateExternalId : ErrorMessages.CatalogSynchronization.InvalidResponse] });

    internal static ServiceResult<CatalogSynchronizationResult> Relay<T>(ServiceResult<T> source)
        => ServiceResult<CatalogSynchronizationResult>.Fail(source.Message ?? ErrorMessages.CatalogSynchronization.ProviderUnavailable,
            source.Error == ErrorType.None ? ErrorType.Unexpected : source.Error, source.ErrorCode, source.ValidationErrors);

    // The scope is required to be clean on entry. Restore only changes to this catalog,
    // so a failed attempt cannot be committed accidentally by a later operation.
    internal static void Restore<TEntity>(UnifiedDideDbContext context) where TEntity : class
    {
        foreach (var entry in context.ChangeTracker.Entries<TEntity>().ToArray())
        {
            if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
            else if (entry.State == EntityState.Modified)
            {
                entry.CurrentValues.SetValues(entry.OriginalValues);
                entry.State = EntityState.Unchanged;
            }
        }
    }
}
