using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Implementations;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class IdentityProvisioningTests
{
    internal static RegisterRequest Request(int asp = 500, string email = "person@example.test", string? role = "user") =>
        new() { AspUserId = asp, Email = email, Username = "doc" + asp, Password = "samplepass", Role = role };

    internal static async Task RunAsync(Action<bool, string> check)
    {
        using (var f = await IdentityFixture.Create())
        {
            var r = await f.Identity.EnsureAsync(Request());
            var bridge = await f.Context.AppUsers.SingleAsync();
            var user = await f.Users.FindByEmailAsync("person@example.test");
            check(r.Success && r.Data == bridge.IdUser && bridge.IdAsp == 500 && bridge.IdLocal == user!.Id, "New institutional user: real EF Identity store, password and bridge.");
            check(await f.Users.CheckPasswordAsync(user!, "samplepass") && await f.Users.IsInRoleAsync(user!, "user"), "New account has password and requested role.");
            var saves = f.Probe.Batches.Count;
            var again = await f.Identity.EnsureAsync(Request());
            check(again.Success && again.Data == r.Data && f.Probe.Batches.Count == saves, "Idempotent existing identity performs no writes.");
            var added = await f.Identity.EnsureAsync(Request(role: "coordinador"));
            check(added.Success && (await f.Users.GetRolesAsync(user!)).Count == 2, "Missing role added without removing prior role.");
            var noRole = await f.Identity.EnsureAsync(Request(role: "technical"));
            check(!noRole.Success && noRole.ErrorCode == ErrorCodes.IdentityProvisioning.InvalidRole, "Allowed but unseeded role fails explicitly.");
            check(!(await f.Identity.EnsureAsync(Request(role: "admin"))).Success, "Role outside allowed set fails.");
            var query = f.Provider.GetRequiredService<IUnifiedIdentityQueryService>();
            check((await query.GetEmailByUserIdAsync(user!.Id))?.Email == user.Email, "Identity query uses same Unified store.");
            check((await query.GetUsernamesByUserIdsAsync([user.Id]))[user.Id] == "doc500", "Identity query resolves usernames by local ID.");
            var defaultRole = await f.Identity.EnsureAsync(Request(501, "default@example.test", null));
            check(defaultRole.Success && await f.Users.IsInRoleAsync((await f.Users.FindByEmailAsync("default@example.test"))!, "user"), "Unspecified role defaults to user.");
        }
        using (var f = await IdentityFixture.Create())
        {
            var u = await f.SeedIdentity(17, "person@example.test");
            var result = await f.Identity.EnsureAsync(Request());
            check(result.Success && (await f.Context.AppUsers.SingleAsync()).IdLocal == 17 && await f.Context.Users.CountAsync() == 1, "Identity without AppUser gets bridge, no second account.");
            f.Context.ChangeTracker.Clear();
            var b = await f.Context.AppUsers.SingleAsync(); b.IdAsp = null; await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear();
            result = await f.Identity.EnsureAsync(Request());
            check(result.Success && (await f.Context.AppUsers.SingleAsync()).IdAsp == 500, "Existing local bridge acquires missing IdAsp.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedIdentity(17, "old@example.test"); await f.SeedIdentity(32, "person@example.test");
            f.Context.AppUsers.Add(new() { IdUser = 71, IdLocal = 17, IdAsp = 500 }); await f.Context.SaveChangesAsync(); f.Context.ChangeTracker.Clear(); f.Probe.Batches.Clear();
            var conflict = await f.Identity.EnsureAsync(Request());
            check(!conflict.Success && conflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict && !f.Context.ChangeTracker.HasChanges() && f.Probe.Batches.Count == 0, "Canonical IdAsp conflict fails before any write.");
            check((await f.Context.AppUsers.AsNoTracking().SingleAsync()).IdLocal == 17, "Conflict never relinks 17 to 32.");
            conflict = await f.Identity.EnsureAsync(Request(501, "old@example.test"));
            check(!conflict.Success && conflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict, "Email/local bridge already tied to another IdAsp conflicts.");
            check((await f.Identity.EnsureAsync(Request(500, "changed@example.test"))).Success && await f.Context.Users.CountAsync() == 2, "Known canonical identity reused when new email does not identify another account.");
        }
        using (var f = await IdentityFixture.Create())
        {
            var request = Request(); request.AspUserId = null;
            check(!(await f.Identity.EnsureAsync(request)).Success && await f.Context.Users.CountAsync() == 0, "Missing institutional ID rejected before account creation.");
            f.Context.Projects.Add(new() { ProjectName = "Pending caller aggregate" });
            var result = await f.Identity.EnsureAsync(Request());
            check(!result.Success && result.ErrorCode == ErrorCodes.IdentityProvisioning.PendingChanges && f.Probe.Batches.Count == 0 && await f.Context.Users.CountAsync() == 0, "Pending Project blocks provisioning before UserManager can flush it.");
        }
        using (var f = await IdentityFixture.Create())
        {
            var requests = new[] { Request(1, "one@example.test"), Request(2, "two@example.test"), Request(3, "three@example.test") };
            requests[2].Password = "x";
            var r = await f.Identity.EnsureSelectedAsync(requests);
            check(!r.Success && await f.Context.AppUsers.CountAsync() == 2 && await f.Context.Users.CountAsync() == 2, "Completed users remain when third provisioning fails.");
        }
        using (var f = await IdentityFixture.Create())
        {
            f.Probe.FailBridge = true;
            var incomplete = await f.Identity.EnsureAsync(Request());
            check(!incomplete.Success && await f.Context.Users.CountAsync() == 0 && await f.Context.AppUsers.CountAsync() == 0 && await f.Context.UserRoles.CountAsync() == 0,
                "Failure inside new provisioning cleans up its incomplete account and roles, not a Project rollback.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedDomain(); f.Probe.FailDomain = true;
            var result = await f.ProjectService().CreateFullAsync(f.FullRequest());
            check(!result.Success && await f.Context.Users.CountAsync() == 2 && await f.Context.AppUsers.CountAsync() == 2, "Project save failure preserves provisioned account and business bridge.");
            check(await f.Context.Projects.AsNoTracking().CountAsync() == 0, "Failed domain save has not persisted Project.");
            check(f.Probe.Batches.Where(b => b.Contains(nameof(IdentityUser<int>)) || b.Contains(nameof(AppUser))).All(b => !b.Contains(nameof(Project)) && !b.Contains(nameof(Group))), "Every provisioning save excluded pending Project/Group entities.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedDomain(); var req = f.FullRequest(); req.Project.ProjectName = "";
            var r = await f.ProjectService().CreateFullAsync(req);
            check(!r.Success && await f.Context.Users.CountAsync() == 1 && f.Probe.Batches.Count == 0, "Project validation fails before participant provisioning.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedDomain();
            var full = f.FullRequest(); full.ExternalResearcherIds = [12];
            var r = await f.ProjectService().CreateFullAsync(full);
            check(r.Success && await f.Context.Projects.CountAsync() == 1 && await f.Context.GroupMembers.CountAsync() == 1, "CreateFull persists complete aggregate and selected member.");
            check(await f.Context.Users.CountAsync() == 2 && await f.Context.ExternalResearcherProjects.CountAsync() == 1, "ExternalResearcher linked without an Identity or AppUser provision.");
            check(f.Probe.Batches.Count(b => b.Contains(nameof(Project))) == 1, "CreateFull domain has one save.");
            var member = await f.Context.GroupMembers.AsNoTracking().SingleAsync(); f.Probe.Batches.Clear();
            var group = new UnifiedGroupService(f.Uow, f.Directory, NullLogger<UnifiedGroupService>.Instance, f.Distributivos, f.Identity, f.Provider.GetRequiredService<IUnifiedIdentityQueryService>());
            var dup = await group.AddMemberAsync(new() { GroupId = member.GroupId, MemberRole = MemberRoleTypeIds.Coordinador, Email = "teacher@example.test", Document = "teacher", AspUserId = 501, FacultyId = 700 });
            check(!dup.Success && f.Probe.Batches.Count == 0 && (await f.Context.Users.CountAsync()) == 2, "Duplicate GroupMember fails without provisioning or role writes.");
            var invalid = await group.AddMemberAsync(new() { GroupId = member.GroupId, MemberRole = MemberRoleTypeIds.Coordinador, Email = "new@example.test", Document = "new", AspUserId = 600, FacultyId = 999 });
            check(!invalid.Success && f.Probe.Batches.Count == 0 && await f.Context.Users.CountAsync() == 2, "Invalid coordinator faculty fails before provisioning.");
            var added = await group.AddMemberAsync(new() { GroupId = member.GroupId, MemberRole = MemberRoleTypeIds.Subrogante, Email = "alternate@example.test", Document = "alternate", AspUserId = 601 });
            check(added.Success && await f.Context.GroupMembers.CountAsync() == 2 && f.Probe.Batches.Count(b => b.Contains(nameof(GroupMember))) == 1, "Group provisions selected alternate then saves membership once.");
            var local = (await f.Users.FindByEmailAsync("teacher@example.test"))!.Id;
            check((await group.GetExternalUserByAspNetIdAsync(local)).Success, "Group single Identity read is functional.");
            check((await group.GetExternalUsersByGroupAsync(member.GroupId)).Success, "Group batch Identity read is functional.");
            check((await group.GetProjectMembersReportAsync(r.Data!.ProjectId)).Success, "Members report uses Identity query boundary.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedDomain();
            var scope = new UnifiedFacultyScopeService(f.Uow, f.Current, f.Identity);
            var result = await scope.AssignScopeToUserAsync(4, new() { Email = "scope@example.test", Document = "scope", AspUserId = 600 });
            check(result.Success && (await f.Context.UserFacultyScopeAssignments.SingleAsync()).IdentityUserId != 17, "Scope assignment stores business IdUser after provisioning.");
            check(f.Probe.Batches.Count(b => b.Contains(nameof(UserFacultyScopeAssignment))) == 1, "Scope owner saves once after provisioning.");
        }
        using (var f = await IdentityFixture.Create())
        {
            await f.SeedDomain();
            var profiles = Enumerable.Range(1, 3).Select(i => new ExternalUserProfileModel { AspId = 700 + i, Email = $"import{i}@example.test", Document = $"import{i}", FullName = $"Person Number {i}" }).ToList();
            var row = new ImportedProjectDTO { ProjectCode = "P", Number = 1, ProjectName = "Import", Faculty = "Engineering", State = "EN EJECUCION", Coordinators = profiles.Select(p => p.FullName).ToList(), VisitPeriods = [new() { HasReport = true, RawValue = "R-1", PeriodLabel = "2026 FIRST" }] };
            var chosen = UnifiedProjectService.SelectImportedMembers(row, profiles, default);
            check(chosen.Count == 2 && chosen[0].Historical && !chosen[1].Historical && chosen.All(x => x.Request.AspUserId != 703), "Import caps selection before any provisioning and preserves historical designation.");
            var result = await f.ProjectService(profiles).ImportFromMatrixAsync(new ProjectMatrixUploadSummaryDTO { ImportedProjects = [row, new() { ProjectCode = "Q", Number = 2, ProjectName = "Second", Faculty = "Engineering", State = "EN EJECUCION", Coordinators = [profiles[1].FullName] }] });
            check(result.Success && result.Data == 2 && await f.Context.Users.CountAsync() == 3, "Two-row import provisions only retained institutional participants.");
            check(await f.Context.Visits.AnyAsync(v => v.AcademicTermId == 8) && f.Probe.Batches.Count(b => b.Contains(nameof(Project))) == 1, "Import preserves local term ID and one domain save for entire batch.");
            check(f.Probe.Batches.Where(b => b.Contains(nameof(AppUser))).All(b => !b.Contains(nameof(Project)) && !b.Contains(nameof(Group))), "No Identity provisioning flushes prior imported rows.");
        }
        {
            // Upload must relay identity conflicts instead of reporting successful processing.
            var project = Stub.For<IUnifiedProjectService>((m, a) => Task.FromResult(ServiceResult<int>.Fail(
                ErrorMessages.IdentityProvisioning.MappingConflict, ErrorType.Conflict, ErrorCodes.IdentityProvisioning.MappingConflict)));
            var matrix = new UnifiedProjectMatrixService(project, NullLogger<UnifiedProjectMatrixService>.Instance);
            using var book = new ClosedXML.Excel.XLWorkbook(); book.AddWorksheet("One"); book.AddWorksheet("Two");
            var sheet = book.AddWorksheet("Matrix"); sheet.Cell(1, 1).Value = "ESTADO"; sheet.Cell(2, 1).Value = "EN EJECUCION";
            using var stream = new MemoryStream(); book.SaveAs(stream); stream.Position = 0;
            var result = await matrix.UploadAsync(stream, "matrix.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            check(!result.Success && result.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict && result.Error == ErrorType.Conflict,
                "Matrix Upload relays Identity conflict metadata without false Success.");
        }
    }
}

internal sealed class IdentitySaveProbe : SaveChangesInterceptor
{
    internal List<HashSet<string>> Batches { get; } = [];
    internal bool FailDomain;
    internal bool FailBridge;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var names = eventData.Context!.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(e => e.Entity.GetType().Name.Split('`')[0]).ToHashSet();
        Batches.Add(names);
        if (FailBridge && names.Contains(nameof(AppUser))) throw new DbUpdateException("Injected bridge failure");
        if (FailDomain && names.Contains(nameof(Project))) throw new DbUpdateException("Injected domain failure");
        return ValueTask.FromResult(result);
    }
}

internal sealed class IdentityFixture : IDisposable
{
    private readonly ServiceProvider root;
    private readonly IServiceScope scope;
    internal IServiceProvider Provider => scope.ServiceProvider;
    internal UnifiedDideDbContext Context => Provider.GetRequiredService<UnifiedDideDbContext>();
    internal UserManager<IdentityUser<int>> Users => Provider.GetRequiredService<UserManager<IdentityUser<int>>>();
    internal IUnifiedIdentityProvisioningService Identity => Provider.GetRequiredService<IUnifiedIdentityProvisioningService>();
    internal IUnifiedUnitOfWork Uow => Provider.GetRequiredService<IUnifiedUnitOfWork>();
    internal IdentitySaveProbe Probe { get; } = new();
    internal ICurrentUserService Current => Stub.For<ICurrentUserService>((m, a) => 17);
    internal IExternalDirectoryClient Directory => DirectoryFor([new() { AspId = 501, Email = "teacher@example.test", Document = "teacher", FullName = "Teacher Person", Careers = [new() { FacultyCareerId = 700, FacultyId = 501, IsActive = true }] }]);
    private static IExternalDirectoryClient DirectoryFor(IReadOnlyList<ExternalUserProfileModel> profiles) => Stub.For<IExternalDirectoryClient>((m, a) => Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok(profiles)));
    internal IExternalPeriodsClient Periods => Stub.For<IExternalPeriodsClient>((m, a) => Task.FromResult(ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>.Ok([new() { PeriodId = 20261, Name = "2026 FIRST", StartDate = new(2026, 1, 1), EndDate = new(2026, 6, 30) }])));
    internal IExternalDistributivosService Distributivos => Stub.For<IExternalDistributivosService>((m, a) => Task.FromResult(ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok([new() { Email = "teacher@example.test", PeriodId = 20261, FacultyId = 501, CareerId = 700 }])));

    private IdentityFixture()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddAuthentication();
        services.AddDbContext<UnifiedDideDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(Probe));
        services.AddScoped<IUnifiedUnitOfWork>(sp =>
        {
            var ctx = sp.GetRequiredService<UnifiedDideDbContext>();
            var repos = new Dictionary<string, object>();
            foreach (var prop in typeof(IUnifiedUnitOfWork).GetProperties())
            {
                // Test composition only: all repositories are the concrete Unified implementations.
                var type = prop.PropertyType.IsGenericType ? (prop.PropertyType.GetGenericTypeDefinition() == typeof(IUnifiedCatalogRepository<>) ? typeof(UnifiedCatalogRepository<>) : typeof(UnifiedGenericRepository<>)).MakeGenericType(prop.PropertyType.GetGenericArguments())
                    : typeof(UnifiedFacultyRepository).Assembly.GetTypes().Single(t => t.Namespace == typeof(UnifiedFacultyRepository).Namespace && !t.IsAbstract && !t.IsInterface && prop.PropertyType.IsAssignableFrom(t));
                repos[prop.Name] = Activator.CreateInstance(type, ctx)!;
            }
            var parameters = typeof(UnifiedUnitOfWork).GetConstructors().Single().GetParameters()
                .Select(p => p.ParameterType == typeof(UnifiedDideDbContext) ? (object)ctx : repos.Values.Single(r => p.ParameterType.IsInstanceOfType(r))).ToArray();
            return (IUnifiedUnitOfWork)Activator.CreateInstance(typeof(UnifiedUnitOfWork), parameters)!;
        });
        services.AddUnifiedIdentityBoundary(); root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }); scope = root.CreateScope();
    }
    internal static async Task<IdentityFixture> Create()
    {
        var f = new IdentityFixture(); var manager = f.Provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        await manager.CreateAsync(new("user")); await manager.CreateAsync(new("coordinador")); f.Probe.Batches.Clear(); return f;
    }
    internal async Task<IdentityUser<int>> SeedIdentity(int id, string email)
    {
        var user = new IdentityUser<int> { Id = id, Email = email, UserName = email, NormalizedEmail = email.ToUpperInvariant(), NormalizedUserName = email.ToUpperInvariant() };
        var result = await Users.CreateAsync(user, "samplepass");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(",", result.Errors.Select(e => e.Code)));
        Context.ChangeTracker.Clear(); return user;
    }
    internal async Task SeedDomain()
    {
        await SeedIdentity(17, "actor@example.test");
        Context.AddRange(new AppUser { IdUser = 41, IdLocal = 17, IdAsp = 99 }, new Faculty { FacultyId = 7, ExternalFacultyId = 501, Name = "Engineering", Acronym = "ENG" }, new Faculty { FacultyId = 29, ExternalFacultyId = 700, ParentFacultyId = 7, Name = "Software" }, new AcademicTerm { AcademicTermId = 8, ExternalPeriodId = 20261, Name = "2026 FIRST", StartDate = new(2026, 1, 1), EndDate = new(2026, 6, 30) },
            new ProjectOriginType { Id = ProjectOriginTypeIds.Interno, Name = "Internal" }, new DocumentType { Id = DocumentTypeIds.ResolucionVisita, Name = "Visit resolution" },
            new Convocation { Id = 1, Name = "Call", Code = "CALL" }, new ProjectType { Id = ProjectTypeIds.Aplicada, Name = "Applied" }, new ProjectState { Id = ProjectStateIds.EnEjecucion, Name = "EN EJECUCION" },
            new GroupType { Id = GroupTypeIds.Integrantes, Name = "Members" }, new MemberRoleType { Id = MemberRoleTypeIds.Coordinador, Name = "Coordinator" }, new MemberRoleType { Id = MemberRoleTypeIds.Subrogante, Name = "Alternate" },
            new FacultyScope { FacultyScopeId = 4, Name = "Scope" }, new ExternalResearcher { ExternalResearcherId = 12, FullName = "External Person", Email = "external@example.test" });
        await Context.SaveChangesAsync(); Context.ChangeTracker.Clear(); Probe.Batches.Clear();
    }
    internal AddProjectFullRequestDTO FullRequest() => new() { Project = new() { ProjectName = "Complete", ProjectTypeId = ProjectTypeIds.Aplicada, ProjectOriginTypeId = ProjectOriginTypeIds.Interno, ProjectStateId = ProjectStateIds.EnEjecucion, ProjectCode = "ENG", ConvocationId = 1, StartDate = new(2026, 2, 1) },
        GroupMembers = [new() { MemberRole = MemberRoleTypeIds.Coordinador, Email = "teacher@example.test", Document = "teacher", AspUserId = 501 }] };
    internal UnifiedProjectService ProjectService(IReadOnlyList<ExternalUserProfileModel>? profiles = null) => new(Uow, Current,
        Stub.For<IUnifiedResearchCategoryService>((m, a) => Task.FromResult(ServiceResult<IReadOnlyList<ResearchCategoryListItemDTO>>.Ok([]))),
        NullLogger<UnifiedProjectService>.Instance, profiles is null ? Directory : DirectoryFor(profiles), Distributivos, Identity);
    public void Dispose() { ((IAsyncDisposable)scope).DisposeAsync().AsTask().GetAwaiter().GetResult(); root.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
}
