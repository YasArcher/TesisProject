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
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Analytic.Implementations;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Implementations;
using tesisproject.backend.UnitOfWork.Interfaces;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

// ===== Configure services =====
builder.ConfigureLogging();
builder.ConfigureDatabase();
builder.ConfigureIdentity();
builder.ConfigureAuthentication();
builder.ConfigureCors();
builder.ConfigureOptions();
builder.ConfigureHttpClients();
builder.ConfigureDependencyInjection();
builder.ConfigureApiDocumentation();

ExcelPackage.License.SetNonCommercialOrganization("Universidad Técnica de Ambato");

var app = builder.Build();

// 1) Migraciones primero (para que existan tablas, incluyendo Identity)
await ApplyMigrationsAsync(app);

// 2) Seed de roles después de migrar
await EnsureIdentityRolesAsync(app, "admin", "financial", "technical", "superadmin", "coordinador", "user");

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

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(defaultConnection));

        builder.Services.AddDbContext<DwContext>(options =>
            options.UseSqlServer(defaultConnection));
    }

    public static void ConfigureIdentity(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddIdentityCore<IdentityUser<int>>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<SignInManager<IdentityUser<int>>>()
            .AddDefaultTokenProviders();

        builder.Services.AddHttpContextAccessor();
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

        builder.Services.AddAuthorization();
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
        builder.Services.AddOptions<ExternalApiOptions>()
            .Bind(builder.Configuration.GetSection(ExternalApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<StorageOptions>()
            .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath), "Storage:RootPath is required")
            .ValidateOnStart();

        builder.Services.AddOptions<DocumentRecognitionOptions>()
            .Bind(builder.Configuration.GetSection(DocumentRecognitionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static void ConfigureHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ExternalApi")
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;

                client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

                if (!string.IsNullOrWhiteSpace(opts.UserAgent))
                    client.DefaultRequestHeaders.Add("User-Agent", opts.UserAgent);
            });

        builder.Services.AddHttpClient<IExternalDirectoryClient, ExternalDirectoryClient>("ExternalApi");
        builder.Services.AddHttpClient<IExternalPeriodsClient, ExternalPeriodsClient>("ExternalApi");
        builder.Services.AddHttpClient<IExternalAcademicsService, ExternalAcademicsService>("ExternalApi");
        builder.Services.AddHttpClient<IExternalDistributivosService, ExternalDistributivosService>("ExternalApi");
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

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddHttpContextAccessor();

        // Repos concretos
        builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
        builder.Services.AddScoped<IGroupRepository, GroupRepository>();
        builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
        builder.Services.AddScoped<IVisitRepository, VisitRepository>();
        builder.Services.AddScoped<IProjectExtensionRepository, ProjectExtensionRepository>();
        builder.Services.AddScoped<IAspNetUserRepository, AspNetUserRepository>();
        builder.Services.AddScoped<IVisitIssueRepository, VisitIssueRepository>();
        builder.Services.AddScoped<IConvocationRepository, ConvocationRepository>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IProductAttributeDefinitionRepository, ProductAttributeDefinitionRepository>();
        builder.Services.AddScoped<IProductAuthorRepository, ProductAuthorRepository>();
        builder.Services.AddScoped<IProductValueRepository, ProductValueRepository>();
        builder.Services.AddScoped<IProjectObjectiveRepository, ProjectObjectiveRepository>();
        builder.Services.AddScoped<IObjectiveActivityRepository, ObjectiveActivityRepository>();
        builder.Services.AddScoped<IObjectiveActivityUserRepository, ObjectiveActivityUserRepository>();
        builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
        builder.Services.AddScoped<IProjectResearchCategoryRepository, ProjectResearchCategoryRepository>();
        builder.Services.AddScoped<IResearchCategoryRepository, ResearchCategoryRepository>();
        builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
        builder.Services.AddScoped<IExternalResearcherRepository, ExternalResearcherRepository>();
        builder.Services.AddScoped<IExternalResearcherProjectRepository, ExternalResearcherProjectRepository>();
        builder.Services.AddScoped<IProjectDocumentRepository, ProjectDocumentRepository>();
        builder.Services.AddScoped<IExportTemplateColumnRepository, ExportTemplateColumnRepository>();
        builder.Services.AddScoped<IExportTemplateRepository, ExportTemplateRepository>();
        builder.Services.AddScoped<IExportFieldRepository, ExportFieldRepository>();
        builder.Services.AddScoped<IMatrixExcelExportService, MatrixExcelExportService>();
        builder.Services.AddScoped<IMatrixTemplateExcelExportService, MatrixTemplateExcelExportService>();
        builder.Services.AddScoped<IVisitObjectiveActivityProgressRepository, VisitObjectiveActivityProgressRepository>();
        builder.Services.AddScoped<IFacultyScopeRepository, FacultyScopeRepository>();
        builder.Services.AddScoped<IFacultyScopeFacultyRepository, FacultyScopeFacultyRepository>();
        builder.Services.AddScoped<IUserFacultyScopeAssignmentRepository, UserFacultyScopeAssignmentRepository>();
        builder.Services.AddScoped<IAppConfigurationRepository, AppConfigurationRepository>();

        // Genéricos
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped(typeof(ICatalogRepository<>), typeof(CatalogRepository<>));

        // Servicios
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        builder.Services.AddScoped<IProjectsFiltersService, ProjectsFiltersService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<ITokenService, JwtTokenService>();
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IGroupService, GroupService>();
        builder.Services.AddScoped<IBudgetService, BudgetService>();
        builder.Services.AddScoped<IVisitService, VisitService>();
        builder.Services.AddScoped<IProjectExtensionService, ProjectExtensionService>();
        builder.Services.AddScoped<IVisitIssueService, VisitIssueService>();
        builder.Services.AddScoped<IConvocationService, ConvocationService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IProjectObjectiveService, ProjectObjectiveService>();
        builder.Services.AddScoped<IObjectiveActivityService, ObjectiveActivityService>();
        builder.Services.AddScoped<IObjectiveActivityUserService, ObjectiveActivityUserService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<IDocumentRecognitionService, DocumentRecognitionService>();
        builder.Services.AddScoped<IMemberRoleTypeService, MemberRoleTypeService>();
        builder.Services.AddScoped<IProjectResearchCategoryService, ProjectResearchCategoryService>();
        builder.Services.AddScoped<IResearchCategoryService, ResearchCategoryService>();
        builder.Services.AddScoped<IResearchCategoryTypeService, ResearchCategoryTypeService>();
        builder.Services.AddScoped<IAppUserService, AppUserService>();
        builder.Services.AddScoped<IExternalResearcherService, ExternalResearcherService>();
        builder.Services.AddScoped<ICountryService, CountryService>();
        builder.Services.AddScoped<IInstitutionService, InstitutionService>();
        builder.Services.AddScoped<IExternalResearcherProjectService, ExternalResearcherProjectService>();
        builder.Services.AddScoped<IDwEtlService, DwEtlService>();
        builder.Services.AddScoped<IIndexingSourceService, IndexingSourceService>();
        builder.Services.AddScoped(typeof(ICatalogCrudService<>), typeof(CatalogCrudService<>));
        builder.Services.AddScoped<IProductAttributeService, ProductAttributeService>();
        builder.Services.AddScoped<IProductAttributeDefinitionService, ProductAttributeDefinitionService>();
        builder.Services.AddScoped<IProductTypeDesignService, ProductTypeDesignService>();
        builder.Services.AddScoped<IProjectMatrixService, ProjectMatrixService>();
        builder.Services.AddScoped<IProjectFlatReportService, ProjectFlatReportService>();
        builder.Services.AddScoped<IExportTemplateService, ExportTemplateService>();
        builder.Services.AddScoped<IExportTemplateExcelService, ExportTemplateExcelService>();
        builder.Services.AddScoped<IVisitObjectiveActivityProgressService, VisitObjectiveActivityProgressService>();
        builder.Services.AddScoped<IFacultyScopeService, FacultyScopeService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();
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