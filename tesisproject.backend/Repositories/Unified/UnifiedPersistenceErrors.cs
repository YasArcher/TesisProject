using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace tesisproject.backend.Repositories.Unified;

public enum PersistenceFailure { Unknown, Duplicate, DuplicateArticleDoi, ReferenceConstraint, ValueTooLong }

/// <summary>Classifies storage failures without replacing the original exception or changing rollback behavior.</summary>
public static class UnifiedPersistenceErrors
{
    public static PersistenceFailure Classify(Exception exception)
    {
        if (exception is not DbUpdateException { InnerException: SqlException sql })
            return PersistenceFailure.Unknown;
        return sql.Number switch
        {
            2601 or 2627 => sql.Message.Contains("UX_ArticleDoiUniqueness_DoiKey", StringComparison.Ordinal)
                ? PersistenceFailure.DuplicateArticleDoi : PersistenceFailure.Duplicate,
            547 => PersistenceFailure.ReferenceConstraint,
            2628 => PersistenceFailure.ValueTooLong,
            _ => PersistenceFailure.Unknown
        };
    }
}
