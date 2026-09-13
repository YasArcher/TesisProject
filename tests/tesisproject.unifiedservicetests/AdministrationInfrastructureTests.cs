using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;
using System.Reflection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.DataMigrations.ProjectsInitialCatalog;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class AdministrationInfrastructureTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        await using var context = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var repository = new OperationExecutionHistoryRepository(context);
        var history = new OperationExecutionHistoryService(repository);
        var running = await history.StartAsync(new(OperationExecutionTypes.ManualProcess, "REPEATABLE_TEST"));
        check(running.Status == OperationExecutionStatuses.Running && running.StartedAt.Kind == DateTimeKind.Utc,
            "History starts RUNNING in UTC.");
        var success = await history.CompleteSuccessAsync(running.ExecutionId, new { count = 1 });
        check(success.Status == OperationExecutionStatuses.Succeeded && success.ResultJson == "{\"count\":1}",
            "History stores typed JSON summary.");
        var failedStart = await history.StartAsync(new(OperationExecutionTypes.ManualProcess, "REPEATABLE_TEST"));
        var failed = await history.CompleteFailureAsync(failedStart.ExecutionId, "SAFE_ERROR", "Safe failure");
        check(failed.Status == OperationExecutionStatuses.Failed &&
              (await history.ListAsync(new(OperationCode: "REPEATABLE_TEST"))).Total == 2,
            "Failed attempts remain and repeatable operations repeat.");

        var model = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OperationExecutionHistory))!;
        check(model.GetIndexes().Single(x => x.IsUnique && x.Properties.Single().Name == nameof(OperationExecutionHistory.ExecutionId)) is not null,
            "ExecutionId unique index mapped.");
        check(model.GetIndexes().Any(x => x.IsUnique && x.GetFilter()?.Contains("DATA_MIGRATION") == true),
            "Successful data migration filtered unique index mapped.");
        check(model.GetIndexes().Any(x => x.IsUnique && x.GetFilter()?.Contains("PROJECTS_MATRIX_IMPORT") == true),
            "Successful Matrix content hash filtered unique index mapped.");
        check(model.GetCheckConstraints().Any(x => x.Sql.Contains("ISJSON", StringComparison.Ordinal)),
            "ResultJson ISJSON constraint mapped.");

        var fakeMigration = new FakeMigration();
        var disabled = new DataMigrationService([fakeMigration], history, context,
            Options.Create(new AdministrativeOperationsOptions { Enabled = false, Secret = "independent-secret" }));
        var disabledResult = await disabled.ApplyAsync(fakeMigration.Code, "independent-secret", 1, "Admin");
        check(disabledResult.ErrorCode == ErrorCodes.AdministrativeOperations.Disabled && fakeMigration.Applies == 0,
            "Disabled administration fails closed.");
        var enabled = new DataMigrationService([fakeMigration], history, context,
            Options.Create(new AdministrativeOperationsOptions { Enabled = true, Secret = "independent-secret" }));
        var unconfigured = new DataMigrationService([fakeMigration], history, context,
            Options.Create(new AdministrativeOperationsOptions { Enabled = true, Secret = "" }));
        check((await unconfigured.ApplyAsync(fakeMigration.Code, "anything", 1, "Admin")).ErrorCode ==
              ErrorCodes.AdministrativeOperations.ConfigurationInvalid,
            "Enabled administration without configured secret fails closed.");
        var wrong = await enabled.ApplyAsync(fakeMigration.Code, "wrong", 1, "Admin");
        check(wrong.ErrorCode == ErrorCodes.AdministrativeOperations.SecretInvalid && fakeMigration.Applies == 0,
            "Wrong secret rejected.");
        check(!JsonSerializer.Serialize(wrong).Contains("wrong", StringComparison.Ordinal),
            "Supplied secret is absent from the response.");
        var applied = await enabled.ApplyAsync(fakeMigration.Code, "independent-secret", 1, "Admin");
        check(applied.Success && fakeMigration.Applies == 1, "Correct secret applies data migration once.");
        var repeated = await enabled.ApplyAsync(fakeMigration.Code, "independent-secret", 1, "Admin");
        check(repeated.ErrorCode == ErrorCodes.AdministrativeOperations.DataMigrationAlreadyApplied && fakeMigration.Applies == 1,
            "Applied migration is not rerun.");
        check(!context.OperationExecutionHistories.Any(x =>
            (x.ResultJson ?? "").Contains("independent-secret", StringComparison.Ordinal)),
            "Administrative secret is absent from history.");

        check(ProjectsSeedData.Tables.Count == 24 && ProjectsSeedData.Tables.Sum(x => x.Count) == 465,
            "C# Projects seed preserves 24 sections and 465 rows.");
        var seedText = File.ReadAllText(Path.Combine("tesisproject.backend", "Services", "Unified",
            "DataMigrations", "ProjectsInitialCatalog", "ProjectsSeedData.cs"));
        check(seedText.Contains("PRODUCCIÓN CIENTÍFICA", StringComparison.Ordinal) &&
              seedText.Contains("Áreas de Investigación", StringComparison.Ordinal) &&
              seedText.Contains("Línea de investigación", StringComparison.Ordinal),
            "C# seed preserves canonical Unicode.");

        var adminController = typeof(UnifiedAdministrativeOperationsController);
        check(adminController.GetCustomAttribute<AuthorizeAttribute>()?.Roles == "superadmin" &&
              adminController.GetCustomAttribute<RouteAttribute>()?.Template == "api/admin",
            "Administrative API requires superadmin.");
        var applyAction = adminController.GetMethod("ApplyDataMigration")!;
        check(applyAction.GetParameters().Single(x => x.Name == "secret")
                  .GetCustomAttribute<FromHeaderAttribute>()?.Name == "X-Admin-Operation-Secret",
            "Data migration mutation reads secret only from header.");
        await VerifyAdministrativeAuthorizationAsync(check);

        await VerifyMatrixTrackingAsync(context, history, check);
    }

    private static async Task VerifyAdministrativeAuthorizationAsync(Action<bool, string> check)
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("superadmin").Build();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "user")], "test"));
        var superadmin = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "superadmin")], "test"));
        check(!(await authorization.AuthorizeAsync(anonymous, null, policy)).Succeeded,
            "Administrative policy rejects unauthenticated callers.");
        check(!(await authorization.AuthorizeAsync(user, null, policy)).Succeeded,
            "Administrative policy rejects authenticated non-superadmin callers.");
        check((await authorization.AuthorizeAsync(superadmin, null, policy)).Succeeded,
            "Administrative policy accepts superadmin before independent-secret validation.");
    }

    private static async Task VerifyMatrixTrackingAsync(UnifiedDideDbContext context,
        IOperationExecutionHistoryService history, Action<bool, string> check)
    {
        var persistedCodeRequested = false;
        var projectRepository = Stub.For<IUnifiedProjectRepository>((method, args) =>
        {
            if (method.Name != "ListIdentifiersByCodesAsync")
                throw new InvalidOperationException(method.Name);
            persistedCodeRequested = ((IReadOnlyCollection<string>)args[0]!).Contains("PFCIAL-28-28");
            return Task.FromResult<IReadOnlyList<ProjectIdentifierRow>>(
                [new(429, "PFCIAL-28-28", 28)]);
        });
        var uow = Stub.For<IUnifiedUnitOfWork>((method, _) => method.Name switch
        {
            "get_Projects" => projectRepository,
            _ => throw new InvalidOperationException(method.Name)
        });
        var projectService = Stub.For<IUnifiedProjectService>((method, args) => method.Name switch
        {
            "ImportFromMatrixAsync" => CompleteImportAsync(args),
            _ => throw new InvalidOperationException(method.Name)
        });
        var user = Stub.For<IUnifiedArticleUserContext>((method, _) => method.Name switch
        {
            "GetAppUserIdAsync" => Task.FromResult<int?>(7),
            "get_DisplayName" => "Admin Test",
            _ => false
        });
        var service = new UnifiedProjectMatrixService(projectService,
            NullLogger<UnifiedProjectMatrixService>.Instance, uow, history, user);

        byte[] content;
        using (var workbook = new XLWorkbook())
        {
            workbook.AddWorksheet("One"); workbook.AddWorksheet("Two");
            var matrix = workbook.AddWorksheet("Matrix");
            matrix.Cell(1, 1).Value = "ESTADO";
            matrix.Cell(1, 2).Value = "CODIGO";
            matrix.Cell(1, 3).Value = "NRO";
            matrix.Cell(2, 1).Value = "EN EJECUCION";
            matrix.Cell(2, 2).Value = "PFCIAL-28";
            matrix.Cell(2, 3).Value = 28;
            using var output = new MemoryStream(); workbook.SaveAs(output); content = output.ToArray();
        }
        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        var first = await service.UploadAsync(new MemoryStream(content), "projects.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        check(first.Success, "Matrix tracked import succeeds.");
        var succeeded = await context.OperationExecutionHistories.AsNoTracking().SingleAsync(x =>
            x.OperationCode == OperationCodes.ProjectsMatrixImport &&
            x.Status == OperationExecutionStatuses.Succeeded);
        check(succeeded.FileHash == expectedHash && succeeded.FileName == "projects.xlsx" &&
              persistedCodeRequested &&
              succeeded.ResultJson!.Contains("PFCIAL-28-28", StringComparison.Ordinal) &&
              !succeeded.ResultJson.Contains("Admin Test", StringComparison.Ordinal),
            "Matrix history stores SHA-256 and project identifiers without actor payload duplication.");

        var duplicate = await service.UploadAsync(new MemoryStream(content), "renamed.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        check(duplicate.ErrorCode == ErrorCodes.AdministrativeOperations.BulkImportAlreadyProcessed &&
              duplicate.Data?.PreviousExecutionId == succeeded.ExecutionId &&
              await context.OperationExecutionHistories.CountAsync(x =>
                  x.OperationCode == OperationCodes.ProjectsMatrixImport) == 2,
            "Same successful Matrix content is rejected by hash, returns prior execution and records the attempt.");

        static async Task<ServiceResult<int>> CompleteImportAsync(object?[] args)
        {
            var callback = (Func<int, CancellationToken, Task>?)args[2];
            if (callback is not null) await callback(1, (CancellationToken)args[1]!);
            return ServiceResult<int>.Ok(1);
        }
    }

    private sealed class FakeMigration : IDataMigration
    {
        public string Code => "TEST_DATA_MIGRATION_V1";
        public string Description => "Test";
        public string? Version => "1";
        public int Order => 1;
        public int Applies { get; private set; }
        public Task<object?> ApplyAsync(CancellationToken ct = default)
        {
            Applies++;
            return Task.FromResult<object?>(new { applied = true });
        }
    }
}
