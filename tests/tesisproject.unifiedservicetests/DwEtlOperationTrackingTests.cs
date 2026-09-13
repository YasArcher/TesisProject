using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Analytic.Implementations;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

public static class DwEtlOperationTrackingTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        await SuccessWritesTypedSummaryAsync(check);
        await FailureIsPreservedAndCanBeRetriedAsync(check);
    }

    private static async Task SuccessWritesTypedSummaryAsync(Action<bool, string> check)
    {
        await using var dw = CreateProjectsDwContext();
        var history = new RecordingHistory();
        var service = CreateService(dw, new EmptyProjectsDwSource(), history);

        var result = await service.RunFullLoadAsync();

        check(result.Success && await dw.DimDates.CountAsync() == 1,
            "Tracked full load must preserve the ETL success result and data behavior.");
        var execution = history.Executions.Single();
        check(execution.OperationType == OperationExecutionTypes.Etl &&
              execution.OperationCode == OperationCodes.ProjectsDwFullLoad &&
              execution.Status == OperationExecutionStatuses.Succeeded &&
              execution.ExecutedByAppUserId == 41 && execution.ExecutedByName == "QA ETL Actor",
            "Successful ETL must record actor, ETL/PROJECTS_DW_FULL_LOAD and SUCCEEDED.");
        using var json = JsonDocument.Parse(execution.ResultJson!);
        var root = json.RootElement;
        var counts = root.GetProperty("counts");
        var requiredCounts = new[]
        {
            "dimDate", "dimProjectState", "dimFundingType", "dimProductType", "dimFaculty",
            "dimResearchCategory", "dimIndexingDatabase", "dimQuartile", "dimAuthor", "dimJournal",
            "factProject", "factBudget", "factProduct", "bridgeProjectResearchCategory", "bridgeProductAuthor"
        };
        check(root.GetProperty("durationMilliseconds").GetInt64() >= 0 &&
              requiredCounts.All(name => counts.TryGetProperty(name, out _)) &&
              counts.GetProperty("dimDate").GetInt32() == 1,
            "ResultJson must be valid, typed and contain all final DW counts.");
        check(root.GetProperty("warnings").EnumerateArray().Any(x =>
                x.GetProperty("code").GetString() == "ACADEMIC_PERIODS_UNAVAILABLE" &&
                x.GetProperty("count").GetInt32() == 1),
            "ResultJson must contain summarized warning codes without payload data.");
    }

    private static async Task FailureIsPreservedAndCanBeRetriedAsync(Action<bool, string> check)
    {
        await using var dw = CreateProjectsDwContext();
        var source = new EmptyProjectsDwSource { FailNextBoundsRead = true };
        var history = new RecordingHistory();
        var service = CreateService(dw, source, history);

        var failed = false;
        try { await service.RunFullLoadAsync(); }
        catch (InvalidOperationException) { failed = true; }

        check(failed && history.Executions.Count == 1 &&
              history.Executions[0].Status == OperationExecutionStatuses.Failed &&
              history.Executions[0].ErrorCode == "DW_ETL_FULL_LOAD_FAILED" &&
              history.LastFailureToken == CancellationToken.None,
            "Failed ETL must preserve FAILED outside the canceled/failed execution token.");

        var retry = await service.RunFullLoadAsync();
        check(retry.Success && history.Executions.Count == 2 &&
              history.Executions[0].Status == OperationExecutionStatuses.Failed &&
              history.Executions[1].Status == OperationExecutionStatuses.Succeeded,
            "A failed ETL attempt must not block a later full-load execution.");
    }

    private static ProjectsDwEtlService CreateService(ProjectsDwContext dw, IUnifiedProjectsDwSource source,
        RecordingHistory history) => new(source, dw, NullLogger<ProjectsDwEtlService>.Instance,
        new EmptyPeriodsClient(), history, new TestActor());

    private static ProjectsDwContext CreateProjectsDwContext()
        => new(new DbContextOptionsBuilder<ProjectsDwContext>()
            .UseInMemoryDatabase($"dw-etl-ledger-{Guid.NewGuid()}").Options);

    private sealed class EmptyProjectsDwSource : IUnifiedProjectsDwSource
    {
        public bool FailNextBoundsRead { get; set; }

        public Task<ProjectsDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default)
        {
            if (FailNextBoundsRead)
            {
                FailNextBoundsRead = false;
                throw new InvalidOperationException("Controlled ETL source failure.");
            }
            return Task.FromResult(new ProjectsDwDateBounds(null, null, null, null, null, null,
                null, null, null, null));
        }

        public Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProjectStatesAsync(CancellationToken ct = default) => Empty<ProjectsDwCatalogRow>();
        public Task<IReadOnlyList<ProjectsDwCatalogRow>> ListFundingTypesAsync(CancellationToken ct = default) => Empty<ProjectsDwCatalogRow>();
        public Task<IReadOnlyList<ProjectsDwCatalogRow>> ListProductTypesAsync(CancellationToken ct = default) => Empty<ProjectsDwCatalogRow>();
        public Task<IReadOnlyList<ProjectsDwFacultyRow>> ListUsedFacultiesAsync(CancellationToken ct = default) => Empty<ProjectsDwFacultyRow>();
        public Task<IReadOnlyList<ProjectsDwResearchCategoryRow>> ListResearchCategoriesAsync(CancellationToken ct = default) => Empty<ProjectsDwResearchCategoryRow>();
        public Task<IReadOnlyList<string>> ListProjectProductAttributeValuesAsync(BaseProductAttributeId attributeId, CancellationToken ct = default) => Empty<string>();
        public Task<IReadOnlyList<ProjectsDwProjectResearchCategoryRow>> ListProjectResearchCategoriesAsync(CancellationToken ct = default) => Empty<ProjectsDwProjectResearchCategoryRow>();
        public Task<IReadOnlyList<ProjectsDwProjectRow>> ListProjectsAsync(CancellationToken ct = default) => Empty<ProjectsDwProjectRow>();
        public Task<IReadOnlyList<ProjectsDwBudgetRow>> ListBudgetsAsync(CancellationToken ct = default) => Empty<ProjectsDwBudgetRow>();
        public Task<IReadOnlyList<ProjectsDwProductRow>> ListProjectProductsAsync(CancellationToken ct = default) => Empty<ProjectsDwProductRow>();
        public Task<IReadOnlyList<ProjectsDwProductAuthorRow>> ListProjectProductAuthorsAsync(CancellationToken ct = default) => Empty<ProjectsDwProductAuthorRow>();
        private static Task<IReadOnlyList<T>> Empty<T>() => Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
    }

    private sealed class EmptyPeriodsClient : IExternalPeriodsClient
    {
        public Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(Array.Empty<ExternalAcademicPeriodModel>()));
        public Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ServiceResult<ExternalAcademicPeriodModel>> GetByIdAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class TestActor : IUnifiedArticleUserContext
    {
        public string? OwnerReference => "qa-etl";
        public bool CanManageAll => true;
        public bool IsAuthenticated => true;
        public int? IdentityUserId => 19;
        public string? Email => null;
        public string? DisplayName => "QA ETL Actor";
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
        public Task<int?> GetAppUserIdAsync(CancellationToken ct = default) => Task.FromResult<int?>(41);
        public Task<int> GetRequiredAppUserIdAsync(CancellationToken ct = default) => Task.FromResult(41);
    }

    private sealed class RecordingHistory : IOperationExecutionHistoryService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        public List<OperationExecutionItem> Executions { get; } = [];
        public CancellationToken LastFailureToken { get; private set; }

        public Task<OperationExecutionItem> StartAsync(OperationExecutionStart request, CancellationToken ct = default)
        {
            var item = new OperationExecutionItem(Executions.Count + 1, Guid.NewGuid(), request.OperationType,
                request.OperationCode, request.Version, OperationExecutionStatuses.Running, DateTime.UtcNow,
                null, request.ExecutedByAppUserId, request.ExecutedByName, request.Source, request.FileName,
                request.FileHash, null, null, null);
            Executions.Add(item);
            return Task.FromResult(item);
        }

        public Task<OperationExecutionItem> CompleteSuccessAsync(Guid executionId, object? result = null, CancellationToken ct = default)
            => Complete(executionId, OperationExecutionStatuses.Succeeded, result, null, null);
        public Task<OperationExecutionItem> CompletePartialAsync(Guid executionId, object? result = null, string? errorCode = null, string? errorMessage = null, CancellationToken ct = default)
            => Complete(executionId, OperationExecutionStatuses.PartiallySucceeded, result, errorCode, errorMessage);
        public Task<OperationExecutionItem> CompleteFailureAsync(Guid executionId, string errorCode, string errorMessage, object? result = null, CancellationToken ct = default)
        {
            LastFailureToken = ct;
            return Complete(executionId, OperationExecutionStatuses.Failed, result, errorCode, errorMessage);
        }

        private Task<OperationExecutionItem> Complete(Guid id, string status, object? result, string? code, string? message)
        {
            var index = Executions.FindIndex(x => x.ExecutionId == id);
            var current = Executions[index];
            var completed = current with { Status = status, CompletedAt = DateTime.UtcNow,
                ResultJson = result is null ? null : JsonSerializer.Serialize(result, JsonOptions),
                ErrorCode = code, ErrorMessage = message };
            Executions[index] = completed;
            return Task.FromResult(completed);
        }

        public Task<OperationExecutionItem?> GetAsync(Guid executionId, CancellationToken ct = default)
            => Task.FromResult(Executions.FirstOrDefault(x => x.ExecutionId == executionId));
        public Task<OperationExecutionPage> ListAsync(OperationExecutionQuery query, CancellationToken ct = default)
            => Task.FromResult(new OperationExecutionPage(Executions, 1, 50, Executions.Count));
        public Task<OperationExecutionItem?> FindSuccessfulAsync(string operationType, string operationCode, string? fileHash = null, CancellationToken ct = default)
            => Task.FromResult(Executions.FirstOrDefault(x => x.OperationType == operationType && x.OperationCode == operationCode && x.Status == OperationExecutionStatuses.Succeeded));
    }
}

