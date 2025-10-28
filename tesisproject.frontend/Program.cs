using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;
using tesisproject.frontend;
using tesisproject.frontend.Services.Auth;
using tesisproject.frontend.Services.Implementations;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Utils;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

// Servicios de dominio
builder.Services.AddScoped<IArticlesClient, ArticlesClient>();
builder.Services.AddScoped<ICatalogsService, CatalogsService>();
builder.Services.AddScoped<ExportJsInterop>();

// appsettings desde wwwroot (opcional)
var bootHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
try { using var s = await bootHttp.GetStreamAsync("appsettings.json"); builder.Configuration.AddJsonStream(s); } catch { }
try { using var s2 = await bootHttp.GetStreamAsync($"appsettings.{builder.HostEnvironment.Environment}.json"); builder.Configuration.AddJsonStream(s2); } catch { }

// ===== API Base (BACKEND) =====
// Si no hay config, se usa http://localhost:5040
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5040";

// ===== Auth / DI =====
builder.Services.AddScoped<tesisproject.frontend.Services.Interfaces.ITokenStore,
                           tesisproject.frontend.Services.Auth.LocalTokenStore>();

builder.Services.AddScoped<tesisproject.frontend.Services.Interfaces.IAuthClient,
                           tesisproject.frontend.Services.Auth.AuthClient>();

builder.Services.AddTransient<tesisproject.frontend.Services.Auth.AuthMessageHandler>();

// AuthenticationStateProvider (JwtAuthStateProvider como implementación concreta)
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());

// HttpClient con handler que añade Authorization: Bearer (excepto login/register)
builder.Services.AddHttpClient("Backend", c =>
{
    c.BaseAddress = new Uri(apiBase); // http://localhost:5040
    c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<tesisproject.frontend.Services.Auth.AuthMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));
// Servicios de dominio
builder.Services.AddScoped<IArticlesClient, ArticlesClient>();
builder.Services.AddScoped<ICatalogsService, CatalogsService>();
builder.Services.AddScoped<ExportJsInterop>();
builder.Services.AddScoped<IApiClient, ApiClient>();
// Wrapper opcional
builder.Services.AddScoped<IApiClient, ApiClient>();

await builder.Build().RunAsync();
