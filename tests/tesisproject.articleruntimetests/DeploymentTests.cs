using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Analytic.Implementations;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Errors;

internal static class DeploymentTests
{
    public static async Task RunAsync()
    {
        var count = 0;
        IConfiguration Config(string? unified, string? projects = null, string? articles = null) => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UnifiedDideConnection"] = unified,
                ["ConnectionStrings:ProjectsDwConnection"] = projects,
                ["ConnectionStrings:ArticlesDwConnection"] = articles,
                ["ExternalApis:BaseUrl"] = "http://127.0.0.1:1",
                ["ExternalApis:TimeoutSeconds"] = "1",
                ["Storage:RootPath"] = Path.GetTempPath(),
                ["AdministrativeOperations:Enabled"] = "true",
                ["AdministrativeOperations:Secret"] = "deployment-test-secret"
            }).Build();
        void Reject(IConfiguration config)
        {
            try { UnifiedDatabaseDeployment.GetValidatedConnection(config); }
            catch (InvalidOperationException) { count++; return; }
            throw new Exception("Unsafe deployment target accepted");
        }
        Reject(Config(null));
        foreach (var name in new[] { "master", "model", "msdb", "tempdb", "tesis_unified_poc" })
            Reject(Config(
                $"Server=sql;Database={name};User Id=test;Password=test",
                $"Server=sql;Database={name};User Id=test;Password=test",
                $"Server=sql;Database={name};User Id=test;Password=test"));
        Reject(Config(
            "Server=sql;Database=unified;User Id=test;Password=test",
            "Server=sql;Database=tesis;User Id=test;Password=test",
            "Server=sql;Database=unified;User Id=test;Password=test"));
        var valid = "Server=sql;Database=unified;User Id=test;Password=test";
        _ = UnifiedDatabaseDeployment.GetValidatedConnection(Config(valid, valid, valid));
        count++;

        var database = Environment.GetEnvironmentVariable("DEPLOYMENT_TEST_DATABASE")
            ?? "tesis_unified_deployment_test_" + Guid.NewGuid().ToString("N");
        var preserveDatabase = string.Equals(
            Environment.GetEnvironmentVariable("DEPLOYMENT_TEST_PRESERVE"), "true", StringComparison.OrdinalIgnoreCase);
        var connection = $@"Server=.\DINNOVA;Database={database};Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
        await using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")).Options);
        try
        {
            if (await db.Database.CanConnectAsync())
                throw new Exception("Temporary database unexpectedly existed before bootstrap");
            count++;

            // The actual published entry point in Production, without HTTP or Identity startup.
            for (var run = 0; run < 2; run++)
            {
                var dotnetHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
                var start = new ProcessStartInfo(dotnetHost)
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                start.ArgumentList.Add(typeof(UnifiedDideDbContext).Assembly.Location);
                start.ArgumentList.Add("--migrate-database");
                start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
                start.Environment["DOTNET_ENVIRONMENT"] = "Production";
                start.Environment["ConnectionStrings__UnifiedDideConnection"] = connection;
                start.Environment["ConnectionStrings__ProjectsDwConnection"] = connection;
                start.Environment["ConnectionStrings__ArticlesDwConnection"] = connection;
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode != 0) throw new Exception(await error + await output);
                count++;
            }

            await using var projects = new ProjectsDwContext(new DbContextOptionsBuilder<ProjectsDwContext>()
                .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "ProjectsDW")).Options);
            await using var articles = new ArticlesDwContext(new DbContextOptionsBuilder<ArticlesDwContext>()
                .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "ArticlesDW")).Options);

            static bool OwnsOnlySchema(DbContext context, string schema)
            {
                string? EffectiveSchema(Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType entity) =>
                    entity.GetSchema() ?? context.Model.GetDefaultSchema();
                return context.Model.GetEntityTypes().All(entity =>
                    string.Equals(EffectiveSchema(entity), schema, StringComparison.OrdinalIgnoreCase) &&
                    entity.GetForeignKeys().All(foreignKey =>
                        string.Equals(EffectiveSchema(foreignKey.PrincipalEntityType), schema, StringComparison.OrdinalIgnoreCase)));
            }
            if (!OwnsOnlySchema(db, "dbo") || !OwnsOnlySchema(projects, "ProjectsDW") ||
                !OwnsOnlySchema(articles, "ArticlesDW"))
                throw new Exception("DbContext schema isolation failed");
            count++;

            foreach (var context in new DbContext[] { db, projects, articles })
            {
                if ((await context.Database.GetAppliedMigrationsAsync()).Count() != context.Database.GetMigrations().Count() ||
                    (await context.Database.GetPendingMigrationsAsync()).Any())
                    throw new Exception($"{context.GetType().Name} migration chain incomplete");
                count++;
            }

            await using var sql = new SqlConnection(connection);
            await sql.OpenAsync();
            async Task<int> ScalarAsync(string commandText)
            {
                await using var command = new SqlCommand(commandText, sql);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }

            if (await ScalarAsync("SELECT COUNT(*) FROM sys.schemas WHERE name IN ('dbo','ProjectsDW','ArticlesDW')") != 3)
                throw new Exception("Expected schemas missing");
            count++;
            if (await ScalarAsync("SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.name='__EFMigrationsHistory' AND s.name IN ('dbo','ProjectsDW','ArticlesDW')") != 3)
                throw new Exception("Independent migration histories missing");
            count++;
            if (await ScalarAsync("SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE (s.name='dbo' AND t.name IN ('Projects','Products','Articles','OperationExecutionHistory')) OR (s.name='ProjectsDW' AND t.name IN ('FactProjects','FactBudgets','FactProducts')) OR (s.name='ArticlesDW' AND t.name IN ('DimArticles','FactArticlePublications','FactArticleAuthors'))") != 10)
                throw new Exception("Representative context tables missing");
            count++;

            var configuration = Config(connection, connection, connection);
            var services = new ServiceCollection();
            services.AddUnifiedDide(configuration, options => options.UseSqlServer(connection,
                sqlOptions => sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")));
            services.AddDbContext<ProjectsDwContext>(options => options.UseSqlServer(connection,
                sqlOptions => sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ProjectsDW")));
            services.AddDbContext<ArticlesDwContext>(options => options.UseSqlServer(connection,
                sqlOptions => sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ArticlesDW")));
            services.AddScoped<IProjectsDwEtlService, ProjectsDwEtlService>();
            services.AddScoped<IArticlesDwEtlService, ArticlesDwEtlService>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var dataMigrations = scope.ServiceProvider.GetRequiredService<IDataMigrationService>();
            var listed = await dataMigrations.ListAsync();
            if (!listed.Success || listed.Data is null || listed.Data.Count != 2 ||
                listed.Data[0].Code != OperationCodes.ProjectsInitialCatalogV1 ||
                listed.Data[1].Code != OperationCodes.UnifiedArticlesCatalogsV1)
                throw new Exception("Expected ordered data migrations are unavailable through generic registry");
            count++;
            var projectsCatalogs = await dataMigrations.ApplyAsync(
                OperationCodes.ProjectsInitialCatalogV1, "deployment-test-secret", null, "Deployment Test");
            var articlesCatalogs = await dataMigrations.ApplyAsync(
                OperationCodes.UnifiedArticlesCatalogsV1, "deployment-test-secret", null, "Deployment Test");
            if (!projectsCatalogs.Success || !articlesCatalogs.Success)
                throw new Exception("Canonical data migrations failed on fresh deployment database");
            count++;
            var articlesSecondApply = await dataMigrations.ApplyAsync(
                OperationCodes.UnifiedArticlesCatalogsV1, "deployment-test-secret", null, "Deployment Test");
            if (articlesSecondApply.ErrorCode != ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied)
                throw new Exception("Articles data migration rerun was not rejected idempotently");
            count++;
            if (await db.PublicationStatuses.CountAsync() != 3 ||
                await db.ResearchLines.CountAsync() != 16 ||
                await db.BroadFields.CountAsync() != 9 ||
                await db.SpecificFields.CountAsync() != 25 ||
                await db.DetailedFields.CountAsync() != 90 ||
                await db.IndexingSources.CountAsync() != 8)
                throw new Exception("Articles canonical catalog counts are incomplete");
            count++;
            var projectsResult = await scope.ServiceProvider.GetRequiredService<IProjectsDwEtlService>().RunFullLoadAsync();
            if (!projectsResult.Success) throw new Exception("Projects full load returned failure");
            count++;
            var articlesResult = await scope.ServiceProvider.GetRequiredService<IArticlesDwEtlService>().RunFullLoadAsync();
            if (!articlesResult.Success) throw new Exception("Articles full load returned failure");
            count++;
            if (await ScalarAsync("SELECT COUNT(*) FROM dbo.OperationExecutionHistory WHERE OperationType='ETL' AND OperationCode IN ('PROJECTS_DW_FULL_LOAD','ARTICLES_DW_FULL_LOAD') AND Status='SUCCEEDED'") != 2)
                throw new Exception("ETL execution ledger entries missing");
            count++;

            if (await db.ArticleReads.CountAsync() != 0) throw new Exception("Unexpected Article seed");
            count++;
        }
        finally
        {
            if (!preserveDatabase) await db.Database.EnsureDeletedAsync();
        }
        Console.WriteLine($"PASS: {count} deployment checks; fresh single database, three migration streams, ordered data migrations and idempotent rerun.");
    }
}
