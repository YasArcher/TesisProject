using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Controllers;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Analytic.Implementations;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;

public static class ArticlesDwEtlTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        await FullLoadAndRerunAsync(check);
        await FailurePreservesExistingWarehouseAsync(check);
        ContractsAndIsolation(check);
    }

    private static async Task FullLoadAndRerunAsync(Action<bool, string> check)
    {
        await using var dw = Context();
        var source = new SourceFixture();
        var history = new HistoryFixture();
        var service = new ArticlesDwEtlService(source, dw, NullLogger<ArticlesDwEtlService>.Instance,
            history, new ActorFixture());

        var result = await service.RunFullLoadAsync();
        check(result.Success && await dw.DimArticles.CountAsync() == 5 &&
              await dw.FactArticlePublications.CountAsync() == 5 &&
              await dw.FactArticleAuthors.CountAsync() == 6,
            "Articles full-load must create five articles/publication facts and six author facts.");
        check(await dw.DimVenues.CountAsync() == 0 && await dw.FactArticleIndexings.CountAsync() == 0 &&
              await dw.FactVenueMetricYears.CountAsync() == 0,
            "Empty structural sources must remain empty and must not be inferred from ProductValues.");
        check((await dw.DimArticles.Where(x => x.ArticleId == null).CountAsync()) == 5,
            "Products without an Article extension must still load with a null ArticleId.");
        var regional = await dw.DimArticles.SingleAsync(x => x.ProductId == 4);
        var regionalFact = await dw.FactArticlePublications.SingleAsync(x => x.ProductId == 4);
        check(regional.Doi is null && regional.PublicationYear is null && regional.YearRaw is null &&
              regionalFact.Sjr is null && regionalFact.QuartileKey is null,
            "RegionalProduction must keep undefined SJR, quartile, DOI and year values null.");
        check(source.Calls.Count == 14 && source.Calls.Values.All(x => x == 1),
            "Articles ETL must execute each of the fourteen batch source queries exactly once.");

        var execution = history.Executions.Single();
        using var json = JsonDocument.Parse(execution.ResultJson!);
        var counts = json.RootElement.GetProperty("counts");
        var requiredCounts = new[] { "dimDates", "dimAuthors", "dimJournals", "dimProductTypes",
            "dimFaculties", "dimIndexingDatabases", "dimQuartiles", "dimArticles", "dimVenues",
            "dimAcademicTerms", "dimPublicationStatuses", "dimResearchLines", "dimFields",
            "dimIndexingSources", "factArticlePublications", "factArticleAuthors",
            "factArticleIndexings", "factVenueMetricYears" };
        check(execution.OperationCode == OperationCodes.ArticlesDwFullLoad &&
              execution.Status == OperationExecutionStatuses.Succeeded &&
              requiredCounts.All(x => counts.TryGetProperty(x, out _)) &&
              counts.GetProperty("dimArticles").GetInt32() == 5 &&
              counts.GetProperty("factArticleAuthors").GetInt32() == 6,
            "Ledger must record ETL/ARTICLES_DW_FULL_LOAD SUCCEEDED with typed Articles counts.");
        check(json.RootElement.GetProperty("warnings").EnumerateArray().Any(x =>
                x.GetProperty("code").GetString() == "PUBLICATION_YEAR_INVALID" && x.GetProperty("count").GetInt32() == 1),
            "Invalid raw publication years must be summarized without aborting or becoming zero.");

        source.Calls.Clear();
        var rerun = await service.RunFullLoadAsync();
        check(rerun.Success && await dw.DimArticles.CountAsync() == 5 && history.Executions.Count == 2 &&
              history.Executions.All(x => x.Status == OperationExecutionStatuses.Succeeded),
            "Articles full-load must be safely repeatable without duplicate warehouse rows.");
    }

    private static async Task FailurePreservesExistingWarehouseAsync(Action<bool, string> check)
    {
        await using var dw = Context();
        dw.DimArticles.Add(new() { ProductId = 999, ProductTypeId = 1, Title = "existing", CreatedAt = DateTime.UtcNow });
        await dw.SaveChangesAsync();
        var source = new SourceFixture { FailNextRead = true };
        var history = new HistoryFixture();
        var service = new ArticlesDwEtlService(source, dw, NullLogger<ArticlesDwEtlService>.Instance,
            history, new ActorFixture());
        var failed = false;
        try { await service.RunFullLoadAsync(); }
        catch (InvalidOperationException) { failed = true; }
        check(failed && await dw.DimArticles.AnyAsync(x => x.ProductId == 999),
            "A failed source snapshot must leave the existing Articles warehouse unchanged.");
        check(history.Executions.Single().Status == OperationExecutionStatuses.Failed &&
              history.Executions.Single().ErrorCode == "ARTICLES_DW_ETL_FULL_LOAD_FAILED" &&
              history.LastFailureToken == CancellationToken.None,
            "A failed Articles ETL must persist FAILED using an independent cancellation token.");
        var retry = await service.RunFullLoadAsync();
        check(retry.Success && await dw.DimArticles.CountAsync() == 5,
            "A later Articles ETL must run after a failed attempt.");
    }

    private static void ContractsAndIsolation(Action<bool, string> check)
    {
        var method = typeof(ArticlesDwEtlController).GetMethod(nameof(ArticlesDwEtlController.RunFullLoad))!;
        var route = typeof(ArticlesDwEtlController).GetCustomAttribute<RouteAttribute>()?.Template;
        var action = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
        check(route == "api/etl/articles" && action == "full-load",
            "Articles ETL controller must expose POST /api/etl/articles/full-load.");
        var projectsMethod = typeof(ProjectsDwEtlController).GetMethod(nameof(ProjectsDwEtlController.RunFull))!;
        check(projectsMethod.GetCustomAttributes<HttpPostAttribute>().Any(x => x.Template == "~/api/etl/projects/full-load"),
            "The explicit Projects full-load endpoint must remain unchanged.");
        var dependencies = typeof(ArticlesDwEtlService).GetConstructors().Single().GetParameters().Select(x => x.ParameterType).ToList();
        check(dependencies.Contains(typeof(ArticlesDwContext)) &&
              dependencies.Contains(typeof(IUnifiedArticlesDwSource)) &&
              !dependencies.Contains(typeof(ProjectsDwContext)) &&
              !dependencies.Contains(typeof(IUnifiedProjectsDwSource)),
            "Articles ETL must depend only on its Articles context and source boundary.");
    }

    private static ArticlesDwContext Context() => new(new DbContextOptionsBuilder<ArticlesDwContext>()
        .UseInMemoryDatabase($"articles-etl-{Guid.NewGuid()}").Options);

    private sealed class SourceFixture : IUnifiedArticlesDwSource
    {
        public Dictionary<string, int> Calls { get; } = new(StringComparer.Ordinal);
        public bool FailNextRead { get; set; }
        private void Call([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Calls[name] = Calls.GetValueOrDefault(name) + 1;
            if (FailNextRead) { FailNextRead = false; throw new InvalidOperationException("controlled source failure"); }
        }
        public Task<ArticlesDwDateBounds> GetDateBoundsAsync(CancellationToken ct = default) { Call(); return Task.FromResult(new ArticlesDwDateBounds(null, null, null, null)); }
        public Task<IReadOnlyList<ArticlesDwPublicationRow>> ListArticlePublicationsAsync(CancellationToken ct = default)
        {
            Call();
            IReadOnlyList<ArticlesDwPublicationRow> rows =
            [
                Publication(1, 1, "2024", 2024, "10.1/a", 1),
                Publication(2, 1, "2023", 2023, "10.1/b", null),
                Publication(3, 1, "invalid", null, "10.1/c", null),
                Publication(4, 2, "2022", 2022, "must-be-null", null),
                Publication(5, 2, null, null, null, null)
            ];
            return Task.FromResult(rows);
        }
        public Task<IReadOnlyList<ArticlesDwAuthorRow>> ListArticleAuthorsAsync(CancellationToken ct = default)
        {
            Call();
            IReadOnlyList<ArticlesDwAuthorRow> rows =
            [
                Author(1, 1, 10, true), Author(2, 1, 20, false), Author(3, 2, 10, true),
                Author(4, 3, 10, true), Author(5, 4, 20, false), Author(6, 5, 20, false)
            ];
            return Task.FromResult(rows);
        }
        public Task<IReadOnlyList<ArticlesDwIndexingRow>> ListArticleIndexingsAsync(CancellationToken ct = default) => Empty<ArticlesDwIndexingRow>();
        public Task<IReadOnlyList<ArticlesDwVenueRow>> ListVenuesAsync(CancellationToken ct = default) => Empty<ArticlesDwVenueRow>();
        public Task<IReadOnlyList<ArticlesDwVenueMetricRow>> ListVenueMetricsAsync(CancellationToken ct = default) => Empty<ArticlesDwVenueMetricRow>();
        public Task<IReadOnlyList<ArticlesDwAcademicTermRow>> ListAcademicTermsAsync(CancellationToken ct = default) => Empty<ArticlesDwAcademicTermRow>();
        public Task<IReadOnlyList<ArticlesDwPublicationStatusRow>> ListPublicationStatusesAsync(CancellationToken ct = default) => Empty<ArticlesDwPublicationStatusRow>();
        public Task<IReadOnlyList<ArticlesDwResearchLineRow>> ListResearchLinesAsync(CancellationToken ct = default) => Empty<ArticlesDwResearchLineRow>();
        public Task<IReadOnlyList<ArticlesDwBroadFieldRow>> ListBroadFieldsAsync(CancellationToken ct = default) => Empty<ArticlesDwBroadFieldRow>();
        public Task<IReadOnlyList<ArticlesDwSpecificFieldRow>> ListSpecificFieldsAsync(CancellationToken ct = default) => Empty<ArticlesDwSpecificFieldRow>();
        public Task<IReadOnlyList<ArticlesDwDetailedFieldRow>> ListDetailedFieldsAsync(CancellationToken ct = default) => Empty<ArticlesDwDetailedFieldRow>();
        public Task<IReadOnlyList<ArticlesDwFacultyRow>> ListFacultiesAsync(CancellationToken ct = default) => Empty<ArticlesDwFacultyRow>();
        public Task<IReadOnlyList<ArticlesDwIndexingSourceRow>> ListIndexingSourcesAsync(CancellationToken ct = default) => Empty<ArticlesDwIndexingSourceRow>();
        private Task<IReadOnlyList<T>> Empty<T>([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        { Call(name); return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>()); }
        private static ArticlesDwPublicationRow Publication(int productId, int type, string? rawYear, short? year, string? doi, int? projectId)
            => new(productId, null, type, $"Product {productId}", true, new DateTime(2024, 1, productId),
                " Journal ", " Scopus ", 1.2m, "1.2", type == 1 ? " Q1 " : "must-be-null", "ISSN", doi,
                year, rawYear, "https://example.test", projectId, projectId.HasValue, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        private static ArticlesDwAuthorRow Author(int productAuthorId, int productId, int authorId, bool institutional)
            => new(productAuthorId, productId, authorId, institutional, institutional ? 100 : null,
                institutional ? 200 : null, institutional ? null : 300, institutional ? null : "External",
                institutional ? "I-ORCID" : "E-ORCID", productAuthorId, productAuthorId == 1,
                "Autor", "Snapshot", "Affiliation");
    }

    private sealed class ActorFixture : IUnifiedArticleUserContext
    {
        public string? OwnerReference => "qa"; public bool CanManageAll => true; public bool IsAuthenticated => true;
        public int? IdentityUserId => 1; public string? Email => null; public string? DisplayName => "QA Actor";
        public IReadOnlyCollection<string> Roles => []; public IReadOnlyCollection<string> Permissions => [];
        public bool IsInRole(string role) => false; public bool HasPermission(string permission) => false;
        public Task<int?> GetAppUserIdAsync(CancellationToken ct = default) => Task.FromResult<int?>(41);
        public Task<int> GetRequiredAppUserIdAsync(CancellationToken ct = default) => Task.FromResult(41);
    }

    private sealed class HistoryFixture : IOperationExecutionHistoryService
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        public List<OperationExecutionItem> Executions { get; } = [];
        public CancellationToken LastFailureToken { get; private set; }
        public Task<OperationExecutionItem> StartAsync(OperationExecutionStart r, CancellationToken ct = default)
        {
            var item = new OperationExecutionItem(Executions.Count + 1, Guid.NewGuid(), r.OperationType, r.OperationCode,
                r.Version, OperationExecutionStatuses.Running, DateTime.UtcNow, null, r.ExecutedByAppUserId,
                r.ExecutedByName, r.Source, null, null, null, null, null);
            Executions.Add(item); return Task.FromResult(item);
        }
        public Task<OperationExecutionItem> CompleteSuccessAsync(Guid id, object? result = null, CancellationToken ct = default)
            => Complete(id, OperationExecutionStatuses.Succeeded, result, null, null);
        public Task<OperationExecutionItem> CompletePartialAsync(Guid id, object? result = null, string? errorCode = null, string? errorMessage = null, CancellationToken ct = default)
            => Complete(id, OperationExecutionStatuses.PartiallySucceeded, result, errorCode, errorMessage);
        public Task<OperationExecutionItem> CompleteFailureAsync(Guid id, string code, string message, object? result = null, CancellationToken ct = default)
        { LastFailureToken = ct; return Complete(id, OperationExecutionStatuses.Failed, result, code, message); }
        private Task<OperationExecutionItem> Complete(Guid id, string status, object? result, string? code, string? message)
        {
            var i = Executions.FindIndex(x => x.ExecutionId == id); var old = Executions[i];
            var next = old with { Status = status, CompletedAt = DateTime.UtcNow,
                ResultJson = result is null ? null : JsonSerializer.Serialize(result, Json), ErrorCode = code, ErrorMessage = message };
            Executions[i] = next; return Task.FromResult(next);
        }
        public Task<OperationExecutionItem?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Executions.FirstOrDefault(x => x.ExecutionId == id));
        public Task<OperationExecutionPage> ListAsync(OperationExecutionQuery q, CancellationToken ct = default) => Task.FromResult(new OperationExecutionPage(Executions, 1, 50, Executions.Count));
        public Task<OperationExecutionItem?> FindSuccessfulAsync(string type, string code, string? hash = null, CancellationToken ct = default)
            => Task.FromResult(Executions.FirstOrDefault(x => x.OperationType == type && x.OperationCode == code && x.Status == OperationExecutionStatuses.Succeeded));
    }
}
