using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace tesisproject.backend.Data;

/// <summary>Explicit deployment operation, separate from local tooling and legacy bootstrap.</summary>
public static class UnifiedDatabaseDeployment
{
    private static readonly string[] ConnectionNames =
        ["UnifiedDideConnection", "ProjectsDwConnection", "ArticlesDwConnection"];

    public static string GetValidatedConnection(IConfiguration configuration)
    {
        var connections = ConnectionNames.ToDictionary(
            name => name,
            name => configuration.GetConnectionString(name)
                ?? throw new InvalidOperationException($"ConnectionStrings:{name} is required."));

        var connection = connections["UnifiedDideConnection"];
        var target = new SqlConnectionStringBuilder(connection);
        if (string.IsNullOrWhiteSpace(target.DataSource) || string.IsNullOrWhiteSpace(target.InitialCatalog) ||
            new[] { "master", "model", "msdb", "tempdb", "tesis_unified_poc" }.Contains(target.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unified deployment requires a dedicated database, not a system database or historical POC.");

        foreach (var name in ConnectionNames.Skip(1))
        {
            var other = new SqlConnectionStringBuilder(connections[name]);
            var sameCredentials = target.IntegratedSecurity == other.IntegratedSecurity &&
                (target.IntegratedSecurity || string.Equals(target.UserID, other.UserID, StringComparison.OrdinalIgnoreCase));
            if (!string.Equals(target.DataSource, other.DataSource, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(target.InitialCatalog, other.InitialCatalog, StringComparison.OrdinalIgnoreCase) ||
                !sameCredentials)
                throw new InvalidOperationException(
                    "UnifiedDideConnection, ProjectsDwConnection and ArticlesDwConnection must target the same server, database and user.");
        }

        return connection;
    }

    public static async Task MigrateAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var connection = GetValidatedConnection(configuration);

        await using (var unified = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")).Options))
        {
            await NormalizeUnifiedHistoryAsync(unified, cancellationToken);
            await unified.Database.MigrateAsync(cancellationToken);
            Console.WriteLine("UnifiedDideDbContext migrations completed successfully.");
        }

        await using (var projects = new ProjectsDwContext(new DbContextOptionsBuilder<ProjectsDwContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "ProjectsDW")).Options))
        {
            await NormalizeProjectsHistoryAsync(projects, cancellationToken);
            await projects.Database.MigrateAsync(cancellationToken);
            Console.WriteLine("ProjectsDwContext migrations completed successfully.");
        }

        await using (var articles = new ArticlesDwContext(new DbContextOptionsBuilder<ArticlesDwContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "ArticlesDW")).Options))
        {
            await articles.Database.MigrateAsync(cancellationToken);
            Console.WriteLine("ArticlesDwContext migrations completed successfully.");
        }
    }

    private static async Task NormalizeUnifiedHistoryAsync(
        UnifiedDideDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
            return;

        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[__EFMigrationsHistoryUnifiedDide]', N'U') IS NOT NULL
            BEGIN
                IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
                    THROW 51000, 'Both Unified migration history tables exist; automatic normalization stopped.', 1;
                EXEC sp_rename N'dbo.__EFMigrationsHistoryUnifiedDide', N'__EFMigrationsHistory';
            END
            """, cancellationToken);
    }

    private static Task NormalizeProjectsHistoryAsync(
        ProjectsDwContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[DW].[__EFMigrationsHistory]', N'U') IS NOT NULL
            BEGIN
                IF SCHEMA_ID(N'ProjectsDW') IS NULL
                    EXEC(N'CREATE SCHEMA [ProjectsDW]');
                IF OBJECT_ID(N'[ProjectsDW].[__EFMigrationsHistory]', N'U') IS NOT NULL
                    THROW 51001, 'Both Projects DW migration history tables exist; automatic normalization stopped.', 1;
                ALTER SCHEMA [ProjectsDW] TRANSFER [DW].[__EFMigrationsHistory];
            END
            """, cancellationToken);
}
