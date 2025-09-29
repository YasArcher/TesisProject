using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;
using tesisproject.frontend;
// 👉 NUEVOS usings para Stores + Mocks
using tesisproject.frontend.Features.Management.State;       // DataEntryStore, DashboardStore, AIStore
using tesisproject.frontend.Services.Auth;
using tesisproject.frontend.Services.Implementations;        // ApiClient, etc.
using tesisproject.frontend.Services.Implementations.Mocks;  // DataEntryMockService, InsightsMockService, ...
using tesisproject.frontend.Services.Interfaces;             // IApiClient + interfaces de dominio

// 1) Builder
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<tesisproject.frontend.Services.Interfaces.IArticlesClient,
                           tesisproject.frontend.Services.Implementations.ArticlesClient>();
builder.Services.AddScoped<tesisproject.frontend.Utils.ExportJsInterop>();

// 2) Servicios base
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();

// UI libs
builder.Services.AddBlazoredToast();

// 3) Cargar appsettings.json (+ Environment)
// ⚠️ Asegúrate de que wwwroot/appsettings.json exista para evitar excepción aquí.
var bootHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
using (var s = await bootHttp.GetStreamAsync("appsettings.json"))
    builder.Configuration.AddJsonStream(s);

var envFile = $"appsettings.{builder.HostEnvironment.Environment}.json";
try
{
    using var s2 = await bootHttp.GetStreamAsync(envFile);
    builder.Configuration.AddJsonStream(s2);
}
catch { /* optional */ }

// 4) BaseAddress del backend
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// 5) Auth + HttpClient con token
builder.Services.AddScoped<ITokenStore, LocalTokenStore>();
builder.Services.AddScoped<AuthMessageHandler>();

// Si aún no tienes autenticación y solo quieres que compile/funcione:
builder.Services.AddHttpClient("Backend", c =>
{
    c.BaseAddress = new Uri(apiBase);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<AuthMessageHandler>();

// HttpClient por defecto para tus servicios
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));

// 6) Wrapper API
builder.Services.AddScoped<IApiClient, ApiClient>();

// 7) Provider de autenticación
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// 👉 8) STORES (estado por página) — necesarios para tus páginas de Management
builder.Services.AddScoped<DataEntryStore>();
builder.Services.AddScoped<DashboardStore>();
builder.Services.AddScoped<AIStore>();

// 👉 9) SERVICES (mocks ahora; luego los cambias por Http*)
builder.Services.AddScoped<IDataEntryService, DataEntryMockService>();
builder.Services.AddScoped<IInsightsService, InsightsMockService>();
builder.Services.AddScoped<IRecService, RecMockService>();
builder.Services.AddScoped<IPredictService, PredictMockService>();
builder.Services.AddScoped<tesisproject.frontend.Services.Interfaces.ICatalogsService,
                           tesisproject.frontend.Services.Implementations.CatalogsService>();


await builder.Build().RunAsync();
