using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Authorization.Articles;
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Analytic.Implementations;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Auth.Articles;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)
        .AddEnvironmentVariables().AddCommandLine(args);

// Deployment job: only Unified, without HTTP, Identity or legacy/DW bootstrap.
if (args.Contains("--migrate-unified", StringComparer.Ordinal))
{
    await UnifiedDatabaseDeployment.MigrateAsync(builder.Configuration);
    return;
}

// ===== Configure services =====
builder.ConfigureLogging();
builder.ConfigureDatabase();
builder.ConfigureAuthentication();
builder.ConfigureCors();
builder.ConfigureOptions();
builder.ConfigureDependencyInjection();
builder.ConfigureApiDocumentation();

ExcelPackage.License.SetNonCommercialOrganization("Universidad T�cnica de Ambato");

var app = builder.Build();

// 1) Migraciones primero (para que existan tablas, incluyendo Identity)
if (app.Configuration.GetValue("DatabaseBootstrap:ApplyMigrations", true))
{
    await ApplyMigrationsAsync(app);
}
else
{
    app.Logger.LogWarning("Database migrations were skipped by configuration.");
}

// 2) Seed de roles despu�s de migrar
var identityRoles = new List<string> { "admin", "financial", "technical", "superadmin", "coordinador", "user" };
if (app.Configuration.GetValue<bool>($"{ArticlesModuleOptions.SectionName}:Enabled"))
    identityRoles.AddRange(ArticleRoles.All);
await EnsureIdentityRolesAsync(app, identityRoles.ToArray());
await tesisproject.backend.Bootstrap.UnifiedSuperadminBootstrap.RunAsync(app.Services, app.Configuration);

// ===== Configure pipeline =====
app.ConfigurePipeline();

app.Run();


// ============================================================================
// ============================   BOOTSTRAP TASKS   ===========================
// ============================================================================

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Migrations");

    // AppDbContext
    var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    logger.LogInformation("Applying migrations for AppDbContext...");
    await appDb.Database.MigrateAsync();

    // DwContext
    var dwDb = scope.ServiceProvider.GetRequiredService<DwContext>();
    logger.LogInformation("Applying migrations for DwContext...");
    await dwDb.Database.MigrateAsync();

    logger.LogInformation("Migrations applied successfully.");
}

static async Task EnsureIdentityRolesAsync(WebApplication app, params string[] roles)
{
    if (roles is null || roles.Length == 0)
        return;

    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(role))
            continue;

        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<int>(role));
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Failed to create role '{role}'. Errors: {errors}");
            }
        }
    }
}


// ============================================================================
// ================      EXTENSION METHODS FOR STARTUP      ===================
// ============================================================================

static class StartupExtensions
{
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddConsole();

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.Services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                        HttpLoggingFields.ResponsePropertiesAndHeaders;
                options.RequestBodyLogLimit = 4096;
                options.ResponseBodyLogLimit = 4096;
            });
        }
    }

    public static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing");

        // Retained exclusively for the existing DwEtlService operational source; Projects/Articles use Unified.
        // Projects and Identity use only the separate Unified composition below.
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(defaultConnection));

        var unifiedConnection = builder.Configuration.GetConnectionString("UnifiedDideConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:UnifiedDideConnection missing");
        builder.Services.AddUnifiedDide(builder.Configuration, options =>
            options.UseSqlServer(unifiedConnection, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));

        builder.Services.AddDbContext<DwContext>(options =>
            options.UseSqlServer(defaultConnection, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "DW")));

    }

    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var keyRaw = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        if (keyRaw.Length < 32) throw new InvalidOperationException("Jwt:Key must be >= 32 chars");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyRaw));

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                options.MapInboundClaims = false;
            });
        builder.Services.AddAuthorization(options => ArticlePolicies.Configure(options));
    }

    public static void ConfigureCors(this WebApplicationBuilder builder)
    {
        var corsOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:7065" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
    }
    public static void ConfigureOptions(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ArticlesModuleOptions>(
            builder.Configuration.GetSection(ArticlesModuleOptions.SectionName));
        // External APIs, storage and recognition options are owned by AddUnifiedDide.
    }

    public static void ConfigureDependencyInjection(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new RouteTokenTransformerConvention(
                new LowercaseRouteTokenTransformer()));
        })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        // Remaining shared/external and DW dependencies. Projects/Articles are composed by AddUnifiedDide.
        // Repos concretos

        // Gen�ricos

        // Servicios
        builder.Services.AddScoped<IDwEtlService, DwEtlService>();
    }

    public static void ConfigureApiDocumentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Tesis Project API",
                Version = "v1",
                Description = "API for Tesis Project Backend"
            });

            options.AddSecurityDefinition("Bearer", new()
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and then your token"
            });

            options.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    sealed class LowercaseRouteTokenTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value)
            => value?.ToString()?.ToLowerInvariant();
    }

    public static void ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tesis Project API v1");
                options.RoutePrefix = string.Empty;
            });

            app.UseHttpLogging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }));
    }
}
