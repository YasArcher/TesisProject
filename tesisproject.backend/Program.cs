using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tesisproject.backend.Data;
using tesisproject.backend.Identity;
using tesisproject.backend.Mapping;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services;
using tesisproject.backend.Services.Implementations;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Implementations;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Abstractions.Articles;
using tesisproject.shared.Abstractions.Auth;
using tesisproject.shared.Abstractions.Project;
using tesisproject.shared.DTOs.Project;

var builder = WebApplication.CreateBuilder(args);

// ===== PUERTOS FIJOS DEL BACKEND =====
builder.WebHost.PreferHostingUrls(true)
               .UseUrls("http://localhost:5040", "https://localhost:7040");

var services = builder.Services;
var config = builder.Configuration;

// ===== DbContext =====
var cs = config.GetConnectionString("DefaultConnection")
          ?? "Server=.;Database=TesisDB;Trusted_Connection=True;TrustServerCertificate=True";
services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

// ===== AutoMapper =====
services.AddAutoMapper(typeof(ArticleMapping).Assembly);

// ===== Capa de dominio / repos / UoW =====
services.AddScoped<IProjectsService, ProjectsService>();
services.AddScoped<IArticlesRepository, ArticlesRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IArticlesService, ArticlesService>();

services.AddHttpContextAccessor();

// ===== CORS (FRONT: 5189 / 7189) =====
const string CorsPolicyName = "wasm";
services.AddCors(o => o.AddPolicy(CorsPolicyName, p => p
    .WithOrigins("http://localhost:5189", "https://localhost:7189")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

// ===== Identity (registra cookies por defecto) =====
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

// ===== JWT (esquema por defecto) =====
JwtSecurityTokenHandler.DefaultMapInboundClaims = false; // nombres de claims tal cual
services.Configure<JwtOptions>(config.GetSection("Jwt"));
var jwt = config.GetSection("Jwt").Get<JwtOptions>()!;

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

// Evitar redirecciones HTML a /Account/Login en requests API → devolver 401/403
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
builder.Services.AddAuthorization(options =>
{
    // Todo requiere autenticación salvo [AllowAnonymous]
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // 👉 Policy para escribir artículos: incluye Admin, Editor y SuperAdmin
    options.AddPolicy("ArticlesWrite", policy =>
        policy.RequireRole("Admin", "Editor", "SuperAdmin"));
});

// ===== Servicios Auth/Audit =====
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

// Redirección HTTPS solo en no-Dev (evita warnings en local)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Orden correcto: CORS → Auth → AuthZ
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

// ===== Seed roles y usuario admin =====
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "Editor", "Viewer" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new ApplicationRole { Name = role });

    var adminEmail = "admin@local.test";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser { UserName = "admin", Email = adminEmail, EmailConfirmed = true };
        await userMgr.CreateAsync(admin, "Admin#1234");
        await userMgr.AddToRoleAsync(admin, "Admin");
    }
}

// ===== Endpoints =====
app.MapControllers();

// Minimal API de Projects (ejemplo)
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
