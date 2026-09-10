using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class LocalCatalogConsumerTests
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        await using (var f = await LocalCatalogFixture.Create())
        {
            var catalog = await UnifiedAcademicCatalogReads.FacultiesAsync(f.Uow, default);
            var roots = UnifiedAcademicCatalogReads.FacultyRoots(catalog.Data!);
            check(roots.Select(x => x.FacultyId).Order().SequenceEqual([500, 600, 700]), "Local external-shaped faculty projection contains roots only, including inactive root");
            check(roots.Single(x => x.FacultyId == 500).Programs.Select(x => x.ProgramId).Order().SequenceEqual([800, 801]), "Programs are direct children Software/Civil, excluding grandchild");
            check(roots.Single(x => x.FacultyId == 500).Programs.All(x => x.FacultyId == 500), "ExternalProgramDTO contains external parent ID 500, never local 17");
            var bootstrap = await f.Sp.GetRequiredService<IUnifiedProjectsFiltersService>().GetBootstrapAsync();
            check(bootstrap.Success && bootstrap.Data!.Faculties.Select(x => x.Id).Order().SequenceEqual([17, 19]), "Faculty selector contains active roots Engineering/Health only with local IDs");
            var terms = await UnifiedAcademicCatalogReads.TermsAsync(f.Uow, default);
            var periods = UnifiedAcademicCatalogReads.Periods(terms.Data!);
            check(periods.Single().PeriodId == 20261 && terms.Data!.Single().AcademicTermId == 8, "Local period 8 maps to external 20261 for existing live selector");
            f.ResetQueries();
            var preparation = await f.Project.PrepareFullProjectFacultyAsync(f.FullRequest());
            check(preparation.Success && preparation.Data!.FacultyId == 17 && preparation.Data.ExternalFacultyId == 500, "CreateFull chooses Software using live distributivo external period and resolves local root");
            check(f.DirectoryCalls == 1 && f.DistributivoCalls == 1 && f.FacultyQueries == 1 && f.TermQueries == 1, "CreateFull uses directory/distributivos live plus two batch catalog queries");
            var member = await f.Group.GetExternalUserByEmailAsync("teacher@example.test");
            check(member.Success && member.Data!.FacultyCareerId == 800 && member.Data.FullName == "Live Teacher", "Group selection joins external 20261, not local 8; directory profile stays live");
            var researchers = new List<ResearcherInfo> { new() { email = "teacher@example.test" }, new() { email = "teacher@example.test" } };
            f.ResetQueries();
            var recognition = await f.Recognition.EnrichResearchersWithExternalDirectoryAsync(researchers, default);
            check(recognition.Success && researchers.All(x => x.FacultyCareerId == 800 && x.FullName == "Live Teacher"), "Recognition keeps live enrichment and institutional career IDs");
            check(f.FacultyQueries == 1 && f.TermQueries == 1 && f.FacultyLookups == 0, "Multiple recognized references resolve in one Faculty query, not one per researcher");
            check(f.DirectoryCalls == 3 && f.DistributivoCalls == 3, "All three migrated flows retain required live calls");
            check(f.AcademicsCalls == 0 && f.PeriodCalls == 0 && !f.Context.ChangeTracker.HasChanges(), "Migrated business flows never call academic HTTP or stage writes");

            // Move the local snapshot while the directory still advertises parent 500.
            var software = await f.Context.Set<Faculty>().SingleAsync(x => x.FacultyId == 29);
            software.ParentFacultyId = 20;
            await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            var moved = await f.Project.PrepareFullProjectFacultyAsync(f.FullRequest());
            var coordinator = await f.Group.PrepareCoordinatorFacultyAsync(new() { FacultyId = 800, Email = "teacher@example.test" });
            check(moved.Data?.FacultyId == 20 && coordinator.Data?.FacultyId == 20 && !moved.Data!.IsActive, "Project/Group reflect moved local program parent, preserving inactive root semantics");
            catalog = await UnifiedAcademicCatalogReads.FacultiesAsync(f.Uow, default);
            roots = UnifiedAcademicCatalogReads.FacultyRoots(catalog.Data!);
            check(roots.Single(x => x.FacultyId == 500).Programs.All(x => x.ProgramId != 800) && roots.Single(x => x.FacultyId == 700).Programs.Any(x => x.ProgramId == 800), "Programs reflect new local parent with no service cache or academic HTTP");

            var localFacultyIds = catalog.Data!.ToDictionary(x => x.ExternalFacultyId!.Value, x => x.FacultyId);
            var localPeriodIds = terms.Data!.ToDictionary(x => x.ExternalPeriodId!.Value, x => x.AcademicTermId);
            var dto = new ImportedProjectDTO { Faculty = "Engineering" };
            dto.VisitPeriods = [new() { HasReport = true, PeriodLabel = "2026", RawValue = "one" }, new() { HasReport = true, PeriodLabel = "2026", RawValue = "two" }];
            f.ResetQueries();
            for (var row = 0; row < 3; row++)
            {
                var imported = await f.Project.PrepareImportAcademicReferencesAsync(dto, roots, periods, default, localFacultyIds, localPeriodIds);
                check(imported.Success && imported.Data!.FacultyId == 17 && imported.Data.Visits.All(x => x.AcademicTermId == 8), "Import maps cached external IDs to local Project/Visit FKs");
            }
            check(f.FacultyQueries == 0 && f.TermQueries == 0 && f.FacultyLookups == 0, "Multiple import rows/visits reuse initial maps without additional catalog queries");
            f.ResetQueries();
            var batch = await UnifiedAcademicReferencePreparation.FacultiesAsync(f.Uow, [500, 800, 801, 500], default);
            check(batch.Success && batch.Data!.Select(x => x.FacultyId).SequenceEqual([17, 29, 30]) && f.FacultyQueries == 1 && f.FacultyLookups == 0, "Reference batch deduplicates and preserves input order in one query");
            check(f.AcademicsCalls == 0 && f.PeriodCalls == 0 && !f.Context.ChangeTracker.HasChanges(), "Reads keep save ownership unchanged");
        }
        await using (var f = await LocalCatalogFixture.Create())
        {
            f.Context.RemoveRange(await f.Context.Set<Faculty>().Where(x => x.FacultyId == 29 || x.FacultyId == 32).ToListAsync());
            await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            var project = await f.Project.PrepareFullProjectFacultyAsync(f.FullRequest());
            var coordinator = await f.Group.PrepareCoordinatorFacultyAsync(new() { FacultyId = 800, Email = "teacher@example.test" });
            var recognition = await f.Recognition.EnrichResearchersWithExternalDirectoryAsync([new() { email = "teacher@example.test" }], default);
            check(new[] { project.ErrorCode, coordinator.ErrorCode, recognition.ErrorCode }.All(x => x == ErrorCodes.AcademicReferences.FacultyNotSynchronized), "Live-selected career missing locally fails without trusting the directory parent as a substitute");
            check(f.AcademicsCalls == 0 && f.PeriodCalls == 0 && !f.Context.ChangeTracker.HasChanges(), "Missing career never triggers catalog HTTP or persistence");
            foreach (var root in await f.Context.Set<Faculty>().Where(x => x.ParentFacultyId == null).ToListAsync()) root.IsActive = false;
            await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            var filters = await f.Sp.GetRequiredService<IUnifiedProjectsFiltersService>().GetBootstrapAsync();
            check(filters.Success && filters.Data!.Faculties.Count == 0, "All roots inactive yields valid empty selector, distinguished from missing catalog");
        }
        await using (var f = await LocalCatalogFixture.Create())
        {
            var unknownFaculty = await UnifiedAcademicReferencePreparation.FacultyAsync(f.Uow, 999, default);
            var unknownTerm = await UnifiedAcademicReferencePreparation.AcademicTermAsync(f.Uow, 999, default);
            check(unknownFaculty.ErrorCode == ErrorCodes.AcademicReferences.FacultyNotSynchronized && unknownTerm.ErrorCode == ErrorCodes.AcademicReferences.AcademicTermNotSynchronized, "Unknown external references use Shared not-synchronized errors");
            check((await UnifiedAcademicReferencePreparation.FacultyAsync(f.Uow, 0, default)).Error == ErrorType.Validation, "Malformed ID is distinguished from unsynchronized positive reference");
            f.Context.RemoveRange(await f.Context.Set<AcademicTerm>().ToListAsync()); await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            var project = await f.Project.PrepareFullProjectFacultyAsync(f.FullRequest());
            var group = await f.Group.GetExternalUserByEmailAsync("teacher@example.test");
            var recognition = await f.Recognition.EnrichResearchersWithExternalDirectoryAsync([new() { email = "teacher@example.test" }], default);
            var import = await f.Project.ImportFromMatrixAsync(new());
            check(new[] { project.ErrorCode, group.ErrorCode, recognition.ErrorCode, import.ErrorCode }.All(x => x == ErrorCodes.AcademicReferences.AcademicTermNotSynchronized), "Empty local periods fail consistently through Project/Group/Recognition/import");
            check(f.DistributivoCalls == 0 && f.AcademicsCalls == 0 && f.PeriodCalls == 0, "Missing catalog never falls back to HTTP or proceeds with incomplete periods");
            f.Context.RemoveRange(await f.Context.Set<Faculty>().ToListAsync()); await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            var roots = await UnifiedAcademicCatalogReads.FacultiesAsync(f.Uow, default);
            var filters = await f.Sp.GetRequiredService<IUnifiedProjectsFiltersService>().GetBootstrapAsync();
            check(roots.ErrorCode == ErrorCodes.AcademicReferences.FacultyNotSynchronized && filters.ErrorCode == roots.ErrorCode, "Empty Faculty catalog is unavailable, not an invalid user selection");
            check(f.AcademicsCalls == 0 && f.PeriodCalls == 0 && !f.Context.ChangeTracker.HasChanges(), "No fallback or writes for missing catalog/reference");
        }
    }
}

internal sealed class LocalCatalogFixture : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly AsyncServiceScope scope;
    internal IServiceProvider Sp => scope.ServiceProvider;
    internal UnifiedDideDbContext Context => Sp.GetRequiredService<UnifiedDideDbContext>();
    internal IUnifiedUnitOfWork Uow => Sp.GetRequiredService<IUnifiedUnitOfWork>();
    internal UnifiedProjectService Project => (UnifiedProjectService)Sp.GetRequiredService<IUnifiedProjectService>();
    internal UnifiedGroupService Group => (UnifiedGroupService)Sp.GetRequiredService<IUnifiedGroupService>();
    internal UnifiedDocumentRecognitionService Recognition => (UnifiedDocumentRecognitionService)Sp.GetRequiredService<IUnifiedDocumentRecognitionService>();
    internal int AcademicsCalls, PeriodCalls, DirectoryCalls, DistributivoCalls, FacultyQueries, TermQueries, FacultyLookups;
    internal void ResetQueries() { FacultyQueries = TermQueries = FacultyLookups = 0; }
    private LocalCatalogFixture()
    {
        var services = new ServiceCollection();
        services.AddUnifiedDide(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExternalApis:BaseUrl"] = "https://example.invalid/", ["Storage:RootPath"] = Path.GetFullPath("artifacts/local-catalog-consumers/storage")
        }).Build(), o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IExternalAcademicsService>(_ => Stub.For<IExternalAcademicsService>((m, a) => { AcademicsCalls++; throw new InvalidOperationException("Academic HTTP forbidden"); }));
        services.AddScoped<IExternalPeriodsClient>(_ => Stub.For<IExternalPeriodsClient>((m, a) => { PeriodCalls++; throw new InvalidOperationException("Period HTTP forbidden"); }));
        services.AddScoped<IExternalDirectoryClient>(_ => Stub.For<IExternalDirectoryClient>((m, a) =>
        {
            DirectoryCalls++;
            return Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok([new()
            {
                Email = "teacher@example.test", FullName = "Live Teacher", Careers =
                [new() { FacultyCareerId = 801, FacultyId = 500, IsActive = true }, new() { FacultyCareerId = 800, FacultyId = 500, IsActive = true }]
            }]));
        }));
        services.AddScoped<IExternalDistributivosService>(_ => Stub.For<IExternalDistributivosService>((m, a) =>
        {
            DistributivoCalls++;
            if (m.Name != "GetDistributivosByCorreosAsync" || !((IEnumerable<string>)a[0]!).Contains("teacher@example.test")) throw new InvalidOperationException("Unexpected live request");
            return Task.FromResult(ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok([new() { Email = "teacher@example.test", FacultyId = 500, CareerId = 800, PeriodId = 20261, Hours = 40 }]));
        }));
        services.AddScoped<IUnifiedFacultyRepository>(sp =>
        {
            var real = new UnifiedFacultyRepository(sp.GetRequiredService<UnifiedDideDbContext>());
            return Stub.For<IUnifiedFacultyRepository>((m, a) => { if (m.Name == "Query") FacultyQueries++; if (m.Name == "GetByExternalFacultyIdAsync") FacultyLookups++; return m.Invoke(real, a); });
        });
        services.AddScoped<IUnifiedAcademicTermRepository>(sp =>
        {
            var real = new UnifiedAcademicTermRepository(sp.GetRequiredService<UnifiedDideDbContext>());
            return Stub.For<IUnifiedAcademicTermRepository>((m, a) => { if (m.Name == "Query") TermQueries++; return m.Invoke(real, a); });
        });
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        scope = provider.CreateAsyncScope();
    }
    internal static async Task<LocalCatalogFixture> Create()
    {
        var f = new LocalCatalogFixture();
        f.Context.AddRange(new Faculty { FacultyId = 17, ExternalFacultyId = 500, Name = "Engineering" },
            new Faculty { FacultyId = 19, ExternalFacultyId = 600, Name = "Health" },
            new Faculty { FacultyId = 20, ExternalFacultyId = 700, Name = "Science", IsActive = false },
            new Faculty { FacultyId = 29, ExternalFacultyId = 800, Name = "Software", ParentFacultyId = 17 },
            new Faculty { FacultyId = 30, ExternalFacultyId = 801, Name = "Civil", ParentFacultyId = 17 },
            new Faculty { FacultyId = 31, ExternalFacultyId = 900, Name = "Medicine", ParentFacultyId = 19 },
            new Faculty { FacultyId = 32, ExternalFacultyId = 850, Name = "Specialization", ParentFacultyId = 29 },
            new AcademicTerm { AcademicTermId = 8, ExternalPeriodId = 20261, Name = "2026", StartDate = new(2026, 1, 1), EndDate = new(2099, 12, 31) });
        await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear(); return f;
    }
    internal AddProjectFullRequestDTO FullRequest() => new() { Project = new() { StartDate = new(2026, 2, 1) }, GroupMembers = [new() { MemberRole = MemberRoleTypeIds.Coordinador, Email = "teacher@example.test" }] };
    public async ValueTask DisposeAsync() { await scope.DisposeAsync(); await provider.DisposeAsync(); }
}
