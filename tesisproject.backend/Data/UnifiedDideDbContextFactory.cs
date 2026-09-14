using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace tesisproject.backend.Data;

/// <summary>Development-only tooling for the unified physical database; never starts the application.</summary>
public sealed class UnifiedDideDbContextFactory : IDesignTimeDbContextFactory<UnifiedDideDbContext>
{
    public UnifiedDideDbContext CreateDbContext(string[] args)
    {
        var environment = ReadEnvironment(args);
        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unified tooling only supports Development/local configuration.");

        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "tesisproject.backend.csproj")))
            root = Path.Combine(root, "tesisproject.backend");
        var configuration = new ConfigurationBuilder().SetBasePath(root)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("UnifiedDideConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:UnifiedDideConnection is required.");
        ValidateDestination(connection, new[] { "ProjectsDwConnection", "ArticlesDwConnection" }
            .Select(configuration.GetConnectionString));

        return new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options);
    }

    // Also used by the database validation executable before opening a connection.
    public static void ValidateDestination(string connection, IEnumerable<string?> applicationConnections)
    {
        var target = new SqlConnectionStringBuilder(connection);
        var server = target.DataSource.Split('\\')[0].Split(',')[0];
        var isLocal = new[] { ".", "(local)", "(localdb)", "localhost", "127.0.0.1", Environment.MachineName }
            .Contains(server, StringComparer.OrdinalIgnoreCase);
        if (!isLocal || !target.IntegratedSecurity)
            throw new InvalidOperationException("Unified tooling requires a local server and Windows authentication; production credentials are not supported.");
        if (string.IsNullOrWhiteSpace(target.InitialCatalog) ||
            new[] { "master", "model", "msdb", "tempdb", "tesis_unified_poc" }.Contains(target.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unified must target a separate development database, not a system database or the applied POC.");
        foreach (var current in applicationConnections.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var other = new SqlConnectionStringBuilder(current);
            if (!string.Equals(target.DataSource, other.DataSource, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(target.InitialCatalog, other.InitialCatalog, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("All application contexts must target the same SQL Server database.");
        }
    }

    private static string ReadEnvironment(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
                return args[i]["--environment=".Length..];
            if (args[i].Equals("--environment", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }
}
