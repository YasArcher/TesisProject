using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Identity;
using tesisproject.backend.Options;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Implementations;
using tesisproject.backend.UnitOfWork.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure services
ConfigureLogging(builder);
ConfigureDatabase(builder);
ConfigureIdentity(builder);
ConfigureAuthentication(builder);
ConfigureCors(builder);
ConfigureOptions(builder);
ConfigureHttpClients(builder);
ConfigureDependencyInjection(builder);
ConfigureApiDocumentation(builder);

var app = builder.Build();

// Configure pipeline
ConfigurePipeline(app);

app.Run();

// ====== SERVICE CONFIGURATION METHODS ======

static void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    builder.Logging.AddConsole();

    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        // Optional: Log HTTP requests in development
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                   HttpLoggingFields.ResponsePropertiesAndHeaders;
            options.RequestBodyLogLimit = 4096;
            options.ResponseBodyLogLimit = 4096;
        });
    }
}

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

static void ConfigureIdentity(WebApplicationBuilder builder)
{
    builder.Services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            // Password requirements
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            // User requirements
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

    builder.Services.AddAuthentication(options =>
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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();
}

static void ConfigureCors(WebApplicationBuilder builder)
{
    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["https://localhost:7065"];

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

static void ConfigureOptions(WebApplicationBuilder builder)
{
    // Register configuration sections as options
    builder.Services.Configure<ExternalApiOptions>(
        builder.Configuration.GetSection("ExternalUsers"));
}

static void ConfigureHttpClients(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<IExternalUsersService, ExternalUsersService>(client =>
    {
        var baseUrl = builder.Configuration["ExternalUsers:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        var timeoutSeconds = builder.Configuration.GetValue<int>("ExternalUsers:TimeoutSeconds", 30);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        client.DefaultRequestHeaders.Add("User-Agent", "TesisProject/1.0");
    });
}

static void ConfigureDependencyInjection(WebApplicationBuilder builder)
{
    // Controllers with JSON options
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

    // Unit of Work pattern
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Repositories
    builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();

    // Application Services
    builder.Services.AddScoped<ITokenService, JwtTokenService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IGroupService, GroupService>();
}

static void ConfigureApiDocumentation(WebApplicationBuilder builder)
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

        // Add JWT authentication to Swagger
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

// ====== PIPELINE CONFIGURATION METHOD ======

static void ConfigurePipeline(WebApplication app)
{
    // Development-specific middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tesis Project API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger at root
        });

        // Optional: Enable HTTP request logging in development
        app.UseHttpLogging();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // Security and CORS
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Routing
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow
    }));
}