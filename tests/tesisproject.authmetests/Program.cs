using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tesisproject.backend.Authorization.Articles;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Errors;

var database = "tesis_auth_me_test_" + Guid.NewGuid().ToString("N");
var server = Environment.GetEnvironmentVariable("ARTICLES_TEST_SQL_SERVER") ?? @".\DINNOVA";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
builder.Logging.ClearProviders();
builder.Host.UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Configuration["ExternalApis:BaseUrl"] = "https://directory.invalid/";
builder.Configuration["Storage:RootPath"] = Path.GetTempPath();
builder.Services.AddUnifiedDide(builder.Configuration, o => o.UseSqlServer(
    $@"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True",
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));
builder.Services.Configure<ArticlesModuleOptions>(o => o.Enabled = true);
builder.Services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "fixture"; o.DefaultChallengeScheme = "fixture"; })
    .AddScheme<AuthenticationSchemeOptions, FixtureAuthentication>("fixture", _ => { });
builder.Services.AddAuthorization(ArticlePolicies.Configure);
builder.Services.AddControllers().ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new AuthOnly()));
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
await using var fixture = app.Services.CreateAsyncScope();
var db = fixture.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; Console.WriteLine("PASS: " + message); }
try
{
    Check(true, "AddUnifiedDide DI ValidateOnBuild/ValidateScopes");
    var discovery = new ControllerFeature();
    new ControllerFeatureProvider().PopulateFeature([new AssemblyPart(typeof(UnifiedAuthController).Assembly)], discovery);
    Check(discovery.Controllers.Contains(typeof(UnifiedAuthController).GetTypeInfo()) && !discovery.Controllers.Any(t => t.Name == "AuthController")
        && discovery.Controllers.Contains(typeof(UnifiedArticlesController).GetTypeInfo()), "Unified Auth and Articles active, legacy Auth inactive");
    await db.Database.MigrateAsync();
    var local = new IdentityUser<int> { UserName = "mapped", Email = "mapped@example.test", NormalizedEmail = "MAPPED@EXAMPLE.TEST" };
    var unlinked = new IdentityUser<int> { UserName = "unlinked", Email = "unlinked@example.test" };
    db.Users.AddRange(local, unlinked); await db.SaveChangesAsync();
    var appUser = new AppUser { IdAsp = 45678, IdLocal = local.Id };
    db.Add(appUser); await db.SaveChangesAsync();
    await app.StartAsync();
    using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    Check((int)(await client.GetAsync("/api/auth/me")).StatusCode == 401, "Unauthenticated me challenged");
    void Claim(string id) { client.DefaultRequestHeaders.Remove("X-User"); client.DefaultRequestHeaders.Add("X-User", id); }
    async Task<JsonElement> Me()
    {
        var response = await client.GetAsync("/api/auth/me");
        Check((int)response.StatusCode == 200, "Authenticated me returns 200");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
    Claim(local.Id.ToString());
    var json = await Me(); var data = json.GetProperty("data");
    Check(json.GetProperty("success").GetBoolean() && json.GetProperty("message").GetString() == "Sesion activa."
        && json.GetProperty("errorCode").ValueKind == JsonValueKind.Null && json.GetProperty("validationErrors").ValueKind == JsonValueKind.Null, "ServiceResult contract preserved");
    Check(data.GetProperty("identityUserId").GetInt32() == local.Id && data.GetProperty("appUserId").GetInt32() == appUser.IdUser,
        "Unified IdLocal resolves AppUser.IdUser");
    Check(data.GetProperty("email").GetString() == "claim@example.test" && data.GetProperty("displayName").GetString() == "Fixture User",
        "Email/display name remain claim snapshots, not identity matching keys");
    Check(data.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "identityUserId", "appUserId", "email", "displayName", "roles", "permissions", "articles" }.Order()), "DTO property contract preserved");
    Check(data.GetProperty("articles").GetProperty("canList").GetBoolean() && data.GetProperty("articles").GetProperty("canRegister").GetBoolean(), "Existing Article policies compute access");
    Claim(unlinked.Id.ToString());
    client.DefaultRequestHeaders.Add("X-Email", local.Email);
    Check((await Me()).GetProperty("data").GetProperty("appUserId").ValueKind == JsonValueKind.Null, "Identity without mapping keeps nullable AppUserId; matching email cannot relink");
    client.DefaultRequestHeaders.Remove("X-Email");
    Claim("not-an-id");
    var invalid = await client.GetAsync("/api/auth/me");
    Check((int)invalid.StatusCode == 401 && (await invalid.Content.ReadAsStringAsync()).Contains("AUTH_USER_ID_MISSING"), "Invalid identity claim preserves explicit error");
    await using (var operation = app.Services.CreateAsyncScope())
    {
        var conflict = await operation.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>().ResolveAsync(new()
        { AspUserId = 99999, Username = "conflicting", Email = local.Email!, Password = "unused" });
        Check(!conflict.Success && conflict.ErrorCode == ErrorCodes.IdentityProvisioning.MappingConflict, "Existing Identity boundary returns explicit mapping conflict");
    }
    Claim(local.Id.ToString());
    Check((await Me()).GetProperty("data").GetProperty("appUserId").GetInt32() == appUser.IdUser
        && await db.Set<AppUser>().AnyAsync(a => a.IdLocal == local.Id && a.IdAsp == 45678), "Mapping conflict never relinks; me retains original mapping");
    app.Services.GetRequiredService<IOptions<ArticlesModuleOptions>>().Value.Enabled = false;
    Check((await Me()).GetProperty("data").GetProperty("articles").EnumerateObject().All(p => !p.Value.GetBoolean()), "Disabled Articles returns all access flags false without cutting over");
    Check(await db.Users.CountAsync() == 2 && await db.Set<AppUser>().CountAsync() == 1, "me is read-only; no parallel identity or provisioning");
    Console.WriteLine($"PASS: {checks} auth/me SQL + HTTP checks.");
}
finally
{
    await app.StopAsync();
    if (!database.StartsWith("tesis_auth_me_test_", StringComparison.Ordinal)) throw new Exception("Invalid fixture database");
    await db.Database.EnsureDeletedAsync();
}
sealed class AuthOnly : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    { feature.Controllers.Clear(); feature.Controllers.Add(typeof(UnifiedAuthController).GetTypeInfo()); }
}
sealed class FixtureAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
        !Request.Headers.TryGetValue("X-User", out var id) ? AuthenticateResult.NoResult() : AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Email,Request.Headers.TryGetValue("X-Email", out var email) ? email.ToString() : "claim@example.test"),
                new Claim("name","Fixture User"), new Claim(ClaimTypes.Role,"admin")], Scheme.Name)), Scheme.Name)));
}
