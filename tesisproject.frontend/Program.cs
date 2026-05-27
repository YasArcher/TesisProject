using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using tesisproject.frontend;
using tesisproject.frontend.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAppCoreServices();

var bootHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
try
{
    using var stream = await bootHttp.GetStreamAsync("appsettings.json");
    builder.Configuration.AddJsonStream(stream);
}
catch
{
}

try
{
    using var environmentStream = await bootHttp.GetStreamAsync($"appsettings.{builder.HostEnvironment.Environment}.json");
    builder.Configuration.AddJsonStream(environmentStream);
}
catch
{
}

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5040";
if (Uri.TryCreate(apiBase, UriKind.Relative, out _))
{
    apiBase = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBase).ToString();
}
builder.Services.AddAppApiClients(apiBase);

await builder.Build().RunAsync();
