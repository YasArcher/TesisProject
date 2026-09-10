using System.Data;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.DTOs.Products.Product.Request;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

const string Server = @".\DINNOVA";
const string IntegrationDatabase = "tesis_unified_integration_20260907_001";
const string UpgradeDatabase = "tesis_unified_upgrade_20260907_001";
const string TestPassword = "SqlFixture9!";
var connectionString = $"Server={Server};Database={IntegrationDatabase};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
var upgradeConnectionString = $"Server={Server};Database={UpgradeDatabase};Integrated Security=True;TrustServerCertificate=True";
var assertions = 0;

if (!IntegrationDatabase.StartsWith("tesis_unified_integration_", StringComparison.Ordinal))
    throw new InvalidOperationException("Refusing to rebuild a database that is not the dedicated integration fixture.");
await using (var rebuild = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options))
{
    await rebuild.Database.EnsureDeletedAsync();
    await rebuild.Database.MigrateAsync();
}

void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    assertions++;
}

async Task<T> ScalarAsync<T>(string cs, string sql)
{
    await using var connection = new SqlConnection(cs);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    var value = await command.ExecuteScalarAsync();
    return (T)Convert.ChangeType(value!, typeof(T));
}

async Task ExecuteAsync(string cs, string sql)
{
    await using var connection = new SqlConnection(cs);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}

async Task ExpectSqlFailureAsync(string sql, params int[] expectedNumbers)
{
    try
    {
        await ExecuteAsync(connectionString, "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; " + sql);
        throw new InvalidOperationException("Expected SQL Server to reject the statement.");
    }
    catch (SqlException ex) when (expectedNumbers.Length == 0 || expectedNumbers.Contains(ex.Number))
    {
        assertions++;
    }
}

Check(await ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM sys.databases WHERE name = DB_NAME()") == 1,
    "Integration database must exist.");
Check(await ScalarAsync<int>(upgradeConnectionString, "SELECT COUNT(*) FROM sys.databases WHERE name = DB_NAME()") == 1,
    "Upgrade database must exist.");

var migrations = await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM dbo.__EFMigrationsHistoryUnifiedDide WHERE MigrationId IN ('20260906042715_InitialUnifiedDide','20260908025200_AddFacultyHierarchy')");
Check(migrations == 2, "All Unified migrations must be applied in the integration database.");
Check(await ScalarAsync<int>(upgradeConnectionString,
    "SELECT COUNT(*) FROM dbo.__EFMigrationsHistoryUnifiedDide WHERE MigrationId = '20260908025200_AddFacultyHierarchy'") == 1,
    "Faculty hierarchy upgrade migration must be applied.");
Check(await ScalarAsync<int>(upgradeConnectionString,
    "SELECT COUNT(*) FROM dbo.Faculties WHERE (FacultyId=17 AND ExternalFacultyId=500 AND ParentFacultyId IS NULL AND Name=N'Ingeniería') OR (FacultyId=29 AND ExternalFacultyId=800 AND ParentFacultyId IS NULL AND Name=N'Software')") == 2,
    "Faculty upgrade must preserve rows and local/external IDs.");

var requiredTables = new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AppUsers", "Projects", "Products", "Authors", "ProductAuthors", "Faculties", "AcademicTerms", "Groups", "GroupMembers", "FacultyScopes", "UserFacultyScopeAssignments" };
foreach (var table in requiredTables)
    Check(await ScalarAsync<int>(connectionString, $"SELECT COUNT(*) FROM sys.tables WHERE name=N'{table}' AND schema_id=SCHEMA_ID('dbo')") == 1,
        $"Missing table {table}.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.key_constraints WHERE type='PK' AND parent_object_id=OBJECT_ID('dbo.Faculties')") == 1, "Faculty PK missing.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.foreign_keys WHERE name='FK_Faculties_Faculties_ParentFacultyId' AND delete_referential_action_desc='NO_ACTION'") == 1,
    "Faculty self FK must use NO ACTION.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action_desc <> 'NO_ACTION'") == 0,
    "Every Unified FK must use NO ACTION.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Faculties') AND name='IX_Faculties_ExternalFacultyId' AND is_unique=1 AND filter_definition IS NOT NULL") == 1,
    "Faculty external ID filtered unique index missing.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Faculties') AND name='IX_Faculties_ParentFacultyId'") == 1,
    "Faculty parent index missing.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Faculties') AND name='ParentFacultyId' AND is_nullable=1") == 1,
    "Faculty parent column must be nullable.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AcademicTerms') AND name='CK_AcademicTerms_Dates'") == 1,
    "AcademicTerm dates check missing.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Products') AND name='ProjectId' AND is_nullable=1") == 1,
    "Product.ProjectId must be nullable.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.check_constraints WHERE name IN ('CK_Authors_ExactlyOneSource','CK_ProductAuthors_PositiveOrder')") == 2,
    "Author/ProductAuthor checks missing.");
Check(await ScalarAsync<int>(connectionString,
    "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AppUsers') AND is_unique=1 AND filter_definition IS NOT NULL AND name IN ('IX_AppUsers_IdAsp','IX_AppUsers_IdLocal')") == 2,
    "AppUser filtered unique indexes missing.");

await ExecuteAsync(connectionString, """
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
SET IDENTITY_INSERT dbo.Faculties ON;
INSERT dbo.Faculties(FacultyId,ExternalFacultyId,ParentFacultyId,Acronym,Name,IsActive,CreatedAt,LastSyncedAt)
VALUES(17,500,NULL,N'ING',N'Ingeniería',1,SYSUTCDATETIME(),NULL),(29,800,17,N'SW',N'Software',1,SYSUTCDATETIME(),NULL);
SET IDENTITY_INSERT dbo.Faculties OFF;
SET IDENTITY_INSERT dbo.AcademicTerms ON;
INSERT dbo.AcademicTerms(AcademicTermId,ExternalPeriodId,StartDate,EndDate,LastSyncedAt,Name)
VALUES(8,29991,'2026-01-01','2026-06-30',NULL,N'Fixture constraint term');
SET IDENTITY_INSERT dbo.AcademicTerms OFF;
""");
Check(await ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM dbo.Faculties WHERE FacultyId=29 AND ExternalFacultyId=800 AND ParentFacultyId=17") == 1,
    "Faculty parent must use the local key, not the external ID.");
Check(await ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM dbo.AcademicTerms WHERE AcademicTermId=8 AND ExternalPeriodId=29991") == 1,
    "AcademicTerm local and external IDs must remain distinct.");
await ExpectSqlFailureAsync("INSERT dbo.Faculties(ExternalFacultyId,Name,IsActive,CreatedAt) VALUES(500,N'Duplicate',1,SYSUTCDATETIME())", 2601, 2627);
await ExpectSqlFailureAsync("INSERT dbo.Faculties(ExternalFacultyId,ParentFacultyId,Name,IsActive,CreatedAt) VALUES(801,999999,N'Orphan',1,SYSUTCDATETIME())", 547);
await ExpectSqlFailureAsync("DELETE dbo.Faculties WHERE FacultyId=17", 547);
await ExpectSqlFailureAsync("INSERT dbo.AcademicTerms(ExternalPeriodId,StartDate,EndDate,Name) VALUES(29991,'2026-07-01','2026-07-31',N'Duplicate')", 2601, 2627);
await ExpectSqlFailureAsync("INSERT dbo.AcademicTerms(ExternalPeriodId,StartDate,EndDate,Name) VALUES(29992,'2026-12-31','2026-01-01',N'Invalid dates')", 547);

var snapshot = new FakeSnapshotClient();
var directoryFake = new FakeDirectoryClient();
var distributivosFake = new FakeDistributivosService();
var academicsGuard = DispatchGuard.For<IExternalAcademicsService>();
var periodsGuard = DispatchGuard.For<IExternalPeriodsClient>();
var services = new ServiceCollection();
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ExternalApis:BaseUrl"] = "http://127.0.0.1/",
    ["ExternalApis:TimeoutSeconds"] = "1",
    ["Storage:RootPath"] = Path.GetTempPath(),
    ["DocumentRecognition:Endpoint"] = "http://127.0.0.1/"
}).Build();
services.AddUnifiedDide(configuration, options => options.UseSqlServer(connectionString,
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));
services.RemoveAll<IUnifiedAcademicCatalogSnapshotClient>();
services.AddSingleton<IUnifiedAcademicCatalogSnapshotClient>(snapshot);
services.RemoveAll<IExternalDirectoryClient>();
services.AddSingleton<IExternalDirectoryClient>(directoryFake);
services.RemoveAll<IExternalDistributivosService>();
services.AddSingleton<IExternalDistributivosService>(distributivosFake);
services.RemoveAll<IExternalAcademicsService>();
services.AddSingleton(academicsGuard.Instance);
services.RemoveAll<IExternalPeriodsClient>();
services.AddSingleton(periodsGuard.Instance);
var currentUser = new FixedCurrentUser();
services.RemoveAll<ICurrentUserService>();
services.AddSingleton<ICurrentUserService>(currentUser);
await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

snapshot.Faculties =
[
    new() { Id=500, ParentId=null, Name="Ingeniería", Acronym="ING" },
    new() { Id=800, ParentId=500, Name="Software", Acronym="SW" },
    new() { Id=900, ParentId=800, Name="IA", Acronym="IA" }
];
await using (var scope = provider.CreateAsyncScope())
{
    var sync = scope.ServiceProvider.GetRequiredService<IUnifiedFacultySynchronizationService>();
    Check((await sync.SynchronizeAsync()).Success, "First faculty sync failed.");
}
var originalSoftwarePk = await ScalarAsync<int>(connectionString, "SELECT FacultyId FROM dbo.Faculties WHERE ExternalFacultyId=800");
var originalIaPk = await ScalarAsync<int>(connectionString, "SELECT FacultyId FROM dbo.Faculties WHERE ExternalFacultyId=900");
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedFacultySynchronizationService>().SynchronizeAsync()).Success,
        "Idempotent faculty sync failed.");
Check(await ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM dbo.Faculties WHERE ExternalFacultyId IN (500,800,900)") == 3,
    "Faculty sync duplicated rows.");
Check(await ScalarAsync<int>(connectionString, $"SELECT COUNT(*) FROM dbo.Faculties WHERE FacultyId={originalIaPk} AND ParentFacultyId={originalSoftwarePk}") == 1,
    "Faculty sync did not translate the external parent into the local parent key.");
snapshot.Faculties =
[
    new() { Id=500, ParentId=null, Name="Ingeniería actualizada", Acronym="ING" },
    new() { Id=700, ParentId=null, Name="Ciencias", Acronym="C" },
    new() { Id=800, ParentId=700, Name="Software", Acronym="SW" },
    new() { Id=900, ParentId=800, Name="IA", Acronym="IA" }
];
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedFacultySynchronizationService>().SynchronizeAsync()).Success,
        "Faculty update/move sync failed.");
Check(await ScalarAsync<int>(connectionString, $"SELECT COUNT(*) FROM dbo.Faculties child JOIN dbo.Faculties parent ON child.ParentFacultyId=parent.FacultyId WHERE child.FacultyId={originalSoftwarePk} AND parent.ExternalFacultyId=700") == 1,
    "Faculty move did not preserve the row and update its local parent FK.");

snapshot.Terms = [new() { PeriodId=20261, Name="2026-I", StartDate=new(2026,1,1), EndDate=new(2026,6,30) }];
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedAcademicTermSynchronizationService>().SynchronizeAsync()).Success,
        "First academic-term sync failed.");
var termPk = await ScalarAsync<int>(connectionString, "SELECT AcademicTermId FROM dbo.AcademicTerms WHERE ExternalPeriodId=20261");
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedAcademicTermSynchronizationService>().SynchronizeAsync()).Success,
        "Idempotent academic-term sync failed.");
Check(await ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM dbo.AcademicTerms WHERE ExternalPeriodId=20261") == 1,
    "Academic-term sync duplicated rows.");
snapshot.Terms[0].Name = "2026-I actualizado";
snapshot.Terms[0].EndDate = new(2026, 7, 1);
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedAcademicTermSynchronizationService>().SynchronizeAsync()).Success,
        "Academic-term update sync failed.");
Check(await ScalarAsync<int>(connectionString, $"SELECT COUNT(*) FROM dbo.AcademicTerms WHERE AcademicTermId={termPk} AND ExternalPeriodId=20261 AND Name=N'2026-I actualizado'") == 1,
    "Academic-term update changed identity or failed to persist.");

int actorLocalId;
int actorAppUserId;
await using (var scope = provider.CreateAsyncScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    foreach (var roleName in new[] { "user", "coordinador" })
        if (!await roles.RoleExistsAsync(roleName)) Check((await roles.CreateAsync(new(roleName))).Succeeded, $"Could not create fixture role {roleName}.");
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
    var actor = new IdentityUser<int> { UserName="actor001", Email="actor001@integration.invalid" };
    Check((await users.CreateAsync(actor, TestPassword)).Succeeded, "Could not create actor Identity user.");
    actorLocalId = actor.Id;
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var bridge = new AppUser { IdLocal=actor.Id, IdAsp=40001 };
    context.AppUsers.Add(bridge);
    await context.SaveChangesAsync();
    actorAppUserId = bridge.IdUser;
}
currentUser.UserId = actorLocalId;

int provisionedAppUserId;
int provisionedLocalId;
await using (var scope = provider.CreateAsyncScope())
{
    var identity = scope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>();
    var request = new RegisterRequest { Email="known@integration.invalid", Username="known001", Password=TestPassword, AspUserId=41001, Role="user" };
    var first = await identity.EnsureAsync(request);
    Check(first.Success, "Identity provisioning failed.");
    provisionedAppUserId = first.Data;
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var bridge = await context.AppUsers.SingleAsync(x => x.IdUser == provisionedAppUserId);
    provisionedLocalId = bridge.IdLocal!.Value;
    Check(bridge.IdAsp == 41001 && await context.Users.AnyAsync(x => x.Id == provisionedLocalId), "AppUser bridge does not point to a real Identity row.");
}
await using (var scope = provider.CreateAsyncScope())
{
    var identity = scope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>();
    var reused = await identity.EnsureAsync(new() { Email="known@integration.invalid", Username="known001", Password=TestPassword, AspUserId=41001, Role="user" });
    Check(reused.Success && reused.Data == provisionedAppUserId, "Known IdAsp was not reused.");
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
    var other = new IdentityUser<int> { UserName="other001", Email="other@integration.invalid" };
    Check((await users.CreateAsync(other, TestPassword)).Succeeded, "Could not create conflicting Identity fixture.");
    var conflict = await identity.ResolveAsync(new() { Email=other.Email!, Username=other.UserName!, Password=TestPassword, AspUserId=41001, Role="user" });
    Check(!conflict.Success && conflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict,
        "Identity mapping conflict did not reject relinking.");
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    Check((await context.AppUsers.SingleAsync(x => x.IdUser == provisionedAppUserId)).IdLocal == provisionedLocalId,
        "Identity conflict relinked the AppUser.");
}

await using (var scope = provider.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    context.AppUsers.Add(new() { IdLocal=provisionedLocalId, IdAsp=49991 });
    try { await context.SaveChangesAsync(); throw new InvalidOperationException("Expected DbUpdateException for duplicate IdLocal."); }
    catch (DbUpdateException) { assertions++; context.ChangeTracker.Clear(); }
    context.AppUsers.Add(new() { IdAsp=41001 });
    try { await context.SaveChangesAsync(); throw new InvalidOperationException("Expected DbUpdateException for duplicate IdAsp."); }
    catch (DbUpdateException) { assertions++; context.ChangeTracker.Clear(); }
    context.Groups.Add(new Group { GroupTypeId=999999, Name="tracked-only" });
    var guarded = await scope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>().EnsureAsync(
        new() { Email="tracked@integration.invalid", Username="tracked01", Password=TestPassword, AspUserId=42001, Role="user" });
    Check(!guarded.Success && guarded.ErrorCode == ErrorCodes.IdentityProvisioning.PendingChanges,
        "Identity provisioning did not reject pending domain changes.");
    Check(!await context.Users.AnyAsync(x => x.Email == "tracked@integration.invalid"), "Tracked-change guard accidentally provisioned Identity.");
}

await ExecuteAsync(connectionString, """
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
SET IDENTITY_INSERT dbo.GroupTypes ON; INSERT dbo.GroupTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Integrantes',1,1); SET IDENTITY_INSERT dbo.GroupTypes OFF;
SET IDENTITY_INSERT dbo.MemberRoleTypes ON; INSERT dbo.MemberRoleTypes(Id,Name,IsActive,IsLocked,Flag) VALUES(1,N'Coordinador',1,1,1),(3,N'Investigador',1,1,1); SET IDENTITY_INSERT dbo.MemberRoleTypes OFF;
SET IDENTITY_INSERT dbo.ProjectTypes ON; INSERT dbo.ProjectTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Investigación',1,1); SET IDENTITY_INSERT dbo.ProjectTypes OFF;
SET IDENTITY_INSERT dbo.ProjectStates ON; INSERT dbo.ProjectStates(Id,Name,IsActive,IsLocked) VALUES(3,N'En ejecución',1,1); SET IDENTITY_INSERT dbo.ProjectStates OFF;
SET IDENTITY_INSERT dbo.ProjectOriginTypes ON; INSERT dbo.ProjectOriginTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Interno',1,1); SET IDENTITY_INSERT dbo.ProjectOriginTypes OFF;
SET IDENTITY_INSERT dbo.Convocations ON; INSERT dbo.Convocations(Id,Code,Name,IsActive,IsLocked) VALUES(1,N'INT',N'Integración',1,1); SET IDENTITY_INSERT dbo.Convocations OFF;
SET IDENTITY_INSERT dbo.ProductTypes ON; INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Producto',1,1); SET IDENTITY_INSERT dbo.ProductTypes OFF;
""");

int groupId;
await using (var scope = provider.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var group = new Group { GroupTypeId=GroupTypeIds.Integrantes, Name="Grupo SQL" };
    context.Groups.Add(group);
    await context.SaveChangesAsync();
    groupId = group.GroupId;
}

int projectId;
var snapshotCallsBeforeBusiness = snapshot.Calls;
await using (var scope = provider.CreateAsyncScope())
{
    var projects = scope.ServiceProvider.GetRequiredService<IUnifiedProjectService>();
    var created = await projects.CreateAsync(new UnifiedAddProjectRequestDTO
    {
        ProjectName="Proyecto SQL básico", ProjectTypeId=1, ProjectGroupId=groupId,
        ApprovalDate=new(2026,1,1), StartDate=new(2026,1,2), ProjectCode="SQL-1",
        ProjectStateId=3, DurationInMonths=6, ExternalFacultyId=500, ConvocationId=1, ProjectOriginTypeId=1
    });
    Check(created.Success, $"Basic Project service flow failed: {created.ErrorCode} / {created.Message}");
    projectId = created.Data!.ProjectId;
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var basicProject = await context.Projects.SingleAsync(x => x.ProjectId == projectId);
    Check(basicProject.FacultyId == 17 && basicProject.ProjectOriginTypeId == 1 && basicProject.ApprovalDate == new DateTime(2026,1,1),
        "Project did not persist its local Faculty FK, origin, and approval date.");
    var missing = await projects.CreateAsync(new UnifiedAddProjectRequestDTO
    {
        ProjectName="Proyecto facultad ausente", ProjectTypeId=1, ProjectGroupId=groupId,
        ApprovalDate=new(2026,1,1), StartDate=new(2026,1,2), ProjectCode="SQL-X",
        ProjectStateId=3, DurationInMonths=6, ExternalFacultyId=999999, ConvocationId=1, ProjectOriginTypeId=1
    });
    Check(!missing.Success, "Unsynchronized Faculty reference should fail.");
}
Check(snapshot.Calls == snapshotCallsBeforeBusiness, "Project business flow invoked an academic HTTP snapshot.");

directoryFake.Profile = new ExternalUserProfileModel
{
    ExternalId=50001, FullName="Coordinator Fixture", Document="coord001", Email="coordinator@integration.invalid", AspId=46001,
    Careers=[new() { TeacherFacultyCareerId=1, IsActive=true, FacultyCareerId=800, FacultyCareerName="Software", FacultyId=500, FacultyName="Ingeniería" }]
};
distributivosFake.Rows = [new()
{
    DistributivoId=1, PeriodId=20261, PeriodName="2026-I", FacultyId=500, CareerId=800,
    FacultyName="Ingeniería", CareerName="Software", AspId=46001, Document="coord001",
    Email="coordinator@integration.invalid", FullName="Coordinator Fixture", Hours=40
}];
AddProjectFullRequestDTO FullRequest(string name) => new()
{
    Project = new()
    {
        ProjectName=name, ProjectTypeId=1, ProjectCode="FULL-", ProjectStateId=3,
        ApprovalDate=new(2026,1,1), StartDate=new(2026,2,1), DurationInMonths=6,
        FacultyId=800, ConvocationId=1, ProjectOriginTypeId=1
    },
    ProjectDocumentData = new DocumentResponseDTO(),
    GroupMembers = [new() { MemberRole=MemberRoleTypeIds.Coordinador, Email="coordinator@integration.invalid", Document="coord001", AspUserId=46001 }]
};
await ExecuteAsync(connectionString, "DELETE dbo.AcademicTerms");
await using (var scope = provider.CreateAsyncScope())
{
    var missingTerm = await scope.ServiceProvider.GetRequiredService<IUnifiedProjectService>().CreateFullAsync(FullRequest("Full sin período"));
    Check(!missingTerm.Success && missingTerm.ErrorCode == ErrorCodes.AcademicReferences.AcademicTermNotSynchronized,
        "CreateFull must fail appropriately when AcademicTerm is not synchronized.");
}
await using (var scope = provider.CreateAsyncScope())
    Check((await scope.ServiceProvider.GetRequiredService<IUnifiedAcademicTermSynchronizationService>().SynchronizeAsync()).Success,
        "Could not restore AcademicTerm snapshot after missing-term test.");
await using (var scope = provider.CreateAsyncScope())
{
    var full = await scope.ServiceProvider.GetRequiredService<IUnifiedProjectService>().CreateFullAsync(FullRequest("Proyecto SQL Full"));
    Check(full.Success, $"CreateFull service flow failed: {full.ErrorCode} / {full.Message}");
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var movedRootId = await context.Faculties.Where(x => x.ExternalFacultyId == 700).Select(x => x.FacultyId).SingleAsync();
    Check(await context.Projects.AnyAsync(x => x.ProjectId == full.Data!.ProjectId && x.FacultyId == movedRootId),
        "CreateFull did not persist the root local Faculty FK.");
    Check(await context.AppUsers.AnyAsync(x => x.IdAsp == 46001) && await context.Users.AnyAsync(x => x.Email == "coordinator@integration.invalid"),
        "CreateFull did not persist Identity/AppUser through the real store.");
}

await using (var scope = provider.CreateAsyncScope())
{
    var groups = scope.ServiceProvider.GetRequiredService<IUnifiedGroupService>();
    var added = await groups.AddMemberAsync(new AddGroupMemberRequestDTO
    {
        GroupId=groupId, MemberRole=MemberRoleTypeIds.Investigador, Email="member@integration.invalid", Document="member001", AspUserId=43001
    });
    Check(added.Success, "Group AddMember service flow failed.");
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var member = await context.GroupMembers.SingleAsync(x => x.GroupMemberId == added.Data!.GroupMemberId);
    Check(await context.AppUsers.AnyAsync(x => x.IdUser == member.UserId), "GroupMember does not reference AppUser.IdUser.");
}

await using (var scope = provider.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var facultyScope = new FacultyScope { Name="Scope SQL", IsActive=true };
    context.FacultyScopes.Add(facultyScope);
    await context.SaveChangesAsync();
    var result = await scope.ServiceProvider.GetRequiredService<IUnifiedFacultyScopeService>().AssignScopeToUserAsync(
        facultyScope.FacultyScopeId, new() { Email="scope@integration.invalid", Document="scope001", AspUserId=44001 });
    Check(result.Success, "FacultyScope assignment service flow failed.");
    var assignment = await context.UserFacultyScopeAssignments.SingleAsync(x => x.FacultyScopeId == facultyScope.FacultyScopeId);
    var appUser = await context.AppUsers.SingleAsync(x => x.IdAsp == 44001);
    Check(assignment.IdentityUserId == appUser.IdUser, "FacultyScope assignment must reference AppUser.IdUser.");
}

await using (var scope = provider.CreateAsyncScope())
{
    var authorService = scope.ServiceProvider.GetRequiredService<IUnifiedAuthorService>();
    var author = await authorService.CreateAsync(new(provisionedAppUserId, null, "0000-0001"));
    Check(author.Success, "Institutional Author service flow failed.");
    var product = await scope.ServiceProvider.GetRequiredService<IUnifiedProductService>().CreateAsync(new ProductCreateRequestDTO
    {
        ProjectId=null, Title="Producto SQL independiente", ProductTypeId=1, AuthorUserIds=[provisionedAppUserId], Values=[]
    });
    Check(product.Success, "Product service flow failed.");
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    var persisted = await context.Products.Include(x => x.Authors).SingleAsync(x => x.Id == product.Data!.Id);
    Check(persisted.ProjectId is null && persisted.Authors!.Count == 1, "Product nullable Project or ProductAuthor persistence failed.");
    var usersBefore = await context.Users.CountAsync();
    var appUsersBefore = await context.AppUsers.CountAsync();
    var external = new ExternalResearcher { FullName="External Fixture", Email="external@integration.invalid" };
    context.ExternalResearchers.Add(external);
    await context.SaveChangesAsync();
    var externalAuthor = await authorService.CreateAsync(new(null, external.ExternalResearcherId, "0000-0002"));
    Check(externalAuthor.Success, "ExternalResearcher Author flow failed.");
    Check(await context.Users.CountAsync() == usersBefore && await context.AppUsers.CountAsync() == appUsersBefore,
        "ExternalResearcher created Identity/AppUser unexpectedly.");
}
await ExpectSqlFailureAsync("INSERT dbo.Authors(AppUserId,ExternalResearcherId) VALUES(NULL,NULL)", 547);
await ExpectSqlFailureAsync("INSERT dbo.ProductAuthors(ProductId,AuthorId,AuthorOrder,IsPrimaryAuthor,CreatedAt) SELECT TOP(1) p.Id,a.AuthorId,0,0,SYSUTCDATETIME() FROM dbo.Products p CROSS JOIN dbo.Authors a WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductAuthors pa WHERE pa.ProductId=p.Id AND pa.AuthorId=a.AuthorId) ORDER BY a.AuthorId DESC", 547);

int durableIdentityId;
await using (var scope = provider.CreateAsyncScope())
{
    var identity = await scope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>().EnsureAsync(
        new() { Email="durable@integration.invalid", Username="durable1", Password=TestPassword, AspUserId=45001, Role="user" });
    Check(identity.Success, "Durable Identity provisioning failed.");
    durableIdentityId = identity.Data;
}
await using (var scope = provider.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    context.Projects.Add(new Project
    {
        ProjectCode="BAD-FK", ProjectName="Invalid after Identity", CreatedByUserId=durableIdentityId,
        ProjectTypeId=999999, ProjectStateId=3, ProjectGroupId=groupId, ProjectOriginTypeId=1,
        ProjectNumber=999, ConvocationId=1, DurationInMonths=1, FacultyId=17
    });
    try { await context.SaveChangesAsync(); throw new InvalidOperationException("Expected domain DbUpdateException."); }
    catch (DbUpdateException) { assertions++; }
}
await using (var scope = provider.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
    Check(await context.AppUsers.AnyAsync(x => x.IdUser == durableIdentityId && x.IdAsp == 45001),
        "Identity/AppUser did not remain after later Project failure.");
    Check(!await context.Projects.AnyAsync(x => x.ProjectCode == "BAD-FK"), "Failed Project left an unexpected domain row.");
    context.AcademicTerms.Add(new() { ExternalPeriodId=20262, Name="Recovery", StartDate=new(2026,8,1), EndDate=new(2026,12,31) });
    await context.SaveChangesAsync();
    Check(await context.AcademicTerms.AnyAsync(x => x.ExternalPeriodId == 20262), "Fresh context could not operate after a DbUpdateException.");
}

Check(academicsGuard.Calls == 0, "ExternalAcademics was called by business integration flows.");
Check(periodsGuard.Calls == 0, "ExternalPeriods was called by business integration flows.");
Console.WriteLine($"PASS: {assertions} SQL Server integration assertions");
Console.WriteLine($"Server: {Server}");
Console.WriteLine($"Databases: {IntegrationDatabase}, {UpgradeDatabase}");
Console.WriteLine($"Snapshot fake calls: {snapshot.Calls}; ExternalAcademics business calls: {academicsGuard.Calls}; ExternalPeriods business calls: {periodsGuard.Calls}");

internal sealed class FixedCurrentUser : ICurrentUserService
{
    public int UserId { get; set; }
    public int? GetUserId() => UserId;
    public int GetRequiredUserId() => UserId;
}

internal sealed class FakeSnapshotClient : IUnifiedAcademicCatalogSnapshotClient
{
    public List<ExternalFacultyCareerFlatModel> Faculties { get; set; } = [];
    public List<ExternalAcademicPeriodModel> Terms { get; set; } = [];
    public int Calls { get; private set; }
    public Task<ServiceResult<List<ExternalFacultyCareerFlatModel>>> GetFacultiesAsync(CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<List<ExternalFacultyCareerFlatModel>>.Ok(Faculties)); }
    public Task<ServiceResult<List<ExternalAcademicPeriodModel>>> GetAcademicTermsAsync(CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<List<ExternalAcademicPeriodModel>>.Ok(Terms)); }
}

internal sealed class FakeDirectoryClient : IExternalDirectoryClient
{
    public ExternalUserProfileModel Profile { get; set; } = new();
    public int Calls { get; private set; }
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok([Profile])); }
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByDocumentsAsync(IEnumerable<string> documents, CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok([Profile])); }
    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetAllAsync(CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok([Profile])); }
}

internal sealed class FakeDistributivosService : IExternalDistributivosService
{
    public List<ExternalTeacherDistributivoModel> Rows { get; set; } = [];
    public int Calls { get; private set; }
    private Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> Result(CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<List<ExternalTeacherDistributivoModel>>.Ok(Rows)); }
    public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosAsync(CancellationToken ct = default) => Result(ct);
    public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCedulasAsync(IEnumerable<string> cedulas, CancellationToken ct = default) => Result(ct);
    public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCorreosAsync(IEnumerable<string> correos, CancellationToken ct = default) => Result(ct);
    public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByPeriodosAsync(IEnumerable<string> periodos, CancellationToken ct = default) => Result(ct);
    public Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByFacultadesAsync(IEnumerable<string> facultades, CancellationToken ct = default) => Result(ct);
    public Task<ServiceResult<ExternalTeacherDistributivoModel>> GetDistributivoByIdAsync(int distributivoId, CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(ServiceResult<ExternalTeacherDistributivoModel>.Ok(Rows.Single(x => x.DistributivoId == distributivoId))); }
}

internal class DispatchGuard : DispatchProxy
{
    public int Calls { get; private set; }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls++;
        throw new InvalidOperationException($"Unexpected external call: {targetMethod!.Name}");
    }
    public static GuardHandle<T> For<T>() where T : class
    {
        var instance = Create<T, DispatchGuard>();
        return new(instance, (DispatchGuard)(object)instance);
    }
}

internal sealed record GuardHandle<T>(T Instance, DispatchGuard Proxy) where T : class
{
    public int Calls => Proxy.Calls;
}
