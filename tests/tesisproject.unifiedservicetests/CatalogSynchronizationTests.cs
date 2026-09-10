using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified.Contracts;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class CatalogSynchronizationTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        foreach (var faculty in new[] { true, false })
        {
            await using var fixture = new SyncFixture();
            using var cancellation = new CancellationTokenSource();
            fixture.Token = cancellation.Token;
            fixture.Faculties = [new() { Id = 501, Name = "  Engineering ", Acronym = " ENG " }, new() { Id = 502, Name = "Science" }, new() { Id = 800, ParentId = 501, Name = "Program" }];
            fixture.Terms = [Period(20261, "FIRST"), Period(20262, "SECOND")];
            Task<ServiceResult<CatalogSynchronizationResult>> Sync() => faculty ? fixture.FacultySync.SynchronizeAsync(cancellation.Token) : fixture.TermSync.SynchronizeAsync(cancellation.Token);
            var first = await Sync();
            check(first.Success && first.Data!.Inserted == (faculty ? 3 : 2) && first.Data.Updated == 0 && fixture.Saves == 1, "Initial catalog: all nodes inserted, one save");
            check(first.Data!.Skipped == 0 && first.Data.TotalExternal == (faculty ? 3 : 2), "Snapshot counts include all hierarchy nodes");
            var timestamp = faculty ? fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501).LastSyncedAt : fixture.Context.Set<AcademicTerm>().Single(x => x.ExternalPeriodId == 20261).LastSyncedAt;
            var localId = faculty ? fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501).FacultyId : fixture.Context.Set<AcademicTerm>().Single(x => x.ExternalPeriodId == 20261).AcademicTermId;
            check(localId != (faculty ? 501 : 20261), "External/local identifiers physically distinct");
            var second = await Sync();
            check(second.Success && second.Data!.Inserted == 0 && second.Data.Updated == 0 && second.Data.Unchanged == (faculty ? 3 : 2) && fixture.Saves == 1, "Second identical snapshot: zero saves and functional changes");
            check(timestamp == (faculty ? fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501).LastSyncedAt : fixture.Context.Set<AcademicTerm>().Single(x => x.ExternalPeriodId == 20261).LastSyncedAt), "Unchanged row retains last content sync timestamp");

            var created = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            if (faculty)
            {
                var entity = fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501);
                entity.IsActive = false; entity.CreatedAt = created;
                fixture.Context.Add(new Faculty { FacultyId = 77, Name = "Renamed", ExternalFacultyId = null, IsActive = false });
                await fixture.Context.SaveChangesAsync();
                fixture.Faculties[0].Name = "Renamed"; fixture.Faculties[0].Acronym = "NEW";
            }
            else
            {
                fixture.Context.Add(new AcademicTerm { AcademicTermId = 77, Name = "Renamed", ExternalPeriodId = null });
                await fixture.Context.SaveChangesAsync();
                fixture.Terms[0].Name = "Renamed";
                fixture.Terms[0].EndDate = new DateTime(2026, 9, 1);
            }
            var changed = await Sync();
            check(changed.Success && changed.Data!.Inserted == 0 && changed.Data.Updated == 1 && fixture.Saves == 2, "Changed name matches only external ID and updates one row");
            if (faculty)
            {
                var entity = fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501);
                check(entity.FacultyId == localId && entity.Acronym == "NEW" && !entity.IsActive && entity.CreatedAt == created, "Faculty metadata and primary key preserved");
                check(fixture.Context.Set<Faculty>().Single(x => x.FacultyId == 77).ExternalFacultyId is null, "No name matching on local-only faculty");
                fixture.Faculties.Add(new() { Id = 503, Name = "Renamed" });
            }
            else
            {
                check(fixture.Context.Set<AcademicTerm>().Single(x => x.ExternalPeriodId == 20261).AcademicTermId == localId, "Term primary key retained");
                check(fixture.Context.Set<AcademicTerm>().Single(x => x.AcademicTermId == 77).ExternalPeriodId is null, "No name matching on local-only term");
                fixture.Terms.Add(Period(20263, "Renamed"));
            }
            var added = await Sync();
            check(added.Success && added.Data!.Inserted == 1 && added.Data.Updated == 0 && fixture.Saves == 3, "New external ID inserts despite same name");
            fixture.Faculties = []; fixture.Terms = [];
            var empty = await Sync();
            check(empty.Success && empty.Data!.TotalExternal == 0 && fixture.Saves == 3, "Legitimate empty catalog leaves all local records unchanged");
            check((faculty ? fixture.Context.Set<Faculty>().Count() : fixture.Context.Set<AcademicTerm>().Count()) == (faculty ? 5 : 4), "Absent and local-only rows not deleted");
            if (faculty) check(!fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501).IsActive, "Absent faculty activation state preserved");

            fixture.Failure = true;
            var failed = await Sync();
            check(!failed.Success && failed.ErrorCode == ErrorCodes.CatalogSynchronization.ProviderUnavailable && fixture.Saves == 3 && !fixture.Context.ChangeTracker.HasChanges(), "Provider failure: no changes/save");
            fixture.Failure = false; fixture.NullResponse = true;
            check((await Sync()).ErrorCode == ErrorCodes.CatalogSynchronization.InvalidResponse && fixture.Saves == 3, "Null successful payload rejected");
            fixture.NullResponse = false;
            fixture.Faculties = [new() { Id = 501, Name = "One" }, new() { Id = 501, Name = "Two" }];
            fixture.Terms = [Period(20261, "One"), Period(20261, "Two")];
            check((await Sync()).ErrorCode == ErrorCodes.CatalogSynchronization.DuplicateExternalId && fixture.Saves == 3, "Duplicate external ID rejects entire snapshot, not first-wins");
            fixture.Faculties = [new() { Id = 900, Name = "" }]; fixture.Terms = [Period(0, "Invalid")];
            check((await Sync()).Error == ErrorType.Validation && fixture.Saves == 3 && !fixture.Context.ChangeTracker.HasChanges(), "Invalid rows rejected before any mutation");
            fixture.Faculties = [null!]; fixture.Terms = [null!];
            check((await Sync()).Error == ErrorType.Validation && fixture.Saves == 3, "Null row rejected");
            fixture.Faculties = [new() { Id = 900, Name = new string('x', 201) }]; fixture.Terms = [Period(900, new string('x', 101))];
            check((await Sync()).Error == ErrorType.Validation && fixture.Saves == 3, "Schema length limits validated before persistence");

            fixture.Faculties = [new() { Id = 501, Name = "Failed rename" }, new() { Id = 901, Name = "New" }];
            fixture.Terms = [Period(20261, "Failed rename"), Period(901, "New")];
            fixture.FailSave = true;
            var persist = await Sync();
            check(persist.ErrorCode == ErrorCodes.CatalogSynchronization.PersistenceFailed && persist.Error == ErrorType.Conflict, "Persistence failure differentiated");
            check(!fixture.Context.ChangeTracker.HasChanges(), "Failed save restores tracked updates and detaches inserts");
            check((faculty ? fixture.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 501).Name : fixture.Context.Set<AcademicTerm>().Single(x => x.ExternalPeriodId == 20261).Name) == "Renamed", "Failed save restored original name");
            check((faculty ? fixture.Context.Set<Faculty>().AsNoTracking().Count() : fixture.Context.Set<AcademicTerm>().AsNoTracking().Count()) == (faculty ? 5 : 4), "Failed save does not persist new row");
            fixture.FailSave = false;
            fixture.Context.Add(new Faculty { Name = "Unrelated pending change" });
            var callsBefore = fixture.Calls;
            check((await Sync()).ErrorCode == ErrorCodes.CatalogSynchronization.PendingChanges && fixture.Calls == callsBefore, "Reject pending domain changes before provider call");
            fixture.Context.ChangeTracker.Clear();
            fixture.CancelOnAdd = cancellation.Cancel;
            var cancelled = false;
            try { await Sync(); } catch (OperationCanceledException) { cancelled = true; }
            check(cancelled && !fixture.Context.ChangeTracker.HasChanges(), "Cancellation after mutation propagates and cleans owned changes");
            callsBefore = fixture.Calls;
            try { await Sync(); } catch (OperationCanceledException) { }
            check(fixture.Calls == callsBefore, "Already-cancelled request never calls provider");
        }
        await using (var fixture = new SyncFixture())
        {
            fixture.Terms = [new() { PeriodId = 9, Name = "Missing dates" }];
            check((await fixture.TermSync.SynchronizeAsync()).Error == ErrorType.Validation && fixture.Saves == 0, "Missing period dates invalid");
            fixture.Terms = [new() { PeriodId = 9, Name = "Reversed", StartDate = new(2026, 3, 1), EndDate = new(2026, 1, 1) }];
            check((await fixture.TermSync.SynchronizeAsync()).Error == ErrorType.Validation && fixture.Saves == 0, "Reversed dates invalid");
            var model = fixture.Context.GetService<IDesignTimeModel>().Model;
            foreach (var (type, property) in new[] { (typeof(Faculty), "ExternalFacultyId"), (typeof(AcademicTerm), "ExternalPeriodId") })
            {
                var index = model.FindEntityType(type)!.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual([property]));
                check(index.IsUnique && index.GetFilter() == $"[{property}] IS NOT NULL", "Unique filtered external ID index " + property);
            }
            check(model.FindEntityType(typeof(AcademicTerm))!.GetCheckConstraints().Any(c => c.Name == "CK_AcademicTerms_Dates"), "Date consistency EF constraint retained");
        }
        await ClientAndControllerAsync(check);
    }

    private static ExternalAcademicPeriodModel Period(int id, string name) => new() { PeriodId = id, Name = name, StartDate = new(2026, 1, 1), EndDate = new(2026, 6, 1) };

    private static async Task ClientAndControllerAsync(Action<bool, string> check)
    {
        var handler = new SnapshotHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid/") };
        var client = new UnifiedAcademicCatalogSnapshotClient(http, Options.Create(new ExternalApiOptions()));
        foreach (var faculty in new[] { true, false })
        {
            async Task<(bool Success, string? Code, int? Count)> Read(CancellationToken ct = default)
            {
                if (faculty) { var r = await client.GetFacultiesAsync(ct); return (r.Success, r.ErrorCode, r.Data?.Count); }
                else { var r = await client.GetAcademicTermsAsync(ct); return (r.Success, r.ErrorCode, r.Data?.Count); }
            }
            handler.Status = HttpStatusCode.OK; handler.Json = "[]";
            check((await Read()).Success, "HTTP 200 [] is a legitimate empty snapshot");
            check(handler.LastPath == (faculty ? "/api/facultades" : "/api/periodos"), "Configured remote endpoint");
            handler.Json = "null";
            check((await Read()).Code == ErrorCodes.CatalogSynchronization.InvalidResponse, "JSON null differs from empty catalog");
            handler.Json = "{bad json";
            check((await Read()).Code == ErrorCodes.CatalogSynchronization.InvalidResponse, "Malformed JSON rejected");
            handler.Json = "{\"items\":[],\"next\":\"page2\"}";
            check((await Read()).Code == ErrorCodes.CatalogSynchronization.InvalidResponse, "Unrecognized pagination envelope rejected, never treated as complete empty catalog");
            handler.Json = faculty ? "[{\"id_facultad_carrera\":501,\"nombre\":\"A\"},{\"id_facultad_carrera\":501,\"nombre\":\"B\"}]" : "[{\"id_periodo\":20261,\"nombre\":\"A\"},{\"id_periodo\":20261,\"nombre\":\"B\"}]";
            check((await Read()).Count == 2, "Client preserves duplicate rows for strict synchronization validation");
            foreach (var status in new[] { HttpStatusCode.NotFound, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError })
            {
                handler.Status = status;
                check((await Read()).Code == ErrorCodes.CatalogSynchronization.ProviderUnavailable, "HTTP failure never becomes an empty successful snapshot");
            }
            handler.Status = HttpStatusCode.OK;
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var propagated = false;
            try { await Read(cancelled.Token); } catch (OperationCanceledException) { propagated = true; }
            check(propagated, "Transport cancellation propagated");
        }
        using var cts = new CancellationTokenSource();
        var expectedService = typeof(IUnifiedFacultySynchronizationService);
        var result = ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.InvalidResponse, ErrorType.Validation, ErrorCodes.CatalogSynchronization.InvalidResponse, new() { ["ExternalId"] = ["invalid"] });
        object? Handle(MethodInfo method, object?[] args)
        {
            check(method.DeclaringType == expectedService && method.Name == "SynchronizeAsync" && (CancellationToken)args[0]! == cts.Token, "Sync controller calls correct service with CT");
            return Task.FromResult(result);
        }
        var controller = new UnifiedCatalogSynchronizationController(Stub.For<IUnifiedFacultySynchronizationService>(Handle), Stub.For<IUnifiedAcademicTermSynchronizationService>(Handle));
        var facultyResponse = await controller.SynchronizeFaculties(cts.Token);
        expectedService = typeof(IUnifiedAcademicTermSynchronizationService);
        var termResponse = await controller.SynchronizeAcademicTerms(cts.Token);
        foreach (var response in new[] { facultyResponse, termResponse })
            check(ReferenceEquals(((BadRequestObjectResult)response.Result!).Value, result), "Sync HTTP preserves complete ServiceResult");
        result = ServiceResult<CatalogSynchronizationResult>.Ok(new(2, 2, 0, 0, 0, 0, DateTime.UtcNow));
        check(ReferenceEquals(((OkObjectResult)(await controller.SynchronizeAcademicTerms(cts.Token)).Result!).Value, result), "Sync success returns 200 and counts unchanged");
        result = ServiceResult<CatalogSynchronizationResult>.Fail(ErrorMessages.CatalogSynchronization.PersistenceFailed, ErrorType.Conflict, ErrorCodes.CatalogSynchronization.PersistenceFailed);
        check(ReferenceEquals(((ConflictObjectResult)(await controller.SynchronizeAcademicTerms(cts.Token)).Result!).Value, result), "Sync persistence conflict returns 409 and code unchanged");
        var type = typeof(UnifiedCatalogSynchronizationController);
        check(type.GetCustomAttribute<AuthorizeAttribute>()!.Roles == "superadmin" && !type.IsDefined(typeof(NonControllerAttribute)), "Administrative role and active discovery");
        check(type.GetCustomAttribute<RouteAttribute>()!.Template == "api/catalog-sync" && type.GetMethods().Count(m => m.IsDefined(typeof(HttpPostAttribute))) == 2, "Only two explicit manual POST synchronization endpoints");
        var manager = new ApplicationPartManager(); manager.ApplicationParts.Add(new AssemblyPart(type.Assembly)); manager.FeatureProviders.Add(new ControllerFeatureProvider());
        var feature = new ControllerFeature(); manager.PopulateFeature(feature);
        check(feature.Controllers.Contains(type.GetTypeInfo()), "Synchronization controller active in runtime discovery");
    }
}

internal sealed class SyncFixture : IAsyncDisposable, IUnifiedAcademicCatalogSnapshotClient
{
    public UnifiedDideDbContext Context { get; } = new(new DbContextOptionsBuilder<UnifiedDideDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    public List<ExternalFacultyCareerFlatModel> Faculties { get; set; } = [];
    public List<ExternalAcademicPeriodModel> Terms { get; set; } = [];
    public CancellationToken Token { get; set; }
    public bool Failure { get; set; }
    public bool NullResponse { get; set; }
    public bool FailSave { get; set; }
    public Action? CancelOnAdd { get; set; }
    public int Calls { get; private set; }
    public int Saves { get; private set; }
    public UnifiedFacultySynchronizationService FacultySync { get; }
    public UnifiedAcademicTermSynchronizationService TermSync { get; }
    public SyncFixture()
    {
        var facultyRepo = new UnifiedFacultyRepository(Context);
        var termRepo = new UnifiedAcademicTermRepository(Context);
        // Real repositories; cancellation injected immediately after adding is observed by SaveChanges token.
        var uow = Stub.For<IUnifiedUnitOfWork>((method, args) =>
        {
            if (method.Name == "get_Faculties") return facultyRepo;
            if (method.Name == "get_AcademicTerms") return termRepo;
            if (method.Name == "SaveChangesAsync")
            {
                Saves++;
                if (FailSave) throw new DbUpdateException("Injected test persistence failure");
                CancelOnAdd?.Invoke();
                return Context.SaveChangesAsync((CancellationToken)args[0]!);
            }
            throw new InvalidOperationException(method.Name);
        });
        FacultySync = new(uow, this, Context); TermSync = new(uow, this, Context);
    }
    public Task<ServiceResult<List<ExternalFacultyCareerFlatModel>>> GetFacultiesAsync(CancellationToken ct = default) => Snapshot(Faculties, ct);
    public Task<ServiceResult<List<ExternalAcademicPeriodModel>>> GetAcademicTermsAsync(CancellationToken ct = default) => Snapshot(Terms, ct);
    private Task<ServiceResult<List<T>>> Snapshot<T>(List<T> rows, CancellationToken ct)
    {
        if (ct != Token) throw new InvalidOperationException("Snapshot cancellation token missing");
        Calls++;
        return Task.FromResult(Failure ? ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.ProviderUnavailable, ErrorType.Unexpected, ErrorCodes.CatalogSynchronization.ProviderUnavailable) : ServiceResult<List<T>>.Ok(NullResponse ? null! : rows));
    }
    public ValueTask DisposeAsync() => Context.DisposeAsync();
}

internal sealed class SnapshotHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string Json { get; set; } = "[]";
    public string? LastPath { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); LastPath = request.RequestUri!.AbsolutePath;
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Json, Encoding.UTF8, "application/json") });
    }
}
