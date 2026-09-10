using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;

internal static class RuntimeProbe
{
    public static async Task RunAsync()
    {
        var assembly = typeof(UnifiedArticlesController).Assembly;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { ContentRootPath = Path.GetFullPath("tesisproject.backend"), EnvironmentName = "Development", ApplicationName = assembly.GetName().Name });
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
        var startup = assembly.GetType("StartupExtensions", true)!;
        foreach (var method in new[] { "ConfigureLogging", "ConfigureDatabase", "ConfigureAuthentication", "ConfigureCors", "ConfigureOptions", "ConfigureDependencyInjection", "ConfigureApiDocumentation" })
            startup.GetMethod(method)!.Invoke(null, [builder]);
        builder.Logging.ClearProviders();
        builder.Host.UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        startup.GetMethod("ConfigurePipeline")!.Invoke(null, [app]);
        var output = Path.GetFullPath("artifacts/articles-runtime"); Directory.CreateDirectory(output);
        var results = new List<object>();
        var allRoutes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .SelectMany(e => e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Select(verb => new
            { verb, route = e.RoutePattern.RawText, controller = e.Metadata.GetMetadata<ControllerActionDescriptor>()!.ControllerTypeInfo.FullName })).ToArray();
        if (allRoutes.GroupBy(r => (r.verb, r.route)).Any(g => g.Count() > 1)) throw new Exception("Duplicate runtime routes");
        Console.WriteLine($"PASS runtime: {allRoutes.Select(r => r.controller).Distinct().Count()} controllers, {allRoutes.Length} endpoints, zero duplicate routes");
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName.Contains("Articles") == true)
            .SelectMany(e => e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Select(verb => new
            { verb, route = e.RoutePattern.RawText, controller = e.Metadata.GetMetadata<ControllerActionDescriptor>()!.ControllerTypeInfo.FullName })).ToArray();
        if (routes.Any(r => !r.controller!.Contains(".Unified.")) || routes.GroupBy(r => (r.verb, r.route)).Any(g => g.Count() > 1)
            || routes.Select(r => r.controller).Distinct().Count() != 4) throw new Exception("Articles runtime route invariant failed");
        await File.WriteAllTextAsync(Path.Combine(output,"routes.json"), JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS runtime: four Unified Articles controllers, {routes.Length} routes, no duplicate Articles routes");
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<UnifiedDideDbContext>();
        foreach (var type in routes.Select(r => assembly.GetType(r.controller!)!).Distinct()) sp.GetRequiredService(type);
        var uow = sp.GetRequiredService<IUnifiedUnitOfWork>();
        foreach (var property in typeof(IUnifiedUnitOfWork).GetProperties())
        {
            var repository = property.GetValue(uow)!;
            var contexts = new List<DbContext>();
            for (var t = repository.GetType(); t is not null; t = t.BaseType)
                contexts.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(f => typeof(DbContext).IsAssignableFrom(f.FieldType)).Select(f => (DbContext)f.GetValue(repository)!));
            if (contexts.Count == 0 || contexts.Any(c => !ReferenceEquals(c, db))) throw new Exception("Invalid repository context: " + property.Name);
        }
        var store = sp.GetRequiredService<IUserStore<IdentityUser<int>>>();
        if (!ReferenceEquals(store.GetType().GetProperty("Context")!.GetValue(store), db)) throw new Exception("Identity not Unified");
        Console.WriteLine("PASS runtime: actual startup DI ValidateOnBuild/ValidateScopes; scoped repositories/UoW/Identity use Unified");
        var tag = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var password = "Smoke!" + Guid.NewGuid().ToString("N");
        var email = $"articles-cutover-{tag}@example.test";
        var registration = await sp.GetRequiredService<IUnifiedIdentityProvisioningService>().EnsureAsync(new RegisterRequest
        { AspUserId = Random.Shared.Next(100000000, 200000000), Username = "articles-cutover-" + tag, Email = email, Password = password });
        if (!registration.Success) throw new Exception("Smoke user provisioning failed: " + registration.ErrorCode);
        var users = sp.GetRequiredService<UserManager<IdentityUser<int>>>();
        var fixture = await users.FindByEmailAsync(email) ?? throw new Exception("Missing smoke user");
        if (!(await users.AddToRoleAsync(fixture, "superadmin")).Succeeded) throw new Exception("Smoke role assignment failed");
        await File.WriteAllTextAsync(Path.Combine(output,"fixture.json"), JsonSerializer.Serialize(new { fixture.Id, Email = email, AppUserId = registration.Data, Purpose = "Articles runtime smoke; retained, no legacy cleanup" }));
        await app.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
            async Task<JsonElement> Call(string name, HttpMethod method, string path, object? body = null, int status = 200)
            {
                using var request = new HttpRequestMessage(method,path);
                if (body is not null) request.Content = JsonContent.Create(body);
                using var response = await http.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                var json = string.IsNullOrWhiteSpace(text) ? JsonSerializer.SerializeToElement(new { }) : JsonDocument.Parse(text).RootElement.Clone();
                var code = json.TryGetProperty("errorCode",out var c) ? c.GetString() : null;
                results.Add(new { name, path, status = (int)response.StatusCode, expected = status, code });
                await File.WriteAllTextAsync(Path.Combine(output,"http.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
                if ((int)response.StatusCode != status) throw new Exception(name + " returned " + response.StatusCode + " " + code);
                Console.WriteLine($"PASS HTTP {name}: {(int)response.StatusCode} {code}"); return json;
            }
            await Call("Anonymous me",HttpMethod.Get,"/api/auth/me",status:401);
            var login = await Call("Login",HttpMethod.Post,"/api/auth/login",new LoginRequest { Email=email, Password=password });
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",login.GetProperty("data").GetProperty("accessToken").GetString());
            await Call("Me",HttpMethod.Get,"/api/auth/me");
            await Call("Projects list",HttpMethod.Get,"/api/projects");
            await Call("Forms",HttpMethod.Get,"/api/config/forms?entityName=Article");
            await Call("Catalogs",HttpMethod.Get,"/api/catalogs/admin/faculties");
            await Call("Resolved form missing",HttpMethod.Get,"/api/config/forms/resolved-active?entityName=Article&preferredFormKey=ArticleForm",status:404);
            await Call("Article list",HttpMethod.Get,"/api/articles");
            await Call("Article detail absent",HttpMethod.Get,"/api/articles/2147483647",status:404);
            await Call("Matrix list",HttpMethod.Get,"/api/articles/registration-matrices");
        }
        finally { await app.StopAsync(); }
    }
}
