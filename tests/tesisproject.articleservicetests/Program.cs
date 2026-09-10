using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

var database = "tesis_article_services_test_" + Guid.NewGuid().ToString("N");
var server = Environment.GetEnvironmentVariable("ARTICLES_TEST_SQL_SERVER") ?? @".\DINNOVA";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
builder.Configuration["ExternalApis:BaseUrl"] = "https://directory.invalid/";
builder.Services.AddUnifiedDide(builder.Configuration, o => o.UseSqlServer(
    $@"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True",
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));
await using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
var http = provider.GetRequiredService<IHttpContextAccessor>();
var count = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; Console.WriteLine("PASS: " + message); }
void Principal(string? id, string? role = null, string? permission = null, string? email = null)
{
    var claims = new List<Claim>();
    if (id is not null) claims.Add(new(ClaimTypes.NameIdentifier, id));
    if (role is not null) claims.Add(new(ClaimTypes.Role, role));
    if (permission is not null) claims.Add(new(ArticlePermissions.ClaimType, permission));
    if (email is not null) claims.Add(new(ClaimTypes.Email, email));
    http.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
}
async Task<T> Matrix<T>(Func<IUnifiedRegistrationMatrixService, Task<T>> action)
{
    await using var operation = provider.CreateAsyncScope();
    return await action(operation.ServiceProvider.GetRequiredService<IUnifiedRegistrationMatrixService>());
}
try
{
    Check(true, "Full AddUnifiedDide composition passes ValidateOnBuild/ValidateScopes");
    Check(!builder.Services.Any(d => d.ServiceType == typeof(IRegistrationMatrixService) || d.ServiceType == typeof(IArticleQueryService) || d.ServiceType == typeof(IArticleUserContext)),
        "Unified registrations do not cut over legacy service contracts");
    await db.Database.MigrateAsync();
    var title = new FieldCatalogEntry { EntityName = "Article", FieldKey = "Title", FieldLabel = "Title", DataType = "string", SourceType = "Physical",
        IsActive = true, IsVisible = true, IsEditable = true, CreatedAt = DateTime.UtcNow };
    var hidden = new FieldCatalogEntry { EntityName = "Article", FieldKey = "Hidden", FieldLabel = "Hidden", DataType = "string", SourceType = "Dynamic", IsActive = true };
    db.AddRange(title, hidden); await db.SaveChangesAsync();
    Principal("owner-A");
    var created = await Matrix(s => s.CreateMatrixAsync(new() { Name = " Matrix A ", FieldIds = [title.FieldId, hidden.FieldId, title.FieldId] }, "owner-A"));
    Check(created.Success && created.Data!.Summary.Name == "Matrix A" && created.Data.Columns.Count == 1 && created.Data.Columns[0].WidthUnits == 2,
        "Matrix create preserves validation, field eligibility, deduplication and width rules");
    var id = created.Data!.Summary.RegistrationMatrixId;
    Check((await Matrix(s => s.GetMatricesAsync(50, "owner-A", false))).Data!.Count == 1
        && (await Matrix(s => s.GetMatricesAsync(50, null, false))).Data!.Count == 0,
        "Owner/no-owner scope");
    Principal("owner-B");
    Check(!(await Matrix(s => s.GetMatrixAsync(id, "owner-A", false))).Success
        && !(await Matrix(s => s.GetMatrixAsync(id, "owner-B", true))).Success,
        "Forged owner and includeAll do not grant access");
    Check(!(await Matrix(s => s.CreateMatrixAsync(new() { Name = "Orphan", FieldIds = [title.FieldId] }, null))).Success
        && !await db.Set<RegistrationMatrix>().AnyAsync(m => m.Name == "Orphan"), "No-owner create cannot leave an inaccessible matrix");
    Principal("analyst", ArticleRoles.Analyst);
    Check((await Matrix(s => s.GetMatrixAsync(id, null, true))).Success, "CanManageAll: Analyst may access all");
    Principal("manager", permission: ArticlePermissions.ManageConfiguration);
    Check((await Matrix(s => s.GetMatrixAsync(id, null, true))).Success, "CanManageAll: explicit permission may access all");
    Principal("owner-A");
    var withRow = await Matrix(s => s.AddRowAsync(id, "owner-A", false));
    var rowId = withRow.Data!.Rows.Single().RegistrationMatrixRowId;
    Check(withRow.Success && withRow.Data.Rows.Single().RowNumber == 1, "Add row uses next row number");
    Check(!(await Matrix(s => s.UpdateCellAsync(id, rowId, new() { FieldId = hidden.FieldId, RawValue = "not allowed" }, "owner-A", false))).Success, "Unassigned cell field rejected");
    var cell = await Matrix(s => s.UpdateCellAsync(id, rowId, new() { FieldId = title.FieldId, RawValue = " trimmed " }, "owner-A", false));
    Check(cell.Success && cell.Data!.Rows.Single().Cells.Single().RawValue == "trimmed", "Create/update cell trims values");
    var removedCell = await Matrix(s => s.UpdateCellAsync(id, rowId, new() { FieldId = title.FieldId, RawValue = " " }, "owner-A", false));
    Check(removedCell.Success && removedCell.Data!.Rows.Single().Cells.Count == 0, "Blank cell deletes saved value");
    await db.Set<RegistrationMatrix>().Where(m => m.RegistrationMatrixId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, "Submitted"));
    Check(!(await Matrix(s => s.AddRowAsync(id, "owner-A", false))).Success && !(await Matrix(s => s.DeleteMatrixAsync(id, "owner-A", false))).Success,
        "Non-Draft matrix cannot mutate or delete");
    Check((await Matrix(s => s.SubmitToStagingAsync(id, new(), "owner-A", false))).ErrorCode == "ARTICLES_MATRIX_STAGING_PENDING", "Staging remains unimplemented");
    await db.Set<RegistrationMatrix>().Where(m => m.RegistrationMatrixId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, "Draft"));
    Check((await Matrix(s => s.DeleteMatrixAsync(id, "owner-A", false))).Success && !await db.Set<RegistrationMatrixRow>().AnyAsync(r => r.RegistrationMatrixId == id), "Delete Draft graph respects Unified NoAction");

    var local = new IdentityUser<int> { UserName = "institutional", Email = "original@example.test", NormalizedEmail = "ORIGINAL@EXAMPLE.TEST" };
    db.Users.Add(local); await db.SaveChangesAsync();
    var appUser = new AppUser { IdLocal = local.Id, IdAsp = 12345 };
    db.Add(appUser); await db.SaveChangesAsync();
    Principal(local.Id.ToString(), email: "changed-claim@example.test");
    await using (var identityScope = provider.CreateAsyncScope())
    {
        var user = identityScope.ServiceProvider.GetRequiredService<IUnifiedArticleUserContext>();
        Check(await user.GetRequiredAppUserIdAsync() == appUser.IdUser && user.IdentityUserId == local.Id, "Article user context resolves institutional AppUser by IdLocal, never email");
        var conflict = await identityScope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>().ResolveAsync(new()
        { AspUserId = 99999, Username = "different", Email = local.Email!, Password = "unused" });
        Check(!conflict.Success && conflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict
            && await db.Set<AppUser>().AnyAsync(a => a.IdUser == appUser.IdUser && a.IdLocal == local.Id && a.IdAsp == 12345), "Identity mapping conflict does not relink");
    }
    Principal("999999", email: local.Email);
    await using (var missingScope = provider.CreateAsyncScope())
        Check(await missingScope.ServiceProvider.GetRequiredService<IUnifiedArticleUserContext>().GetAppUserIdAsync() is null, "Missing IdLocal does not fall back to email");

    await db.Database.ExecuteSqlRawAsync("""
        SET IDENTITY_INSERT dbo.ProductTypes ON;
        INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Scientific',1,0),(2,N'Regional',1,0);
        SET IDENTITY_INSERT dbo.ProductTypes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributes ON;
        INSERT dbo.ProductAttributes(Id,Name,IsActive,IsLocked,DataType) VALUES(3,N'Journal',1,0,0),(6,N'Quartile',1,0,0),(8,N'DOI',1,0,0),(9,N'Year',1,0,0);
        SET IDENTITY_INSERT dbo.ProductAttributes OFF;
        INSERT dbo.ProductAttributeDefinitions(ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT 1,Id,0,Id FROM dbo.ProductAttributes;
        """);
    var form = new FormDefinition { FormKey = "ArticleForm", FormName = "Article", EntityName = "Article", IsActive = true,
        Fields = [new() { FieldId = title.FieldId, IsVisible = true, IsEditable = true }] };
    var external = new ExternalResearcher { FullName = "External", Email = "external@example.test" };
    db.AddRange(form, external); await db.SaveChangesAsync();
    await using (var writingScope = provider.CreateAsyncScope())
    {
        var result = await writingScope.ServiceProvider.GetRequiredService<IUnifiedArticleRegistrationCommandService>().RegisterAsync(new()
        {
            FormKey = "ArticleForm", Article = new() { Title = "Canonical article", ProductTypeId = 1, Doi = "10.services/read", Year = 2026 },
            Venue = new() { JournalName = "Canonical journal" }, VenueMetric = new() { Quartile = "Q2" },
            Participants = [new() { Index = 1, Nombre = "Snapshot author", ExternalResearcherId = external.ExternalResearcherId, Participacion = "Autor" }]
        });
        Check(result.Success, "Approved Unified registration remains functional: " + result.Message);
        var reading = writingScope.ServiceProvider.GetRequiredService<IUnifiedArticleQueryService>();
        var page = await reading.GetPageAsync(new() { Search = "10.services", Year = 2026, Page = 0, PageSize = 999 });
        Check(page.Success && page.Data!.Page == 1 && page.Data.PageSize == 100 && page.Data.TotalCount == 1
            && page.Data.Items.Single().Id == result.Data!.ArticleId && page.Data.Items.Single().VenueName == "Canonical journal"
            && page.Data.Items.Single().AuthorsSummary == "Snapshot author", "Unified query preserves pagination and DTOs with canonical view values and author snapshots");
        var detail = await reading.GetDetailAsync(result.Data!.ArticleId);
        Check(detail.Success && detail.Data!.Doi == "10.services/read" && detail.Data.Year == 2026 && detail.Data.Quartile == "Q2"
            && detail.Data.Participants!.Single().Id == result.Data.ParticipantIds.Single(), "Detail reads ProductValues and ProductAuthor IDs immediately after registration");
        Check((await reading.GetDetailAsync(0)).ErrorCode == "ARTICLE_ID_INVALID"
            && (await reading.GetDetailAsync(int.MaxValue)).ErrorCode == "ARTICLE_NOT_FOUND", "Read validation/not-found contracts preserved");
    }
    Console.WriteLine($"PASS: {count} Article service SQL/EF assertions.");
}
finally
{
    if (!database.StartsWith("tesis_article_services_test_", StringComparison.Ordinal)) throw new Exception("Invalid fixture");
    await db.Database.EnsureDeletedAsync();
    http.HttpContext = null;
}
