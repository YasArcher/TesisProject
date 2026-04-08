using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using tesisproject.frontend.Services.Auth;
using tesisproject.frontend.Services.Implementations;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Auth;
using tesisproject.frontend.Utils;

namespace tesisproject.frontend.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppCoreServices(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddAuthorizationCore();
        services.AddBlazoredLocalStorage();
        services.AddBlazoredToast();
        services.AddScoped<ITokenStore, LocalTokenStore>();
        services.AddScoped<IAuthClient, AuthClient>();
        services.AddScoped<IIdentityAdministrationClient, IdentityAdministrationClient>();
        services.AddTransient<AuthMessageHandler>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<JwtAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthStateProvider>());

        return services;
    }

    public static IServiceCollection AddAppApiClients(this IServiceCollection services, string apiBase)
    {
        services.AddHttpClient("Backend", c =>
        {
            c.BaseAddress = new Uri(apiBase);
            c.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<AuthMessageHandler>();

        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));
        services.AddScoped<IArticlesClient, ArticlesClient>();
        services.AddScoped<IVenuesClient, VenuesClient>();
        services.AddScoped<ICatalogsService, CatalogsService>();
        services.AddScoped<IFormConfigurationClient, FormConfigurationClient>();
        services.AddScoped<IBulkImportClient, BulkImportClient>();
        services.AddScoped<IWorkflowClient, WorkflowClient>();
        services.AddScoped<IRegistrationMatrixClient, RegistrationMatrixClient>();
        services.AddScoped<IExternalApiExplorerClient, ExternalApiExplorerClient>();
        services.AddScoped<ExportJsInterop>();
        services.AddScoped<IApiClient, ApiClient>();

        return services;
    }
}
