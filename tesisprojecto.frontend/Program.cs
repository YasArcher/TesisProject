using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;
using tesisproject.frontend;
using Blazored.LocalStorage;

// ?? 1) Crear el builder
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBlazoredLocalStorage();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ?? 2) Cargar appsettings.json (+ appsettings.{Environment}.json)
var bootHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

// appsettings.json
using (var s = await bootHttp.GetStreamAsync("appsettings.json"))
{
    builder.Configuration.AddJsonStream(s);
}

// appsettings.{Environment}.json (opcional)
var envFile = $"appsettings.{builder.HostEnvironment.Environment}.json";
try
{
    using var s2 = await bootHttp.GetStreamAsync(envFile);
    builder.Configuration.AddJsonStream(s2);
}
catch
{
    // Ignora si no existe
}

// ?? 3) Leer ApiBaseUrl y registrar HttpClient del backend
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// HttpClient por defecto apuntando a tu backend
builder.Services.AddScoped(sp =>
{
    var http = new HttpClient { BaseAddress = new Uri(apiBase) };
    http.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    return http;
});

// Si vas a usar un wrapper / handler JWT más adelante, aquí los registras:
// builder.Services.AddScoped<AuthMessageHandler>();
// builder.Services.AddHttpClient("Backend", c => { c.BaseAddress = new Uri(apiBase); })
//       .AddHttpMessageHandler<AuthMessageHandler>();
// builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));
// builder.Services.AddScoped<IApiClient, ApiClient>();

await builder.Build().RunAsync();
