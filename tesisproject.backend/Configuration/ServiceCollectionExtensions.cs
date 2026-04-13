using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tesisproject.backend.BI.ETL;
using tesisproject.backend.Data;
using tesisproject.backend.DataWarehouse;
using tesisproject.backend.Filters;
using tesisproject.backend.Identity;
using tesisproject.backend.Options;
using tesisproject.backend.Reporting.Data;
using tesisproject.backend.Services;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.Abstractions.Auth;

namespace tesisproject.backend.Configuration;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "wasm";

    public static IServiceCollection AddAppDataProtection(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var dataProtectionPath = Path.Combine(environment.ContentRootPath, "App_Data", "DataProtectionKeys");
        Directory.CreateDirectory(dataProtectionPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

        return services;
    }

    public static IServiceCollection AddAppPersistence(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment, string connectionString)
    {
        services.AddDbContext<DwDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DwConnection")));

        services.AddDbContext<ReportingDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("ReportingConnection")
                                 ?? config.GetConnectionString("DwConnection")));

        services.AddScoped<IEtlOrchestrator, EtlOrchestrator>();

        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseSqlServer(connectionString);
            opt.EnableSensitiveDataLogging(environment.IsDevelopment());
        });

        return services;
    }

    public static IServiceCollection AddAppDomainServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IArticlesService, ArticlesService>();
        services.AddScoped<IVenuesService, VenuesService>();
        services.AddScoped<IConfigurationFormsService, ConfigurationFormsService>();
        services.AddScoped<IArticleRegistrationService, ArticleRegistrationService>();
        services.AddScoped<IArticleAggregatePersistenceService, ArticleAggregatePersistenceService>();
        services.AddScoped<IBulkImportService, BulkImportService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IRegistrationMatrixService, RegistrationMatrixService>();
        services.AddScoped<IExternalApiExplorerService, ExternalApiExplorerService>();
        services.AddScoped<IInstitutionalReportingService, InstitutionalReportingService>();
        services.Configure<ExternalApiExplorerOptions>(config.GetSection("ExternalApis"));
        services.Configure<LegacyReportingOptions>(config.GetSection("LegacyReporting"));
        services.AddScoped<LegacyReportingEnabledFilter>();
        services.AddHttpClient("external-api-explorer", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(25);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TesisProject/1.0 (external-api-explorer)");
        });
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<IInstitutionAuthorDirectoryService, LocalInstitutionAuthorDirectoryService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.Configure<InstitutionIdentityOptions>(config.GetSection("InstitutionIdentity"));

        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services)
    {
        services.AddCors(o => o.AddPolicy(CorsPolicyName, p => p
            .WithOrigins("http://localhost:5189", "https://localhost:7189")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return services;
    }

    public static IServiceCollection AddAppIdentityAndAuth(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(opt =>
            {
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireDigit = true;
                opt.User.RequireUniqueEmail = true;
                opt.SignIn.RequireConfirmedAccount = false;
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        var jwt = config.GetSection("Jwt").Get<JwtOptions>()
                  ?? throw new InvalidOperationException("La sección Jwt no está configurada.");

        if (string.IsNullOrWhiteSpace(jwt.Issuer)
            || string.IsNullOrWhiteSpace(jwt.Audience)
            || string.IsNullOrWhiteSpace(jwt.Key))
        {
            throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience y Jwt:Key son obligatorios.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        services.ConfigureApplicationCookie(opt =>
        {
            opt.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };

            opt.Events.OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(AppPolicies.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AppPolicies.SecurityAdministration, policy =>
                policy.RequireRole(AppRoles.Admin));

            options.AddPolicy(AppPolicies.AuthorSubmission, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.Author));

            options.AddPolicy(AppPolicies.ArticlesWrite, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Analyst));

            options.AddPolicy(AppPolicies.WorkflowAccess, policy =>
                policy.RequireRole(
                    AppRoles.Admin,
                    AppRoles.Author,
                    AppRoles.WorkflowReviewerUodide,
                    AppRoles.WorkflowReviewerAreaTecnica,
                    AppRoles.WorkflowProcessorAreaTecnica));

            options.AddPolicy(AppPolicies.WorkflowReview, policy =>
                policy.RequireRole(
                    AppRoles.Admin,
                    AppRoles.WorkflowReviewerUodide,
                    AppRoles.WorkflowReviewerAreaTecnica));

            options.AddPolicy(AppPolicies.WorkflowProcess, policy =>
                policy.RequireRole(
                    AppRoles.Admin,
                    AppRoles.WorkflowProcessorAreaTecnica));

            options.AddPolicy(AppPolicies.BulkImportAccess, policy =>
                policy.RequireRole(
                    AppRoles.Admin,
                    AppRoles.Analyst,
                    AppRoles.WorkflowReviewerUodide,
                    AppRoles.WorkflowReviewerAreaTecnica,
                    AppRoles.WorkflowProcessorAreaTecnica));

            options.AddPolicy(AppPolicies.ConfigurationAdministration, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Analyst));

            options.AddPolicy(AppPolicies.ExternalApiAccess, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.Author));

            options.AddPolicy(AppPolicies.ReportingAccess, policy =>
                policy.RequireRole(
                    AppRoles.Admin,
                    AppRoles.Analyst,
                    AppRoles.WorkflowReviewerAreaTecnica,
                    AppRoles.WorkflowProcessorAreaTecnica));
        });

        return services;
    }

    public static IServiceCollection AddAppApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tesis API", Version = "v1" });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingresa el token como: Bearer {token}",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtScheme, Array.Empty<string>() }
            });
        });

        services.AddProblemDetails();
        return services;
    }
}
