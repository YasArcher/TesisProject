namespace tesisproject.shared.Errors;

public static partial class ErrorMessages
{
    public static class CatalogSynchronization
    {
        public const string ProviderUnavailable = "The external catalog provider is unavailable or rejected the request.";
        public const string InvalidResponse = "The external catalog snapshot is invalid.";
        public const string DuplicateExternalId = "The external catalog snapshot contains duplicate external identifiers.";
        public const string PersistenceFailed = "The catalog synchronization could not be persisted.";
        public const string PendingChanges = "Catalog synchronization requires a scope without pending changes.";
    }
}
