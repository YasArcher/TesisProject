using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;

// Explicit local operator tool. No hosted services, startup migration or automatic sync.
var backend = typeof(UnifiedProjectsController).Assembly;
var root = Path.GetFullPath("tesisproject.backend");
var output = Path.GetFullPath("artifacts/cutover-runtime");
Directory.CreateDirectory(output);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = root, EnvironmentName = "Development", ApplicationName = backend.GetName().Name
});
var startup = backend.GetType("StartupExtensions", throwOnError: true)!;
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
foreach (var method in new[] { "ConfigureLogging", "ConfigureDatabase", "ConfigureAuthentication", "ConfigureCors", "ConfigureOptions", "ConfigureDependencyInjection", "ConfigureApiDocumentation" })
    startup.GetMethod(method)!.Invoke(null, [builder]);
builder.Logging.ClearProviders();
builder.Host.UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; });
await using var app = builder.Build();
if (args.Contains("smoke-academic") || args.Contains("smoke-createfull"))
{
    var fullOnly = args.Contains("smoke-createfull");
    await AcademicFollowup.RunAsync(app.Services, Path.Combine(output, fullOnly ? "createfull-followup" : "academic-followup"), fullOnly);
    return;
}
startup.GetMethod("ConfigurePipeline")!.Invoke(null, [app]);
var assertions = 0;
void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
    assertions++;
}
var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
    .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null).ToArray();
var routes = endpoints.SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
    .Select(verb => new
    {
        verb, route = e.RoutePattern.RawText!,
        controller = e.Metadata.GetMetadata<ControllerActionDescriptor>()!.ControllerTypeInfo.FullName,
        action = e.Metadata.GetMetadata<ControllerActionDescriptor>()!.ActionName
    })).ToArray();
var duplicates = routes.GroupBy(e => e.verb + " " + Regex.Replace(e.route.ToLowerInvariant(), @"\{[^}:]+", "{parameter"))
    .Where(g => g.Count() > 1).ToArray();
Check(duplicates.Length == 0, "Duplicate MVC routes: " + string.Join(",", duplicates.Select(g => g.Key)));
var unified = routes.Where(r => r.controller!.Contains(".Controllers.Unified.")).ToArray();
Check(unified.Select(r => r.controller).Distinct().Count() == 51, "51 active Unified controllers after Articles cutover");
Check(unified.Length == 297, "244 existing routes plus auth/me and 52 Articles routes");
Check(routes.Count(r => r.route == "api/auth/me" && r.verb == "GET") == 1, "auth/me published exactly once");
var replaced = backend.GetTypes().Where(t => t.Namespace == typeof(UnifiedProjectsController).Namespace && !t.IsAbstract && t.Name.EndsWith("Controller")
    && t != typeof(UnifiedCatalogSynchronizationController) && t != typeof(UnifiedFacultiesController)).ToArray();
foreach (var type in replaced)
    Check(!routes.Any(r => r.controller!.EndsWith("." + type.Name[7..])), "Legacy controller inactive: " + type.Name[7..]);
await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<UnifiedDideDbContext>();
    Check(db.Database.GetDbConnection().Database == "tesis_unified", "Expected local runtime database");
    Check(!ReferenceEquals(db, sp.GetRequiredService<AppDbContext>()), "Separate legacy context retained");
    foreach (var storeType in new[] { typeof(IUserStore<IdentityUser<int>>), typeof(IRoleStore<IdentityRole<int>>) })
    {
        Check(builder.Services.Count(d => d.ServiceType == storeType) == 1, "One Identity store registration");
        var store = sp.GetRequiredService(storeType);
        Check(ReferenceEquals(store.GetType().GetProperty("Context")!.GetValue(store), db), "Identity store shares Unified context");
    }
    sp.GetRequiredService<UserManager<IdentityUser<int>>>();
    sp.GetRequiredService<RoleManager<IdentityRole<int>>>();
    sp.GetRequiredService<SignInManager<IdentityUser<int>>>();
    foreach (var type in unified.Select(r => backend.GetType(r.controller!)!).Distinct())
        Check(sp.GetRequiredService(type) is not null, "Resolve actual runtime controller " + type.Name);
    var uow = sp.GetRequiredService<IUnifiedUnitOfWork>();
    foreach (var property in typeof(IUnifiedUnitOfWork).GetProperties())
    {
        var repository = property.GetValue(uow)!;
        Check(ReferenceEquals(repository, sp.GetRequiredService(property.PropertyType)), "Scoped repository " + property.Name);
        var contexts = new List<DbContext>();
        for (var type = repository.GetType(); type is not null; type = type.BaseType)
            contexts.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(f => typeof(DbContext).IsAssignableFrom(f.FieldType)).Select(f => (DbContext)f.GetValue(repository)!));
        Check(contexts.Count > 0 && contexts.All(c => ReferenceEquals(c, db)), "Repository uses only scoped Unified context: " + property.Name);
    }
    foreach (var descriptor in builder.Services.Where(d => d.ServiceType.Namespace?.Contains(".Unified") == true))
    {
        Check(builder.Services.Count(d => d.ServiceType == descriptor.ServiceType) == 1, "Unique Unified registration " + descriptor.ServiceType.Name);
        if (descriptor.ImplementationType is { } implementation)
        {
            foreach (var dependency in implementation.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType))
            {
                var name = dependency.FullName ?? dependency.Name;
                Check(dependency != typeof(AppDbContext) && !name.Contains(".Data.Articles.ArticlesDbContext") &&
                    !name.Contains(".UnitOfWork.Interfaces.") && !name.Contains(".Repositories.Interfaces."),
                    "No legacy persistence in " + implementation.Name);
                if (implementation.Namespace?.Contains(".Services.Unified.") == true)
                    Check(!name.EndsWith(".IExternalAcademicsService") && !name.EndsWith(".IExternalPeriodsClient"),
                        "No academic HTTP clients in business service " + implementation.Name);
            }
        }
    }
}
await File.WriteAllTextAsync(Path.Combine(output, "mvc-endpoints.json"), JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PREFLIGHT PASS: {assertions} checks; {unified.Length} Unified endpoints / 51 controllers; legacy counterparts inactive; {routes.Length} total MVC endpoints; duplicates=0; Identity managers/stores Unified; all runtime controllers resolved.");
if (args.Contains("smoke") || args.Contains("smoke-local"))
    await HttpSmoke.RunAsync(app.Services, output, skipExternal: args.Contains("smoke-local"));
