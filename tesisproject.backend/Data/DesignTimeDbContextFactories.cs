using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using tesisproject.backend.Data.Articles;

namespace tesisproject.backend.Data;

// EF tooling only needs database configuration, not authentication or startup tasks.
internal static class DesignTimeDatabaseConfiguration
{
    public static DbContextOptions<TContext> CreateOptions<TContext>(string[] args, string connectionName)
        where TContext : DbContext
    {
        var contentRoot = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(contentRoot, "tesisproject.backend.csproj")) &&
            File.Exists(Path.Combine(contentRoot, "tesisproject.backend", "tesisproject.backend.csproj")))
        {
            contentRoot = Path.Combine(contentRoot, "tesisproject.backend");
        }

        // Use the same environment and configuration precedence as the application.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = contentRoot
        });
        var connection = builder.Configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionName} missing for environment '{builder.Environment.EnvironmentName}'. " +
                "Select the intended environment with -- --environment Development or configure the connection explicitly.");
        }

        return new DbContextOptionsBuilder<TContext>().UseSqlServer(connection, sql =>
        {
            if (typeof(TContext) == typeof(DwContext))
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "DW");
            else if (typeof(TContext) == typeof(ArticlesDbContext))
                sql.MigrationsHistoryTable("__EFMigrationsHistoryArticles", "dbo");
        }).Options;
    }
}

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabaseConfiguration.CreateOptions<AppDbContext>(args, "DefaultConnection"));
}

public sealed class DwContextFactory : IDesignTimeDbContextFactory<DwContext>
{
    public DwContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabaseConfiguration.CreateOptions<DwContext>(args, "DefaultConnection"));
}

public sealed class ArticlesDbContextFactory : IDesignTimeDbContextFactory<ArticlesDbContext>
{
    public ArticlesDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabaseConfiguration.CreateOptions<ArticlesDbContext>(args, "ArticlesOltpConnection"));
}
