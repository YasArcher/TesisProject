using tesisproject.backend.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var backendUrl = builder.Configuration["BackendUrl"]
                 ?? Environment.GetEnvironmentVariable("TESIS_BACKEND_URL")
                 ?? "http://localhost:5040";

builder.WebHost.PreferHostingUrls(true)
               .UseUrls(backendUrl);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no está configurada.");

builder.Services
    .AddAppDataProtection(builder.Environment)
    .AddAppPersistence(builder.Configuration, builder.Environment, connectionString)
    .AddAppDomainServices(builder.Configuration)
    .AddAppCors()
    .AddAppIdentityAndAuth(builder.Configuration)
    .AddAppApi();

var app = builder.Build();

app.UseAppPipeline();
await app.ValidateConfiguredDatabaseAsync(connectionString);
app.MapAppEndpoints();
app.Run();
