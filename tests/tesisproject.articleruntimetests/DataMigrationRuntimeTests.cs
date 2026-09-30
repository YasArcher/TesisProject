using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.DataMigrations.ArticlesInitialCatalog;
using tesisproject.backend.Services.Unified.DataMigrations.ProjectsInitialCatalog;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Options;
using tesisproject.shared.Errors;

internal static class DataMigrationRuntimeTests
{
    public static async Task RunAsync()
    {
        var database = "tesis_data_migration_test_" + Guid.NewGuid().ToString("N");
        var baseConnection = Environment.GetEnvironmentVariable("DATA_MIGRATION_TEST_CONNECTION")
            ?? @"Server=.\DINNOVA;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = database,
            TrustServerCertificate = true
        };
        var connectionString = builder.ConnectionString;
        var options = new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")).Options;
        var checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            checks++;
            Console.WriteLine("PASS: " + message);
        }

        await using var db = new UnifiedDideDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT dbo.ProductTypes ON; INSERT dbo.ProductTypes (Id,Name,IsActive,IsLocked) VALUES (1,N'Producto smoke',1,0); SET IDENTITY_INSERT dbo.ProductTypes OFF;");
            var history = new OperationExecutionHistoryService(new OperationExecutionHistoryRepository(db));
            var service = new DataMigrationService(
                [new ProjectsInitialCatalogV1(db), new UnifiedArticlesCatalogsV1(db)], history, db,
                Options.Create(new AdministrativeOperationsOptions { Enabled = true, Secret = "runtime-test-secret" }));
            var before = await service.ListAsync();
            Check(before.Success && before.Data!.Count == 2 && before.Data.All(x => !x.Applied) &&
                  before.Data[0].Code == OperationCodes.ProjectsInitialCatalogV1 &&
                  before.Data[1].Code == OperationCodes.UnifiedArticlesCatalogsV1,
                "Generic endpoint registry lists both ordered data migrations as pending");

            var missingDependency = await service.ApplyAsync(OperationCodes.UnifiedArticlesCatalogsV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(missingDependency.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationFailed &&
                  !await db.PublicationStatuses.AnyAsync() &&
                  await db.OperationExecutionHistories.AnyAsync(x =>
                      x.OperationCode == OperationCodes.UnifiedArticlesCatalogsV1 &&
                      x.Status == OperationExecutionStatuses.Failed),
                "Articles catalog migration fails cleanly before PROJECTS_INITIAL_CATALOG_V1");

            var applied = await service.ApplyAsync(OperationCodes.ProjectsInitialCatalogV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(applied.Success && applied.Data!.Status == OperationExecutionStatuses.Succeeded,
                "C# data migration succeeds");
            var execution = await db.OperationExecutionHistories.AsNoTracking().SingleAsync(x =>
                x.OperationCode == OperationCodes.ProjectsInitialCatalogV1);
            Check(execution.Status == OperationExecutionStatuses.Succeeded &&
                  execution.OperationType == OperationExecutionTypes.DataMigration,
                "OperationExecutionHistory records successful data migration");
            Check(await db.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM dbo.__EFMigrationsHistory WHERE [MigrationId] LIKE '%PROJECTS_INITIAL_CATALOG_V1%'")
                    .SingleAsync() == 0,
                "Data migration is absent from EF schema migration history");
            var result = JsonDocument.Parse(execution.ResultJson!);
            Check(result.RootElement.GetProperty("inserted").GetInt32() == 464 &&
                  result.RootElement.GetProperty("updated").GetInt32() == 1,
                "ResultJson stores compact 465-row summary and exact smoke correction");

            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync("scripts/seeds/projects-unified.manifest.json"));
            foreach (var table in manifest.RootElement.GetProperty("rows").EnumerateObject())
            {
                var entityType = db.Model.GetEntityTypes().Single(x => x.GetTableName() == table.Name);
                var count = await CountAsync(db, entityType.ClrType);
                Check(count == table.Value.GetInt32(), "Equivalent canonical count: " + table.Name);
            }
            Check((await db.ProductTypes.AsNoTracking().SingleAsync(x => x.Id == 1)).Name == "PRODUCCIÓN CIENTÍFICA",
                "Unicode: PRODUCCIÓN CIENTÍFICA");
            Check(await db.ResearchCategoryGroups.AnyAsync(x => x.Name == "Áreas de Investigación"),
                "Unicode: Áreas de Investigación");
            Check(await db.ResearchCategoryTypes.AnyAsync(x => x.Name == "Línea de investigación"),
                "Unicode: Línea de investigación");
            Check(!await db.Faculties.AnyAsync() && !await db.AcademicTerms.AnyAsync() && !await db.Users.AnyAsync(),
                "Data migration does not create synchronized catalogs or Identity users");
            Check((await new UnifiedProjectRepository(db)
                    .ListIdentifiersByCodesAsync(["TRANSLATION-PROBE"])).Count == 0,
                "Project identifier projection executes on SQL Server");

            var articlesApplied = await service.ApplyAsync(OperationCodes.UnifiedArticlesCatalogsV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(articlesApplied.Success && articlesApplied.Data!.Status == OperationExecutionStatuses.Succeeded,
                "UNIFIED_ARTICLES_CATALOGS_V1 succeeds after shared catalog dependency");
            var articlesExecution = await db.OperationExecutionHistories.AsNoTracking().SingleAsync(x =>
                x.OperationCode == OperationCodes.UnifiedArticlesCatalogsV1 &&
                x.Status == OperationExecutionStatuses.Succeeded);
            using var articlesResult = JsonDocument.Parse(articlesExecution.ResultJson!);
            Check(articlesResult.RootElement.GetProperty("inserted").GetInt32() == 216 &&
                  articlesResult.RootElement.GetProperty("catalogs").GetProperty("DetailedFields").GetInt32() == 90 &&
                  articlesResult.RootElement.GetProperty("catalogs").GetProperty("FieldCatalog").GetInt32() == 40 &&
                  articlesResult.RootElement.GetProperty("catalogs").GetProperty("FormDefinitions").GetInt32() == 2 &&
                  articlesResult.RootElement.GetProperty("catalogs").GetProperty("FormFields").GetInt32() == 23,
                "Articles ResultJson stores compact 216-row catalog and form summary");
            Check(await db.PublicationStatuses.CountAsync() == 3 &&
                  await db.ResearchLines.CountAsync() == 16 &&
                  await db.BroadFields.CountAsync() == 9 &&
                  await db.SpecificFields.CountAsync() == 25 &&
                  await db.DetailedFields.CountAsync() == 90 &&
                  await db.IndexingSources.CountAsync() == 8 &&
                  await db.FieldCatalogEntries.CountAsync() == 40 &&
                  await db.FormDefinitions.CountAsync() == 2 &&
                  await db.FormFieldDefinitions.CountAsync() == 23,
                "Articles initial catalog and form counts match accepted backup rows");
            Check(await db.FormDefinitions.AllAsync(x => x.IsActive) &&
                  await db.FormFieldDefinitions.AllAsync(x =>
                      db.FormDefinitions.Any(form => form.FormId == x.FormId) &&
                      db.FieldCatalogEntries.Any(field => field.FieldId == x.FieldId)),
                "Articles form definitions preserve valid field catalog relationships");
            Check(!await db.PublicationStatuses.AnyAsync(x => x.Name.Contains("DW")) &&
                  !await db.ResearchLines.AnyAsync(x => x.Name.StartsWith("DW")) &&
                  !await db.BroadFields.AnyAsync(x => x.BroadFieldId >= 1000) &&
                  !await db.FieldCatalogEntries.AnyAsync(x => x.FieldId == 33 ||
                      (x.FieldId >= 1037 && x.FieldId <= 1049)),
                "Articles migration excludes DW and uncertain high-ID fixtures");
            Check(await db.ProductAttributeDefinitions.CountAsync(x => x.ProductTypeId == 2) == 6 &&
                  !await db.ProductAttributeDefinitions.AnyAsync(x => x.ProductTypeId == 2 &&
                      new[] { 5, 6, 8, 9 }.Contains(x.ProductAttributeId)),
                "Regional production keeps only canonical attributes 1, 2, 3, 4, 7 and 10");

            var secondArticles = await service.ApplyAsync(OperationCodes.UnifiedArticlesCatalogsV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(secondArticles.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied,
                "Second Articles apply returns DATA_MIGRATION_ALREADY_APPLIED");

            var second = await service.ApplyAsync(OperationCodes.ProjectsInitialCatalogV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(second.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied,
                "Second apply returns DATA_MIGRATION_ALREADY_APPLIED");
            Check(await db.OperationExecutionHistories.CountAsync(x =>
                x.OperationType == OperationExecutionTypes.DataMigration &&
                x.Status == OperationExecutionStatuses.Succeeded) == 2,
                "Exactly one successful execution exists per canonical data migration");

            db.OperationExecutionHistories.Add(new OperationExecutionHistory
            {
                ExecutionId = Guid.NewGuid(), OperationType = OperationExecutionTypes.ManualProcess,
                OperationCode = "INVALID_JSON_TEST", Status = OperationExecutionStatuses.Running,
                StartedAt = DateTime.UtcNow, ResultJson = "not-json"
            });
            try
            {
                await db.SaveChangesAsync();
                throw new InvalidOperationException("Invalid JSON was accepted.");
            }
            catch (DbUpdateException) { db.ChangeTracker.Clear(); Check(true, "SQL rejects invalid ResultJson"); }

            var retryMigration = new FailOnceMigration(db);
            var retryService = new DataMigrationService([retryMigration], history, db,
                Options.Create(new AdministrativeOperationsOptions { Enabled = true, Secret = "runtime-test-secret" }));
            var failedAttempt = await retryService.ApplyAsync(retryMigration.Code,
                "runtime-test-secret", null, "Runtime Test");
            Check(failedAttempt.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationFailed &&
                  !await db.Countries.AnyAsync(x => x.Name == FailOnceMigration.ProbeCountryName) &&
                  await db.OperationExecutionHistories.AnyAsync(x => x.OperationCode == retryMigration.Code &&
                      x.Status == OperationExecutionStatuses.Failed),
                "Failed data migration rolls back business data and remains FAILED");
            var retried = await retryService.ApplyAsync(retryMigration.Code,
                "runtime-test-secret", null, "Runtime Test");
            Check(retried.Success && await db.Countries.AnyAsync(x => x.Name == FailOnceMigration.ProbeCountryName) &&
                  await db.OperationExecutionHistories.CountAsync(x => x.OperationCode == retryMigration.Code &&
                      x.Status == OperationExecutionStatuses.Succeeded) == 1,
                "Retry after FAILED succeeds exactly once");

            var barrier = new AsyncTwoPartyBarrier();
            await using var concurrentDb1 = new UnifiedDideDbContext(options);
            await using var concurrentDb2 = new UnifiedDideDbContext(options);
            var concurrent1 = CreateService(concurrentDb1, new ConcurrentProbeMigration(barrier));
            var concurrent2 = CreateService(concurrentDb2, new ConcurrentProbeMigration(barrier));
            var concurrentResults = await Task.WhenAll(
                concurrent1.ApplyAsync(ConcurrentProbeMigration.MigrationCode, "runtime-test-secret", null, "Runtime Test"),
                concurrent2.ApplyAsync(ConcurrentProbeMigration.MigrationCode, "runtime-test-secret", null, "Runtime Test"));
            db.ChangeTracker.Clear();
            Check(concurrentResults.Count(x => x.Success) == 1 &&
                  await db.OperationExecutionHistories.CountAsync(x =>
                      x.OperationCode == ConcurrentProbeMigration.MigrationCode &&
                      x.Status == OperationExecutionStatuses.Succeeded) == 1,
                "Concurrent apply cannot create two SUCCEEDED executions");

            DataMigrationService CreateService(UnifiedDideDbContext migrationDb, IDataMigration migration)
                => new([migration],
                    new OperationExecutionHistoryService(new OperationExecutionHistoryRepository(migrationDb)),
                    migrationDb, Options.Create(new AdministrativeOperationsOptions
                    { Enabled = true, Secret = "runtime-test-secret" }));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
        Console.WriteLine($"PASS: {checks} C# data migration runtime checks.");
    }

    private static async Task<int> CountAsync(UnifiedDideDbContext context, Type entityType)
    {
        var method = typeof(DataMigrationRuntimeTests).GetMethod(nameof(CountGenericAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(entityType);
        return await (Task<int>)method.Invoke(null, [context])!;
    }

    private static Task<int> CountGenericAsync<TEntity>(UnifiedDideDbContext context) where TEntity : class
        => context.Set<TEntity>().CountAsync();

    private sealed class FailOnceMigration(UnifiedDideDbContext context) : IDataMigration
    {
        public const string ProbeCountryName = "Prueba de rollback";
        private bool _fail = true;
        public string Code => "RUNTIME_FAIL_ONCE_V1";
        public string Description => "Runtime rollback probe";
        public string? Version => "1";
        public int Order => 1;

        public async Task<object?> ApplyAsync(CancellationToken ct = default)
        {
            context.Countries.Add(new Country
            {
                Name = ProbeCountryName,
                IsoCode = "ZZ",
                IsoAlpha3 = "ZZZ"
            });
            await context.SaveChangesAsync(ct);
            if (_fail)
            {
                _fail = false;
                throw new InvalidOperationException("Injected rollback probe.");
            }
            return new { inserted = 1 };
        }
    }

    private sealed class ConcurrentProbeMigration(AsyncTwoPartyBarrier barrier) : IDataMigration
    {
        public const string MigrationCode = "RUNTIME_CONCURRENT_V1";
        public string Code => MigrationCode;
        public string Description => "Runtime concurrency probe";
        public string? Version => "1";
        public int Order => 1;
        public async Task<object?> ApplyAsync(CancellationToken ct = default)
        {
            await barrier.SignalAndWaitAsync(ct);
            return new { completed = true };
        }
    }

    private sealed class AsyncTwoPartyBarrier
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public async Task SignalAndWaitAsync(CancellationToken ct)
        {
            if (Interlocked.Increment(ref _arrivals) == 2) _release.TrySetResult();
            await _release.Task.WaitAsync(ct);
        }
    }
}
