using tesisproject.backend.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.WebHost.PreferHostingUrls(true)
               .UseUrls("http://localhost:5040");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Server=PERSONAL\\DINNOVA;Database=TesisDB_Extensible;User Id=sa;Password=admin123;Encrypt=False;TrustServerCertificate=True";

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
