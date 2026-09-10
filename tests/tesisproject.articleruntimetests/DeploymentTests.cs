using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using tesisproject.backend.Data;

internal static class DeploymentTests
{
    public static async Task RunAsync()
    {
        var count = 0;
        IConfiguration Config(string? target, string? legacy = null) => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UnifiedDideConnection"] = target,
                ["ConnectionStrings:DefaultConnection"] = legacy
            }).Build();
        void Reject(IConfiguration config)
        {
            try { UnifiedDatabaseDeployment.GetValidatedConnection(config); }
            catch (InvalidOperationException) { count++; return; }
            throw new Exception("Unsafe deployment target accepted");
        }
        Reject(Config(null));
        foreach (var name in new[] { "master", "model", "msdb", "tempdb", "tesis_unified_poc" })
            Reject(Config($"Server=sql;Database={name};User Id=test;Password=test"));
        Reject(Config("Server=sql;Database=legacy", "Server=other-alias;Database=LEGACY"));
        _ = UnifiedDatabaseDeployment.GetValidatedConnection(Config("Server=sql;Database=unified;User Id=test;Password=test", "Server=sql;Database=legacy"));
        count++;

        var database = "tesis_unified_deployment_test_" + Guid.NewGuid().ToString("N");
        var connection = $@"Server=.\DINNOVA;Database={database};Integrated Security=True;TrustServerCertificate=True";
        await using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options);
        try
        {
            // The actual published entry point in Production, without JWT/external config.
            // An unreachable legacy source proves the migration job never opens it.
            for (var run = 0; run < 2; run++)
            {
                var start = new ProcessStartInfo(Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT")!, "dotnet.exe"))
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                start.ArgumentList.Add(typeof(UnifiedDideDbContext).Assembly.Location);
                start.ArgumentList.Add("--migrate-unified");
                start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
                start.Environment["DOTNET_ENVIRONMENT"] = "Production";
                start.Environment["ConnectionStrings__UnifiedDideConnection"] = connection;
                start.Environment["ConnectionStrings__DefaultConnection"] = "Server=127.0.0.1,1;Database=legacy;Integrated Security=True;Connect Timeout=1";
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode != 0) throw new Exception(await error + await output);
                count++;
            }
            if ((await db.Database.GetAppliedMigrationsAsync()).Count() != 4 || (await db.Database.GetPendingMigrationsAsync()).Any())
                throw new Exception("Deployment migration chain incomplete");
            count++;
            if (await db.ArticleReads.CountAsync() != 0) throw new Exception("Unexpected Article seed");
            count++;
        }
        finally { await db.Database.EnsureDeletedAsync(); }
        Console.WriteLine($"PASS: {count} deployment checks; Production CLI, fresh SQL migration chain, idempotent rerun, no legacy connection or invented seeds.");
    }
}
