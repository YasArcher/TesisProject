using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Authorization;
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

// ✅ CAMBIO: autorización global (por defecto TODO requiere usuario autenticado)
builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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
builder.Services.AddScoped<IProjectObjectiveClientService, ProjectObjectiveClientService>();
builder.Services.AddScoped<IExternalResearcherClientService, ExternalResearcherClientService>();
builder.Services.AddScoped<ICountryClientService, CountryClientService>();
builder.Services.AddScoped<IInstitutionClientService, InstitutionClientService>();
builder.Services.AddScoped<IExternalResearcherProjectClientService, ExternalResearcherProjectClientService>();
builder.Services.AddScoped<IConvocationClientService, ConvocationClientService>();
builder.Services.AddScoped<IAcademicPeriodClientService, AcademicPeriodClientService>();
builder.Services.AddScoped<IIndexingSourceClientService, IndexingSourceClientService>();
builder.Services.AddScoped<IProductClientService, ProductClientService>();
builder.Services.AddScoped<IProductTypeClientService, ProductTypeClientService>();
builder.Services.AddScoped<IProductTypeDesignClientService, ProductTypeDesignClientService>();
builder.Services.AddScoped<IProductAttributeClientService, ProductAttributeClientService>();
builder.Services.AddScoped<IProductAttributeDefinitionClientService, ProductAttributeDefinitionClientService>();
builder.Services.AddScoped<IProjectMatrixClientService, ProjectMatrixClientService>();
builder.Services.AddScoped<IExportTemplateClientService, ExportTemplateClientService>();
builder.Services.AddScoped<IObjectiveActivityClientService, ObjectiveActivityClientService>();
builder.Services.AddScoped<IProjectStateClientService, ProjectStateClientService>();
builder.Services.AddScoped<IExternalPeriodsClientService, ExternalPeriodsClientService>();
builder.Services.AddScoped<IVisitStateClientService, VisitStateClientService>();
builder.Services.AddScoped<IProjectOriginTypeClientService, ProjectOriginTypeClientService>();
builder.Services.AddScoped<IDwEtlClientService, DwEtlClientService>();
builder.Services.AddScoped<IProjectExtensionTypeClientService, ProjectExtensionTypeClientService>();
builder.Services.AddScoped<IVisitObjectiveActivityProgressClientService, VisitObjectiveActivityProgressClientService>();

// External
builder.Services.AddScoped<IExternalAcademicsClientService, ExternalAcademicsClientService>();
builder.Services.AddScoped<IExternalUserService, ExternalUserClientService>();

// 7) Provider de autenticación
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();
