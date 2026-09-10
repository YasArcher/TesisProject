using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Responses;

internal static class UnifiedControllerTests
{
    private static readonly Assembly Backend = typeof(UnifiedProjectsController).Assembly;
    private static readonly Type[] Controllers = Backend.GetTypes().Where(t => t.Namespace == typeof(UnifiedProjectsController).Namespace && !t.IsAbstract && t.Name.EndsWith("Controller") && t != typeof(UnifiedCatalogSynchronizationController) && t != typeof(UnifiedFacultiesController)).OrderBy(t => t.Name).ToArray();
    private static readonly Dictionary<string, string[]> Validation = new() { ["identity"] = ["conflicting mapping"] };

    public static async Task RunAsync(Action<bool, string> check)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExternalApis:BaseUrl"] = "https://example.invalid/",
            ["Storage:RootPath"] = Path.GetFullPath("artifacts/unified-controllers/storage")
        }).Build();
        var services = new ServiceCollection();
        services.AddUnifiedDide(configuration, o => o.UseInMemoryDatabase("composition-only"));
        var owned = services.Where(d => d.ServiceType.Namespace?.Contains(".Unified") == true || d.ServiceType == typeof(UnifiedDideDbContext)).ToArray();
        foreach (var group in owned.GroupBy(d => d.ServiceType))
        {
            check(group.Count() == 1, "Single registration: " + group.Key);
            check(group.Single().Lifetime == (group.Key == typeof(IUnifiedAcademicCatalogSnapshotClient) ? ServiceLifetime.Transient : ServiceLifetime.Scoped), "Scoped: " + group.Key);
        }
        check(!services.Any(d => d.ServiceType.FullName == "tesisproject.backend.Data.AppDbContext" || d.ServiceType.FullName == "tesisproject.backend.UnitOfWork.Interfaces.IUnitOfWork"), "No operational legacy context/UoW");
        await using (var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
        {
            await using var scope = provider.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            foreach (var d in owned.Where(d => !d.ServiceType.ContainsGenericParameters))
                check(sp.GetRequiredService(d.ServiceType) is not null, "Resolve " + d.ServiceType.Name);
            var context = sp.GetRequiredService<UnifiedDideDbContext>();
            check(ReferenceEquals(context, sp.GetRequiredService<UnifiedDideDbContext>()), "One context per scope");
            var uow = sp.GetRequiredService<IUnifiedUnitOfWork>();
            var repos = typeof(IUnifiedUnitOfWork).GetProperties();
            check(repos.Length == 58, "58 Unified repository slots including Articles");
            foreach (var p in repos)
            {
                check(ReferenceEquals(p.GetValue(uow), sp.GetRequiredService(p.PropertyType)), "Repository scoped instance " + p.Name);
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(tesisproject.backend.Repositories.Unified.Interfaces.IUnifiedCatalogRepository<>))
                    check(sp.GetRequiredService(typeof(IUnifiedCatalogCrudService<>).MakeGenericType(p.PropertyType.GenericTypeArguments)) is not null,
                        "Resolve closed catalog service " + p.Name);
            }
            var store = sp.GetRequiredService<IUserStore<IdentityUser<int>>>();
            check(store.GetType().GetGenericArguments().Contains(typeof(UnifiedDideDbContext)), "Identity EF store uses Unified context");
            var storeContext = store.GetType().GetProperty("Context")!.GetValue(store);
            check(ReferenceEquals(storeContext, context), "Identity and UoW share scoped context");
            sp.GetRequiredService<UserManager<IdentityUser<int>>>();
            sp.GetRequiredService<RoleManager<IdentityRole<int>>>();
            sp.GetRequiredService<SignInManager<IdentityUser<int>>>();
            await uow.DisposeAsync();
            check(context.ChangeTracker.Entries().Count() == 0, "UoW disposal leaves DI-owned context usable");
            await using var other = provider.CreateAsyncScope();
            check(!ReferenceEquals(context, other.ServiceProvider.GetRequiredService<UnifiedDideDbContext>()), "Distinct context in second scope");
        }
        var duplicateRejected = false;
        try { services.AddUnifiedDide(configuration, _ => { }); }
        catch (InvalidOperationException) { duplicateRejected = true; }
        check(duplicateRejected, "Reject duplicate composition");
        var mixed = new ServiceCollection();
        mixed.AddIdentityCore<IdentityUser<int>>().AddEntityFrameworkStores<tesisproject.backend.Data.AppDbContext>();
        var mixedRejected = false;
        try { mixed.AddUnifiedDide(configuration, _ => { }); }
        catch (InvalidOperationException) { mixedRejected = true; }
        check(mixedRejected, "Reject legacy Identity store mix");

        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new AssemblyPart(Backend));
        manager.FeatureProviders.Add(new ControllerFeatureProvider());
        var feature = new ControllerFeature();
        manager.PopulateFeature(feature);
        check(Controllers.All(t => feature.Controllers.Any(c => c.AsType() == t)), "Default MVC publishes all Unified replacements");
        check(!feature.Controllers.Any(t => t.Name == "ProjectsController"), "Legacy Projects discovery is inactive");

        var rows = new List<object>();
        var exercised = 0;
        using var cancellation = new CancellationTokenSource();
        foreach (var unified in Controllers)
        {
            var legacyName = unified.Name[7..];
            check(!unified.IsDefined(typeof(NonControllerAttribute)) && !Backend.GetTypes().Any(t => t.Name == legacyName), "Unified controller only " + unified.Name);
            check(ControllerContractSnapshot.Matches(unified), "Pre-cleanup HTTP/DTO/auth/binding snapshot " + unified.Name);
            foreach (var action in Actions(unified))
            {
                var oldRoute = Route(unified);
                foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>())
                    rows.Add(new { unified = unified.Name, endpoint = action.Name, route = oldRoute, verb = string.Join(",", http.HttpMethods), metadata = Attrs(action) });
                // These legacy endpoints explicitly reject writes to the external periods catalog.
                if (legacyName == "AcademicPeriodsController" && action.Name is "Create" or "Update") continue;
                foreach (var error in new[] { ErrorType.None, ErrorType.Validation, ErrorType.NotFound, ErrorType.Conflict, ErrorType.Unauthorized, ErrorType.Forbidden })
                {
                    var calls = new List<(MethodInfo Method, object?[] Args)>();
                    object? Handler(MethodInfo m, object?[] args)
                    {
                        calls.Add((m, args));
                        foreach (var token in args.OfType<CancellationToken>())
                            if (action.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)) || legacyName == "ResearchCategoryTypeController")
                                check(token == cancellation.Token, "CT " + unified.Name + "." + action.Name);
                        if (m.Name == "get_Value" && m.ReturnType == typeof(tesisproject.backend.Options.ArticlesModuleOptions)) return new tesisproject.backend.Options.ArticlesModuleOptions { Enabled = true };
                        return ServiceReturn(m.ReturnType, error);
                    }
                    var ctor = unified.GetConstructors().Single();
                    var deps = ctor.GetParameters().Select(p => typeof(Stub).GetMethod(nameof(Stub.For))!.MakeGenericMethod(p.ParameterType).Invoke(null, [ (Func<MethodInfo, object?[], object?>)Handler ])).ToArray();
                    var controller = ctor.Invoke(deps);
                    if (controller is ControllerBase cb)
                        cb.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token, User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "17")], "test")) } };
                    var args = action.GetParameters().Select(p => p.ParameterType == typeof(CancellationToken) ? (object)cancellation.Token : Sample(p.ParameterType)).ToArray();
                    var task = (Task)action.Invoke(controller, args)!;
                    await task;
                    var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
                    if (result.GetType().IsGenericType && result.GetType().GetGenericTypeDefinition() == typeof(ActionResult<>))
                        result = result.GetType().GetProperty("Result")!.GetValue(result)!;
                    check(calls.Count > 0, "Service reached " + unified.Name + "." + action.Name);
                    check(calls.All(c => c.Method.DeclaringType!.Namespace!.Contains(".Unified") || c.Method.DeclaringType.Name.StartsWith("IExternal") || c.Method.Name == "get_Value" || c.Method.DeclaringType.Name.StartsWith("IArticle") || c.Method.DeclaringType.Name == "IRegistrationMatrixService"), "Unified service target " + unified.Name);
                    var expected = error switch { ErrorType.Validation => 400, ErrorType.NotFound => 404, ErrorType.Conflict => 409, ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, _ => legacyName == "ExportTemplatesController" && action.Name == "CreateTemplate" ? 201 : 200 };
                    var status = result is ObjectResult obj ? obj.StatusCode ?? 200 : result is FileResult ? 200 : -1;
                    check(status == expected, $"HTTP {unified.Name}.{action.Name} {error}: {status}/{expected}");
                    if (error != ErrorType.None && result is ObjectResult failure)
                    {
                        var value = failure.Value!;
                        check((string?)value.GetType().GetProperty("ErrorCode")!.GetValue(value) == "IDENTITY_MAPPING_CONFLICT", "ErrorCode preserved");
                        check(ReferenceEquals(value.GetType().GetProperty("ValidationErrors")!.GetValue(value), Validation), "ValidationErrors preserved");
                        check((ErrorType)value.GetType().GetProperty("Error")!.GetValue(value)! == error, "ErrorType preserved");
                        check((string?)value.GetType().GetProperty("Message")!.GetValue(value) == "mapping failure", "Message preserved");
                    }
                    exercised++;
                }
            }
        }
        Directory.CreateDirectory("artifacts/unified-controllers");
        await File.WriteAllTextAsync("artifacts/unified-controllers/compiled-contracts.json", JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Unified HTTP/DI: {Controllers.Length} controllers, {rows.Count} endpoints compared, {exercised} HTTP scenarios; all scoped registrations resolved, no SQL connection.");
    }

    private static IEnumerable<MethodInfo> Actions(Type t) => t.GetMethods().Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
    private static string Friendly(Type t) => t.IsGenericType ? t.Name.Split('`')[0] + "<" + string.Join(",", t.GetGenericArguments().Select(Friendly)) + ">" : t.Name;
    private static string Route(Type t) => t.GetCustomAttributes<RouteAttribute>(false).Select(a => a.Template.Replace("[controller]", t.Name.Replace("Controller", "").ToLowerInvariant())).Aggregate((a,b) => a + "|" + b);
    private static string Authorization(MemberInfo m) => string.Join(";", m.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Select(a => $"roles={a.Roles},policy={a.Policy},schemes={a.AuthenticationSchemes}")) + (m.IsDefined(typeof(AllowAnonymousAttribute), true) ? ";anonymous" : "");
    private static string Parameters(MethodInfo m) => string.Join(";", m.GetParameters().Select(p => $"{Friendly(p.ParameterType)} {p.Name} default={p.DefaultValue} {string.Join(" ", p.GetCustomAttributesData().Where(a => a.AttributeType.Namespace?.StartsWith("Microsoft.AspNetCore.Mvc") == true))}"));
    private static string Attrs(MemberInfo m) => string.Join(";", m.GetCustomAttributes(true).Where(a => a is ApiControllerAttribute or AuthorizeAttribute or AllowAnonymousAttribute or ProducesResponseTypeAttribute or ProducesAttribute or ConsumesAttribute or HttpMethodAttribute).Select(a => a.GetType().Name + ":" + string.Join(",", a.GetType().GetProperties().Where(p => p.Name != "TypeId" && p.GetIndexParameters().Length == 0).Select(p => p.Name + "=" + (p.GetValue(a) is System.Collections.IEnumerable list and not string ? string.Join("|", list.Cast<object>()) : p.GetValue(a))))).OrderBy(x => x));

    private static object? Sample(Type t)
    {
        if (t == typeof(ValueTuple<Stream, string, string>)) return ((Stream)new MemoryStream([1, 2]), "application/octet-stream", "sample.bin");
        if (t == typeof(string)) return "sample";
        if (t == typeof(int)) return 1;
        if (t == typeof(bool)) return true;
        if (t == typeof(byte[])) return new byte[] { 1, 2 };
        if (t == typeof(Stream)) return new MemoryStream([1, 2]);
        if (t == typeof(IFormFile)) return new FormFile(new MemoryStream([1, 2]), 0, 2, "file", "sample.xlsx") { Headers = new HeaderDictionary(), ContentType = "application/octet-stream" };
        if (Nullable.GetUnderlyingType(t) is not null) return null;
        if (t.IsGenericType && t.GetGenericTypeDefinition().FullName!.StartsWith("System.ValueTuple")) return Activator.CreateInstance(t, t.GetGenericArguments().Select(Sample).ToArray());
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ServiceResult<>)) return Result(t, ErrorType.None);
        if (t.IsGenericType && t.IsInterface && t.GetGenericArguments().Length == 1) return Activator.CreateInstance(typeof(List<>).MakeGenericType(t.GetGenericArguments()));
        return Activator.CreateInstance(t);
    }
    private static object Result(Type t, ErrorType error)
    {
        var value = Activator.CreateInstance(t)!;
        t.GetProperty("Success")!.SetValue(value, error == ErrorType.None);
        t.GetProperty("Error")!.SetValue(value, error);
        t.GetProperty("Message")!.SetValue(value, error == ErrorType.None ? "ok" : "mapping failure");
        if (error == ErrorType.None) t.GetProperty("Data")!.SetValue(value, Sample(t.GetGenericArguments()[0]));
        else
        {
            t.GetProperty("ErrorCode")!.SetValue(value, "IDENTITY_MAPPING_CONFLICT");
            t.GetProperty("ValidationErrors")!.SetValue(value, Validation);
        }
        return value;
    }
    private static object? ServiceReturn(Type t, ErrorType error)
    {
        if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Task<>)) return Sample(t);
        var inner = t.GetGenericArguments()[0];
        object? value;
        if (inner.GetGenericTypeDefinition() == typeof(ServiceResult<>)) value = Result(inner, error);
        else // Auth ServiceResult plus optional refresh cookie.
            value = Activator.CreateInstance(inner, Result(inner.GetGenericArguments()[0], error), null);
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, [value]);
    }
}
