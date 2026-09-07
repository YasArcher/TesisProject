using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Constants;
using ClosedXML.Excel;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class AcademicReferenceTests
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        using (var f = new AcademicFixture())
        {
            var faculty = await UnifiedAcademicReferencePreparation.FacultyAsync(f.Uow, 501, default);
            var term = await UnifiedAcademicReferencePreparation.AcademicTermAsync(f.Uow, 20261, default);
            check(faculty.Data is { FacultyId: 17, ExternalFacultyId: 501 } && term.Data is { AcademicTermId: 8, ExternalPeriodId: 20261 }, "Resolve physically distinct external/local keys.");
            check(f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Resolvers never mutate or save.");
            check((await UnifiedAcademicReferencePreparation.FacultyAsync(f.Uow, 17, default)).ErrorCode == ErrorCodes.AcademicReferences.FacultyNotSynchronized, "Local FacultyId is not an external ID.");
            check((await UnifiedAcademicReferencePreparation.AcademicTermAsync(f.Uow, 8, default)).ErrorCode == ErrorCodes.AcademicReferences.AcademicTermNotSynchronized, "Local AcademicTermId is not an external period ID.");
        }
        foreach (var ids in new int[][] { [501, 502, 501], [], [501], [502] })
        {
            using var f = new AcademicFixture();
            var result = await f.Scopes.CreateAsync(new(" Name ", ids));
            check(result.Success && f.Saves == 1 && result.Data!.Name == "Name", "Scope Create owns one commit.");
            check(result.Data!.Faculties.Select(x => x.FacultyId).Order().SequenceEqual(ids.Distinct().Select(x => x == 501 ? 17 : 18).Order()), "Scope links store resolved local IDs, deduplicated.");
            check(result.Data.Faculties.All(x => x.ExternalFacultyId == (x.FacultyId == 17 ? 501 : 502)), "Scope response carries both local and external identity.");
            check(f.AddedCatalogs == 0 && f.ScopeKeysValid, "No automatic catalog insertion; EF propagates scope keys.");
        }
        foreach (var update in new[] { false, true })
        foreach (var ids in new int[][] { [501, 999], [501, -1], [0], [17] })
        {
            using var f = new AcademicFixture();
            var scope = f.SeedScope();
            var result = update ? await f.Scopes.SetFacultiesAsync(scope.FacultyScopeId, new(ids)) : await f.Scopes.CreateAsync(new("New", ids));
            check(!result.Success && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "All faculty references validate before applying: " + string.Join(',', ids) + $" update={update}, success={result.Success}, saves={f.Saves}: " + f.Context.ChangeTracker.DebugView.ShortView);
            check(!scope.Faculties!.Single(x => x.FacultyId == 17).IsActive && scope.Faculties.Single(x => x.FacultyId == 18).IsActive, "Failure leaves old membership state intact.");
        }
        using (var f = new AcademicFixture())
        {
            var scope = f.SeedScope();
            var result = await f.Scopes.SetFacultiesAsync(scope.FacultyScopeId, new([501, 501]));
            check(result.Success && f.Saves == 1 && f.TrackedScopeRead, "SetFaculties loads tracked aggregate and saves once.");
            check(scope.Faculties!.Single(x => x.FacultyId == 17).IsActive && !scope.Faculties.Single(x => x.FacultyId == 18).IsActive, "Existing links reactivate/deactivate without detached update loss.");
            result = await f.Scopes.SetFacultiesAsync(scope.FacultyScopeId, new([]));
            check(result.Success && f.Saves == 2 && scope.Faculties.All(x => !x.IsActive), "Empty selection deactivates all scope faculties.");
        }
        foreach (var externalFacultyId in new[] { 501, 999, 17, 0 })
        {
            using var f = new AcademicFixture();
            var result = await f.ProjectsService.CreateAsync(new() { ProjectName = "New", ProjectTypeId = 2, ProjectStateId = 3, ProjectGroupId = 4, ConvocationId = 5, ExternalFacultyId = externalFacultyId });
            if (externalFacultyId == 501)
                check(result.Success && result.Data!.PrincipalCoordinatorFacultyId == 17 && f.Projects.Single().FacultyId == 17 && f.Saves == 1, "Simple Project Create resolves faculty and saves once without Identity.");
            else check(!result.Success && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Unresolvable Project faculty fails without mutation.");
        }
        using (var f = new AcademicFixture())
        {
            var dto = new ImportedProjectDTO { Faculty = "Engineering", VisitPeriods = [new() { HasReport = true, PeriodLabel = "2026 FIRST", RawValue = " Resolution " }] };
            var result = await f.ProjectsService.PrepareImportAcademicReferencesAsync(dto, f.ExternalFaculties, f.Periods);
            check(result.Success && result.Data is { FacultyId: 17 } && result.Data.Visits.Single().AcademicTermId == 8 && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Import prepares the entire row using local academic keys without writes.");
            var project = f.SeedProject();
            var visits = result.Data!.ApplyTo(project, 41);
            await f.Uow.Visits.AddRangeAsync(visits);
            check(visits.Single().AcademicTermId == 8 && visits.Single().Document!.ResolutionCode == "Resolution" && f.Saves == 0, "Prepared Visit persists local FK and original document semantics; no inner commit.");
            await f.Uow.SaveChangesAsync();
            check(f.Saves == 1 && f.AddedCatalogs == 0, "Prepared import aggregate is compatible with one future owner save.");
            // The import entry point remains absent until Identity is separated.
            check(typeof(IUnifiedProjectService).GetMethod("ImportFromMatrixAsync") is not null && typeof(IUnifiedProjectService).GetMethod("CreateFullAsync") is not null, "Identity entry points are implemented.");
        }
        using (var f = new AcademicFixture())
        {
            f.Terms.RemoveAt(1);
            var dto = new ImportedProjectDTO { Faculty = "Engineering", VisitPeriods = [new() { HasReport = true, PeriodLabel = "2026 FIRST", RawValue = "one" }, new() { HasReport = true, PeriodLabel = "2026 SECOND", RawValue = "two" }] };
            var result = await f.ProjectsService.PrepareImportAcademicReferencesAsync(dto, f.ExternalFaculties, f.Periods);
            check(result.ErrorCode == ErrorCodes.AcademicReferences.AcademicTermNotSynchronized && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "Second unsynchronized period aborts row before any application.");
            dto.VisitPeriods = [new() { HasReport = true, PeriodLabel = "unknown", RawValue = "one" }];
            result = await f.ProjectsService.PrepareImportAcademicReferencesAsync(dto, f.ExternalFaculties, f.Periods);
            check(!result.Success, "Legacy latest-period fallback still requires local synchronization.");
        }
        using (var f = new AcademicFixture())
        {
            var request = new AddProjectFullRequestDTO { Project = new() { FacultyId = 999, StartDate = new DateTime(2026, 2, 1) }, GroupMembers = [new() { MemberRole = MemberRoleTypeIds.Coordinador, Email = "teacher@example.test" }] };
            var result = await f.ProjectsService.PrepareFullProjectFacultyAsync(request);
            check(result.Success && result.Data!.FacultyId == 17 && request.Project.FacultyId == 999 && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges(), "CreateFull preparation selects external career parent then resolves local Faculty, without mutating request.");
            var group = new UnifiedGroupService(f.Uow, f.Directory, NullLogger<UnifiedGroupService>.Instance, f.PeriodClient, f.Distributivos, AcademicFixture.NoProvisioning, Stub.For<IUnifiedIdentityQueryService>((m, a) => throw new InvalidOperationException()));
            var groupFaculty = await group.PrepareCoordinatorFacultyAsync(new() { FacultyId = 700, Email = "teacher@example.test" });
            check(groupFaculty.Success && groupFaculty.Data!.FacultyId == 17 && f.Saves == 0, "Group legacy FacultyCareerId 700 -> external faculty 501 -> local 17.");
        }
        using (var f = new AcademicFixture())
        {
            var catalogs = Stub.For<IUnifiedCatalogQueryService>((m, a) => Task.FromResult(ServiceResult<List<KeyValueItemDTO>>.Ok([])));
            var bootstrap = await new UnifiedProjectsFiltersService(catalogs, f.Uow).GetBootstrapAsync();
            check(bootstrap.Success && bootstrap.Data!.Faculties.Any(x => x.Id == 17 && x.Name == "Engineering") && bootstrap.Data.Faculties.All(x => x.Id != 501) && f.Saves == 0, "Bootstrap IDs match local Project filters.");
            f.SeedProject();
            var report = await new UnifiedProjectFlatReportService(f.Uow, f.Directory, NullLogger<UnifiedProjectFlatReportService>.Instance).GetFlatReportAsync();
            check(report.Success && report.Data!.Single().FacultyId == 17 && report.Data!.Single().FacultyName == "Engineering" && report.Data!.Single().GroupTypeName == "Team" && f.Saves == 0, "Flat report joins local Faculty and reads GroupType via Groups navigation.");
            var reportService = new UnifiedProjectFlatReportService(f.Uow, f.Directory, NullLogger<UnifiedProjectFlatReportService>.Instance);
            var exporter = new UnifiedExportTemplateExcelService(reportService, NullLogger<UnifiedExportTemplateExcelService>.Instance);
            var excel = await exporter.GenerateExcelAsync(new() { Columns = [new() { FieldKey = ExportFieldKeys.FacultyId, Header = "Faculty", OrderIndex = 1 }, new() { FieldKey = ExportFieldKeys.FacultyName, Header = "Name", OrderIndex = 2 }] });
            check(excel.Success && excel.Data is { Length: > 0 } && f.Saves == 0, "Unblocked exporter generates a workbook from the local-faculty report.");
            using (var workbook = new XLWorkbook(new MemoryStream(excel.Data!)))
                check(workbook.Worksheet(1).Cell(2, 1).GetString() == "17" && workbook.Worksheet(1).Cell(2, 2).GetString() == "Engineering", "Workbook retains local identity and correct faculty name.");
            var facultySql = new UnifiedFacultyRepository(f.Context).Query().Where(x => x.ExternalFacultyId == 501).ToQueryString();
            var termSql = new UnifiedAcademicTermRepository(f.Context).Query().Where(x => x.ExternalPeriodId == 20261).ToQueryString();
            check(facultySql.Contains("ExternalFacultyId") && termSql.Contains("ExternalPeriodId"), "Real local repository predicates translate for SQL Server.");
        }
    }
}

internal sealed class AcademicFixture : IDisposable
{
    internal UnifiedDideDbContext Context { get; } = new(new DbContextOptionsBuilder<UnifiedDideDbContext>().UseSqlServer("Server=(local);Database=AcademicReferenceTests;Trusted_Connection=True;TrustServerCertificate=True").Options);
    internal List<Faculty> Faculties { get; } = [new() { FacultyId = 17, ExternalFacultyId = 501, Name = "Engineering" }, new() { FacultyId = 18, ExternalFacultyId = 502, Name = "Science" }];
    internal List<AcademicTerm> Terms { get; } = [new() { AcademicTermId = 8, ExternalPeriodId = 20261, Name = "2026 FIRST" }, new() { AcademicTermId = 9, ExternalPeriodId = 20262, Name = "2026 SECOND" }];
    internal List<ExternalAcademicPeriodModel> Periods { get; } = [new() { PeriodId = 20261, Name = "2026 FIRST", StartDate = new(2026, 1, 1), EndDate = new(2026, 6, 30) }, new() { PeriodId = 20262, Name = "2026 SECOND", StartDate = new(2026, 7, 1), EndDate = new(2026, 12, 31) }];
    internal List<ExternalFacultyDTO> ExternalFaculties { get; } = [new() { FacultyId = 501, Name = "Engineering" }, new() { FacultyId = 502, Name = "Science" }];
    internal List<Project> Projects { get; } = [];
    internal List<FacultyScope> ScopeRows { get; } = [];
    internal IUnifiedUnitOfWork Uow { get; }
    internal IExternalDirectoryClient Directory { get; }
    internal IExternalPeriodsClient PeriodClient { get; }
    internal IExternalDistributivosService Distributivos { get; }
    internal UnifiedProjectService ProjectsService { get; }
    internal UnifiedFacultyScopeService Scopes { get; }
    internal int Saves, AddedCatalogs;
    internal bool TrackedScopeRead, ScopeKeysValid = true;
    private int generatedId = 1000;

    internal static IUnifiedIdentityProvisioningService NoProvisioning => Stub.For<IUnifiedIdentityProvisioningService>((m, a) => throw new InvalidOperationException("No provisioning expected"));
    internal AcademicFixture()
    {
        Context.AttachRange(Faculties); Context.AttachRange(Terms);
        var user = new AppUser { IdUser = 41, IdLocal = 19 };
        var type = new ProjectType { Id = 2, Name = "Applied" };
        var state = new ProjectState { Id = 3, Name = "Active" };
        var group = new Group { GroupId = 4, Name = "Group", GroupTypeId = 6, GroupType = new() { Id = 6, Name = "Team" } };
        var call = new Convocation { Id = 5, Name = "Call" };
        Context.Attach(user); Context.Attach(type); Context.Attach(state); Context.Attach(group); Context.Attach(call);
        var repositories = new Dictionary<string, object>
        {
            ["Faculties"] = Rows<IUnifiedFacultyRepository, Faculty>(Faculties),
            ["AcademicTerms"] = Rows<IUnifiedAcademicTermRepository, AcademicTerm>(Terms),
            ["FacultyScopes"] = Rows<IUnifiedFacultyScopeRepository, FacultyScope>(ScopeRows),
            ["FacultyScopeFaculties"] = Rows<IUnifiedFacultyScopeFacultyRepository, FacultyScopeFaculty>([]),
            ["Projects"] = Rows<IUnifiedProjectRepository, Project>(Projects),
            ["AppUsers"] = Rows<IUnifiedAppUserRepository, AppUser>([user]),
            ["ProjectTypes"] = Rows<IUnifiedCatalogRepository<ProjectType>, ProjectType>([type]),
            ["ProjectStates"] = Rows<IUnifiedCatalogRepository<ProjectState>, ProjectState>([state]),
            ["Groups"] = Rows<IUnifiedGroupRepository, Group>([group]),
            ["Convocations"] = Rows<IUnifiedConvocationRepository, Convocation>([call]),
            ["ResearchCategoryTypes"] = Rows<IUnifiedCatalogRepository<ResearchCategoryType>, ResearchCategoryType>([]),
            ["FundingTypes"] = Rows<IUnifiedCatalogRepository<FundingType>, FundingType>([]),
            ["ProductTypes"] = Rows<IUnifiedCatalogRepository<ProductType>, ProductType>([]),
            ["ObjectiveTypes"] = Rows<IUnifiedCatalogRepository<ObjectiveType>, ObjectiveType>([]),
            ["Institutions"] = Rows<IUnifiedCatalogRepository<Institution>, Institution>([]),
            ["ProductAttributes"] = Rows<IUnifiedCatalogRepository<ProductAttribute>, ProductAttribute>([]),
            ["ProductAttributeDefinitions"] = Rows<IUnifiedProductAttributeDefinitionRepository, ProductAttributeDefinition>([]),
            ["Budgets"] = Rows<IUnifiedBudgetRepository, Budget>([]),
            ["Products"] = Rows<IUnifiedProductRepository, Product>([]),
            ["ProductValues"] = Rows<IUnifiedProductValueRepository, ProductValue>([]),
            ["ProjectObjectives"] = Rows<IUnifiedProjectObjectiveRepository, ProjectObjective>([]),
            ["ProjectResearchCategories"] = Rows<IUnifiedProjectResearchCategoryRepository, ProjectResearchCategory>([]),
            ["ExternalResearchers"] = Rows<IUnifiedExternalResearcherRepository, ExternalResearcher>([]),
            ["ExternalResearcherProjects"] = Rows<IUnifiedExternalResearcherProjectRepository, ExternalResearcherProject>([]),
            ["GroupMembers"] = Rows<IUnifiedGroupMemberRepository, GroupMember>([]),
            ["MemberRoleTypes"] = Rows<IUnifiedCatalogRepository<MemberRoleType>, MemberRoleType>([]),
            ["Visits"] = Rows<IUnifiedVisitRepository, Visit>([])
        };
        Uow = Stub.For<IUnifiedUnitOfWork>((m, a) => m.Name == "SaveChangesAsync" ? Save() : repositories[m.Name[4..]]);
        var profile = new ExternalUserProfileModel { Email = "teacher@example.test", Careers = [new() { FacultyCareerId = 700, FacultyId = 501, IsActive = true }] };
        Directory = Stub.For<IExternalDirectoryClient>((m, a) => Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok([profile])));
        PeriodClient = Stub.For<IExternalPeriodsClient>((m, a) => Task.FromResult(ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok(Periods)));
        Distributivos = Stub.For<IExternalDistributivosService>((m, a) => Task.FromResult(ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok([new() { Email = profile.Email, PeriodId = 20261, FacultyId = 501, CareerId = 700 }])));
        var current = Stub.For<ICurrentUserService>((m, a) => 19);
        var academics = Stub.For<IExternalAcademicsService>((m, a) => throw new InvalidOperationException("Unexpected external academic fetch"));
        var categories = Stub.For<IUnifiedResearchCategoryService>((m, a) => throw new InvalidOperationException("Unexpected category call"));
        ProjectsService = new(Uow, current, academics, categories, NullLogger<UnifiedProjectService>.Instance, Directory, PeriodClient, Distributivos, NoProvisioning);
        Scopes = new(Uow, current, NoProvisioning);
    }

    internal FacultyScope SeedScope()
    {
        var scope = new FacultyScope { FacultyScopeId = 71, Name = "Old", Faculties = [new() { FacultyScopeId = 71, FacultyId = 17, IsActive = false }, new() { FacultyScopeId = 71, FacultyId = 18, IsActive = true }] };
        Context.Attach(scope); ScopeRows.Add(scope); return scope;
    }
    internal Project SeedProject()
    {
        var p = new Project { ProjectId = 81, FacultyId = 17, ProjectTypeId = 2, ProjectStateId = 3, ProjectGroupId = 4, CreatedByUserId = 41, ProjectName = "Existing", ConvocationId = 5 };
        Context.Attach(p); Projects.Add(p); return p;
    }
    private TRepo Rows<TRepo, T>(List<T> rows) where TRepo : class where T : class => Stub.For<TRepo>((m, a) =>
    {
        foreach (var token in a.OfType<CancellationToken>()) token.ThrowIfCancellationRequested();
        switch (m.Name)
        {
            case "GetByExternalFacultyIdAsync": return Task.FromResult(Faculties.SingleOrDefault(x => x.ExternalFacultyId == (int)a[0]!));
            case "GetByExternalPeriodIdAsync": return Task.FromResult(Terms.SingleOrDefault(x => x.ExternalPeriodId == (int)a[0]!));
            case "GetByLocalIdAsync": return Task.FromResult(rows.Cast<AppUser>().SingleOrDefault(x => x.IdLocal == (int)a[0]!));
            case "GetByIdUserAsync": return Task.FromResult(rows.Cast<AppUser>().SingleOrDefault(x => x.IdUser == (int)a[0]!));
            case "QueryWithRefs": if (typeof(T) == typeof(FacultyScope)) TrackedScopeRead = !(bool)a[1]!; return new AsyncRows<T>(rows);
            case "Query": return new AsyncRows<T>(rows);
            case "ExistsAsync": return Task.FromResult(rows.Any(((Expression<Func<T, bool>>)a[0]!).Compile()));
            case "CountAsync": return Task.FromResult(rows.Count);
            case "GetAllAsync": return Task.FromResult(rows.ToList());
            case "GetByIdWithRefsAsync":
            case "GetByIdAsync":
                var id = a[0] is object[] keys ? keys[0] : a[0];
                var keyName = Context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties[0].Name;
                return Task.FromResult(rows.FirstOrDefault(x => Equals(Context.Entry(x).Property(keyName).CurrentValue, id)));
            case "AddAsync": rows.Add((T)a[0]!); Context.Add(a[0]!); return Task.CompletedTask;
            case "AddRangeAsync": foreach (var row in (IEnumerable<T>)a[0]!) { rows.Add(row); Context.Add(row); } return Task.CompletedTask;
            default: throw new InvalidOperationException(typeof(T).Name + "." + m.Name);
        }
    });
    private Task<int> Save()
    {
        Saves++;
        var added = Context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
        AddedCatalogs += added.Count(e => e.Entity is Faculty or AcademicTerm);
        foreach (var link in Context.ChangeTracker.Entries<FacultyScopeFaculty>())
            ScopeKeysValid &= link.Property(x => x.FacultyScopeId).CurrentValue == Context.Entry(link.Entity.FacultyScope).Property(x => x.FacultyScopeId).CurrentValue;
        foreach (var entry in added)
        foreach (var key in entry.Properties.Where(p => p.Metadata.IsPrimaryKey() && p.IsTemporary)) { key.CurrentValue = ++generatedId; key.IsTemporary = false; }
        Context.ChangeTracker.AcceptAllChanges();
        return Task.FromResult(added.Count);
    }
    public void Dispose() => Context.Dispose();
}
