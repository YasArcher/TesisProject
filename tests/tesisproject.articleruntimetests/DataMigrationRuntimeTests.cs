using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified.Contracts.Administration;
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
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options;
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
            var service = new DataMigrationService([new ProjectsInitialCatalogV1(db)], history, db,
                Options.Create(new AdministrativeOperationsOptions { Enabled = true, Secret = "runtime-test-secret" }));
            var before = await service.ListAsync();
            Check(before.Success && before.Data!.Single().Applied == false, "PROJECTS_INITIAL_CATALOG_V1 starts pending");

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
                    "SELECT COUNT(*) AS [Value] FROM dbo.__EFMigrationsHistoryUnifiedDide WHERE [MigrationId] LIKE '%PROJECTS_INITIAL_CATALOG_V1%'")
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

            var second = await service.ApplyAsync(OperationCodes.ProjectsInitialCatalogV1,
                "runtime-test-secret", null, "Runtime Test");
            Check(second.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied,
                "Second apply returns DATA_MIGRATION_ALREADY_APPLIED");
            Check(await db.OperationExecutionHistories.CountAsync(x =>
                x.OperationType == OperationExecutionTypes.DataMigration &&
                x.Status == OperationExecutionStatuses.Succeeded) == 1,
                "Only one successful execution exists");

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
