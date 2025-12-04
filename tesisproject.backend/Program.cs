using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tesisproject.backend.BI.ETL;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Seed;          // ✅ Importante: SeedCatalogs
using tesisproject.backend.DataWarehouse;
using tesisproject.backend.Identity;
using tesisproject.backend.Mapping;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Implementations;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Abstractions.Auth;
using tesisproject.shared.Abstractions.Project;
using tesisproject.shared.DTOs.Project;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var config = builder.Configuration;

// ===== PUERTOS (dev local) =====
builder.WebHost.PreferHostingUrls(true)
               .UseUrls("http://localhost:5040", "https://localhost:7040");

// ===== DbContext =====
var cs = config.GetConnectionString("DefaultConnection")
          ?? "Server=PERSONAL\\DINNOVA;Database=TesisDB;User Id=sa;Password=admin123;TrustServerCertificate=True;MultipleActiveResultSets=True";

builder.Services.AddDbContext<DwDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DwConnection")));

builder.Services.AddScoped<IEtlOrchestrator, EtlOrchestrator>();
services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(cs);
    opt.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// ===== AutoMapper =====
services.AddAutoMapper(typeof(ArticleMapping).Assembly);

// ===== Repos/UoW/Servicios de dominio =====
services.AddScoped<IProjectsService, ProjectsService>();
services.AddScoped<IArticlesRepository, ArticlesRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IArticlesService, ArticlesService>();
services.AddScoped<IVenuesService, VenuesService>();
services.AddHttpContextAccessor();


// ===== CORS =====
const string CorsPolicyName = "wasm";
services.AddCors(o => o.AddPolicy(CorsPolicyName, p => p
    .WithOrigins("http://localhost:5189", "https://localhost:7189")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

// ===== Identity =====
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

// ===== JWT =====
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

services.Configure<JwtOptions>(config.GetSection("Jwt"));

var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "local",
    Audience = "local",
    Key = "dev-very-long-key-please-change"
};

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

// ===== Evitar redirects HTML en APIs =====
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

// ===== Autorización =====
services.AddAuthorization(options =>
{
    // Todo requiere autenticación por defecto
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("ArticlesWrite", policy =>
        policy.RequireRole("Admin", "Editor", "SuperAdmin"));

    options.AddPolicy("OnlyAdmins", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
});

// ===== Auth/Audit =====
services.AddScoped<IAuthService, IdentityAuthService>();
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IAuditLogger, AuditLogger>();

// ===== Controllers + Swagger =====
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

var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

// ===== Migrar BD + Seed =====
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    logger.LogWarning("EF connecting to: {cs}", cs);

    try
    {
        await db.Database.MigrateAsync();

        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT t.name FROM sys.tables t ORDER BY t.name")
            .ToListAsync();

        logger.LogWarning("EF existing tables: {tables}", string.Join(", ", tables));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error migrating database");
        throw;
    }

    // Seed roles + admin
    try
    {
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Editor", "Viewer", "SuperAdmin" })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new ApplicationRole { Name = role });
        }

        var adminEmail = "admin@local.test";
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userMgr.CreateAsync(admin, "Admin#1234");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Seeding Identity failed");
    }

    // ✅ Seed de catálogos alineado al modelo + TXT/XLSX
    try
    {
        await SeedCatalogs.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Seeding catalogs failed");
    }
}

// ===== Endpoints =====
app.MapControllers();

// Minimal API Projects
var projects = app.MapGroup("/api/projects");
projects.MapGet("", (IProjectsService svc, CancellationToken ct) => svc.GetAllAsync(ct)).WithOpenApi();
projects.MapGet("/{id}", (int id, IProjectsService svc, CancellationToken ct) => svc.GetByIdAsync(id, ct)).WithOpenApi();
projects.MapPost("/", (CreateProjectRequest req, IProjectsService svc, CancellationToken ct) => svc.CreateAsync(req, ct))
        .RequireAuthorization("OnlyAdmins").WithOpenApi();
projects.MapPut("/", (UpdateProjectRequest req, IProjectsService svc, CancellationToken ct) => svc.UpdateAsync(req, ct))
        .RequireAuthorization("OnlyAdmins").WithOpenApi();
projects.MapDelete("/{id}", (int id, IProjectsService svc, CancellationToken ct) => svc.DeleteAsync(id, ct))
        .RequireAuthorization("OnlyAdmins").WithOpenApi();

app.MapGet("/ping", () => "pong").AllowAnonymous();

app.Run();
