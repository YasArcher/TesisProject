using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace tesisproject.backend.Data;

/// <summary>Explicit deployment operation, separate from local tooling and legacy bootstrap.</summary>
public static class UnifiedDatabaseDeployment
{
    public static string GetValidatedConnection(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("UnifiedDideConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ConnectionStrings:UnifiedDideConnection is required.");
        var target = new SqlConnectionStringBuilder(connection);
        if (string.IsNullOrWhiteSpace(target.DataSource) || string.IsNullOrWhiteSpace(target.InitialCatalog) ||
            new[] { "master", "model", "msdb", "tempdb", "tesis_unified_poc" }.Contains(target.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unified deployment requires a dedicated database, not a system database or historical POC.");
        foreach (var name in new[] { "DefaultConnection", "ArticlesOltpConnection", "ArticlesOlapConnection" })
        {
            var other = configuration.GetConnectionString(name);
            if (!string.IsNullOrWhiteSpace(other) && string.Equals(target.InitialCatalog,
                new SqlConnectionStringBuilder(other).InitialCatalog, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unified destination matches a legacy/DW database. Stopping.");
        }
        return connection;
    }

    public static async Task MigrateAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var connection = GetValidatedConnection(configuration);
        await using var context = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options);
        // Data conflicts propagate as a nonzero deployment exit code; no automatic repairs.
        await context.Database.MigrateAsync(cancellationToken);
        Console.WriteLine("Unified migrations completed successfully.");
    }
}
