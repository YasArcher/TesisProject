using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.backend.Data;
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Implementations;
using tesisproject.backend.UnitOfWork.Interfaces;

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

var app = builder.Build();

// ===== Configure pipeline =====
app.ConfigurePipeline();

app.Run();


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
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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

        // Do not remap inbound claims automatically
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
        // Bind strongly-typed options once
        builder.Services.Configure<ExternalApiOptions>(
            builder.Configuration.GetSection(ExternalApiOptions.SectionName));
    }

    public static void ConfigureHttpClients(this WebApplicationBuilder builder)
    {
        // Snapshot for initial defaults (non-rooted values are fine)
        var opts = builder.Configuration
            .GetSection(ExternalApiOptions.SectionName)
            .Get<ExternalApiOptions>() ?? new ExternalApiOptions();

        builder.Services.AddHttpClient("ExternalApi", client =>
        {
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl);

            if (opts.TimeoutSeconds > 0)
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(opts.UserAgent))
                client.DefaultRequestHeaders.Add("User-Agent", opts.UserAgent);
        });

        builder.Services.AddHttpClient<IExternalDirectoryClient, ExternalDirectoryClient>("ExternalApi");

        builder.Services.AddScoped<IExternalAcademicsService, ExternalAcademicsService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi");
            var options = sp.GetRequiredService<IOptions<ExternalApiOptions>>();
            var logger = sp.GetRequiredService<ILogger<ExternalAcademicsService>>();
            return new ExternalAcademicsService(http, options, logger);
        });
    }

    public static void ConfigureDependencyInjection(this WebApplicationBuilder builder)
    {
        // Controllers + JSON
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        // UoW
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // =========================
        // Repositorios concretos de dominio (no catálogos genéricos)
        // =========================
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
        builder.Services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
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


        // ?? OJO: ya NO registramos repositorios de catálogo específicos como:
        // IObjectiveTypeRepository, IMemberRoleTypeRepository,
        // IProjectTypeRepository, IDocumentTypeRepository, IResearchCategoryTypeRepository, etc.
        // Porque ahora usas ICatalogRepository<T> en el UnitOfWork.

        // =========================
        // Repositorios genéricos
        // =========================
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped(typeof(ICatalogRepository<>), typeof(CatalogRepository<>));

        // =========================
        // Servicios de aplicación
        // =========================
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        builder.Services.AddScoped<IProjectsFiltersService, ProjectsFiltersService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
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
        builder.Services.AddScoped<IProductTypeService, ProductTypeService>();
        builder.Services.AddScoped<IObjectiveTypeService, ObjectiveTypeService>();
        builder.Services.AddScoped<IProjectObjectiveService, ProjectObjectiveService>();
        builder.Services.AddScoped<IObjectiveActivityService, ObjectiveActivityService>();
        builder.Services.AddScoped<IObjectiveActivityUserService, ObjectiveActivityUserService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<IDocumentRecognitionService, DocumentRecognitionService>();
        builder.Services.AddScoped<IMemberRoleTypeService, MemberRoleTypeService>();
        builder.Services.AddScoped<IProjectTypeService, ProjectTypeService>();
        builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        builder.Services.AddScoped<IProjectResearchCategoryService, ProjectResearchCategoryService>();
        builder.Services.AddScoped<IResearchCategoryService, ResearchCategoryService>();
        builder.Services.AddScoped<IResearchCategoryTypeService, ResearchCategoryTypeService>();
        builder.Services.AddScoped<IFundingTypeService, FundingTypeService>();
        builder.Services.AddScoped<IResearchCategoryGroupService, ResearchCategoryGroupService>();
        builder.Services.AddScoped<IAppUserService, AppUserService>();
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

        app.UseHttpsRedirection();
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
