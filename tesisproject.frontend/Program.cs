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
using tesisproject.frontend.SharedUI.Modal;

// 1) Builder
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 2) Servicios base
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();

// Componentes de UI de librería
builder.Services.AddBlazoredToast();
builder.Services.AddScoped<IModalService, ModalService>();

// 3) Cargar appsettings.json (+ Environment)
var bootHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
using (var s = await bootHttp.GetStreamAsync("appsettings.json"))
    builder.Configuration.AddJsonStream(s);

var envFile = $"appsettings.{builder.HostEnvironment.Environment}.json";
try
{
    using var s2 = await bootHttp.GetStreamAsync(envFile);
    builder.Configuration.AddJsonStream(s2);
}
catch { /* Luego veo que pongo :v */ }

// 4) BaseAddress del backend
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// 5) Auth + HttpClient con token
builder.Services.AddScoped<ITokenStore, LocalTokenStore>();
builder.Services.AddScoped<AuthMessageHandler>();

builder.Services.AddHttpClient("Backend", c =>
{
    c.BaseAddress = new Uri(apiBase);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));

// 6) Wrapper API
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IProjectClientService, ProjectClientService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IBudgetClientService, BudgetClientService>();
builder.Services.AddScoped<IProjectFiltersClientService, ProjectFiltersClientService>();
builder.Services.AddScoped<IProjectExtensionClientService, ProjectExtensionClientService>();
builder.Services.AddScoped<IVisitClientService, VisitClientService>();
builder.Services.AddScoped<IAuthClientService, AuthClientService>();
builder.Services.AddScoped<IDocumentRecognitionClientService, DocumentRecognitionClientService>();
builder.Services.AddScoped<IProjectTypeClientService, ProjectTypeClientService>();
builder.Services.AddScoped<IMemberRoleTypeClientService, MemberRoleTypeClientService>();
builder.Services.AddScoped<IDocumentTypeClientService, DocumentTypeClientService>();
builder.Services.AddScoped<IObjectiveTypeClientService, ObjectiveTypeClientService>();
builder.Services.AddScoped<IDocumentClientService, DocumentClientService>();
builder.Services.AddScoped<IResearchCategoryClientService, ResearchCategoryClientService>();
builder.Services.AddScoped<IResearchCategoryTypeClientService, ResearchCategoryTypeClientService>();
builder.Services.AddScoped<IFundingTypeClientService, FundingTypeClientService>();
builder.Services.AddScoped<IResearchCategoryGroupClientService, ResearchCategoryGroupClientService>();
builder.Services.AddScoped<IVisitIssueClientService, VisitIssueClientService>();
builder.Services.AddScoped<ITransactionTypeClientService, TransactionTypeClientService>();

// External
builder.Services.AddScoped<IExternalAcademicsClientService, ExternalAcademicsClientService>();
builder.Services.AddScoped<IExternalUserService, ExternalUserClientService>();

// 7) Provider de autenticación
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();