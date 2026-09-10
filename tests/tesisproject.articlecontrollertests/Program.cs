using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tesisproject.backend.Authorization.Articles;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.Responses;

Type[] unified = [typeof(UnifiedArticlesController), typeof(UnifiedArticlesConfigurationController), typeof(UnifiedArticlesCatalogsController), typeof(UnifiedArticlesRegistrationMatricesController)];
var checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
MethodInfo[] Actions(Type type) => type.GetMethods().Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any()).ToArray();
var discovery = new ControllerFeature();
new ControllerFeatureProvider().PopulateFeature([new AssemblyPart(typeof(UnifiedArticlesController).Assembly)], discovery);
for (int i = 0; i < unified.Length; i++)
{
    Check(discovery.Controllers.Contains(unified[i].GetTypeInfo()) && !discovery.Controllers.Any(t => t.Name == unified[i].Name[7..]), "Only Unified Articles controllers are discoverable");
    Check(ControllerContractSnapshot.Matches(unified[i]), "Pre-cleanup DTO/routes/auth/binding snapshot: " + unified[i].Name);
}
var endpoints = unified.SelectMany(t => t.GetCustomAttributes<RouteAttribute>().SelectMany(route => Actions(t).SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods.Select(verb => verb + " " + route.Template + "/" + a.Template))))).ToList();
Check(endpoints.Distinct().Count() == endpoints.Count, "Duplicate active Articles endpoints");

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuthentication>("fixture", _ => { });
builder.Services.AddAuthorization(ArticlePolicies.Configure);
builder.Services.AddSingleton(Options.Create(new ArticlesModuleOptions { Enabled = true }));
builder.Services.AddSingleton(Proxy<IUnifiedArticleQueryService>());
builder.Services.AddSingleton(Proxy<IUnifiedArticleRegistrationCommandService>());
builder.Services.AddSingleton(Proxy<IUnifiedArticleConfigurationService>());
builder.Services.AddSingleton(Proxy<IUnifiedRegistrationMatrixService>());
builder.Services.AddSingleton(Proxy<IUnifiedArticleUserContext>());
builder.Services.AddControllers().ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new FixtureControllers(unified)));
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();

// Invoke every action against service doubles: exact result object, status and argument forwarding.
foreach (var type in unified)
{
    var controller = ActivatorUtilities.CreateInstance(app.Services, type);
    foreach (var action in Actions(type))
    foreach (var (error, status) in new (ErrorType?, int)[] { (null,200), (ErrorType.Validation,400), (ErrorType.NotFound,404), (ErrorType.Conflict,409), (ErrorType.Forbidden,403), (ErrorType.Unauthorized,401), (ErrorType.Unexpected,500) })
    {
        ServiceDouble.Error = error;
        var actionArgs = action.GetParameters().Select(p => p.ParameterType == typeof(string) ? (object)"fixture" : p.ParameterType == typeof(int) ? 17 : p.ParameterType == typeof(int?) ? 23 : Activator.CreateInstance(p.ParameterType)).ToArray();
        var task = (Task)action.Invoke(controller, actionArgs)!; await task;
        var actionResult = task.GetType().GetProperty("Result")!.GetValue(task)!;
        var http = (ObjectResult)actionResult.GetType().GetProperty("Result")!.GetValue(actionResult)!;
        Check(http.StatusCode == status && ReferenceEquals(http.Value, ServiceDouble.LastResult), "ServiceResult/status/validation passthrough: " + action.Name);
        Check(actionArgs.All(arg => ServiceDouble.LastArguments.Contains(arg)), "Binding argument lost: " + action.Name);
        if (type == typeof(UnifiedArticlesRegistrationMatricesController))
            Check(ServiceDouble.LastArguments.Contains("fixture-owner") && (action.Name == "CreateMatrix" || ServiceDouble.LastArguments.Contains(true)), "Matrix context forwarding");
    }
}
ServiceDouble.Error = null;
await app.StartAsync();
try
{
    using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    Check((int)(await client.GetAsync("/api/articles")).StatusCode == 401, "Anonymous must be challenged");
    client.DefaultRequestHeaders.Add("X-Role", "unprivileged");
    Check((int)(await client.GetAsync("/api/articles")).StatusCode == 403, "Listing policy denied");
    Check((int)(await client.PostAsync("/api/articles", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))).StatusCode == 403, "Registration policy denied");
    client.DefaultRequestHeaders.Remove("X-Role"); client.DefaultRequestHeaders.Add("X-Role", ArticleRoles.Analyst);
    foreach (var path in new[] { "/api/articles", "/api/articles/17", "/api/config/forms", "/api/scientific-production/config/forms", "/api/catalogs/admin/faculties", "/api/scientific-production/catalogs/admin/faculties", "/api/articles/registration-matrices" })
        Check((int)(await client.GetAsync(path)).StatusCode == 200, "HTTP route: " + path);
    ServiceDouble.Error = ErrorType.Validation;
    var failure = await client.GetAsync("/api/articles/17");
    var payload = await failure.Content.ReadAsStringAsync();
    Check((int)failure.StatusCode == 400 && payload.Contains("FIXTURE_ERROR") && payload.Contains("validationErrors") && payload.Contains("fixture validation"), "Serialized ServiceResult metadata");
    app.Services.GetRequiredService<IOptions<ArticlesModuleOptions>>().Value.Enabled = false;
    Check((int)(await client.GetAsync("/api/articles")).StatusCode == 503 && (int)(await client.GetAsync("/api/articles/registration-matrices")).StatusCode == 503, "Module disabled HTTP contract");
}
finally { await app.StopAsync(); }
Console.WriteLine($"PASS: {checks} Articles controller checks; all actions, HTTP policies/routes, ServiceResult, validation metadata, Unified-only discovery.");

static T Proxy<T>() where T : class => DispatchProxy.Create<T, ServiceDouble>();
public class ServiceDouble : DispatchProxy
{
    public static ErrorType? Error;
    public static object? LastResult;
    public static object?[] LastArguments = [];
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "get_OwnerReference") return "fixture-owner";
        LastArguments = args ?? [];
        var resultType = method.ReturnType.GenericTypeArguments.Single();
        var result = Activator.CreateInstance(resultType)!;
        resultType.GetProperty("Success")!.SetValue(result, Error is null);
        resultType.GetProperty("Error")!.SetValue(result, Error ?? ErrorType.None);
        resultType.GetProperty("Message")!.SetValue(result, "fixture message");
        resultType.GetProperty("ErrorCode")!.SetValue(result, Error is null ? null : "FIXTURE_ERROR");
        resultType.GetProperty("ValidationErrors")!.SetValue(result, Error is null ? null : new Dictionary<string,string[]> { ["Title"] = ["fixture validation"] });
        LastResult = result;
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [result]);
    }
}
sealed class FixtureControllers(Type[] controllers) : IApplicationFeatureProvider<ControllerFeature>
{
    // Test host only: production uses the same active Unified controller types.
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    { feature.Controllers.Clear(); foreach (var type in controllers) feature.Controllers.Add(type.GetTypeInfo()); }
}
sealed class FixtureAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
        !Request.Headers.TryGetValue("X-Role", out var role) ? AuthenticateResult.NoResult() : AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"fixture-owner"), new Claim(ClaimTypes.Role,role.ToString())], Scheme.Name)), Scheme.Name)));
}
