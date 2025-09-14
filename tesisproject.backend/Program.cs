using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AutoMapper;

using tesisproject.backend.Data;
using tesisproject.backend.Mapping;                       // ArticleMapping
using tesisproject.backend.Repositories.Interfaces;       // IArticlesRepository
using tesisproject.backend.Repositories.Implementations;  // ArticlesRepository
using tesisproject.backend.UnitOfWork.Interfaces;         // IUnitOfWork
using tesisproject.backend.UnitOfWork.Implementations;    // UnitOfWork
using tesisproject.backend.Services;                      // ProjectsService (si está aquí)
using tesisproject.backend.Services.Implementations;      // ArticlesService
using tesisproject.shared.Abstractions.Articles;          // IArticlesService
using tesisproject.shared.Abstractions.Project;           // IProjectsService
using tesisproject.shared.DTOs.Project;                   // DTOs de Projects

var builder = WebApplication.CreateBuilder(args);

// Kestrel: HTTP y HTTPS en dev
builder.WebHost.ConfigureKestrel(k =>
{
    k.ListenLocalhost(5040);                      // HTTP
    k.ListenLocalhost(7040, o => o.UseHttps());  // HTTPS
});

// DbContext (usa tu cadena "Default"; si falta, un fallback seguro)
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Server=.;Database=TesisDB;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

// AutoMapper (requiere paquetes AutoMapper y AutoMapper.Extensions.Microsoft.DependencyInjection)
builder.Services.AddAutoMapper(typeof(ArticleMapping).Assembly);

// DI (Projects y Articles)
builder.Services.AddScoped<IProjectsService, ProjectsService>();
builder.Services.AddScoped<IArticlesRepository, ArticlesRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IArticlesService, ArticlesService>();

// CORS para tu frontend
builder.Services.AddCors(o => o.AddPolicy("wasm", p => p
    .WithOrigins("https://localhost:7065")
    .AllowAnyHeader()
    .AllowAnyMethod()
));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tesis API", Version = "v1" });
});

// Controllers (por ejemplo, ArticlesController)
builder.Services.AddControllers();

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tesis API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("wasm");

// Controllers (Articles, etc.)
app.MapControllers();

// Minimal API de Projects (lo que ya tenías)
var projects = app.MapGroup("/api/projects");
projects.MapGet("/", (IProjectsService svc, CancellationToken ct) => svc.GetAllAsync(ct)).WithOpenApi();
projects.MapGet("/{id}", (int id, IProjectsService svc, CancellationToken ct) => svc.GetByIdAsync(id, ct)).WithOpenApi();
projects.MapPost("/", (CreateProjectRequest req, IProjectsService svc, CancellationToken ct) => svc.CreateAsync(req, ct)).WithOpenApi();
projects.MapPut("/", (UpdateProjectRequest req, IProjectsService svc, CancellationToken ct) => svc.UpdateAsync(req, ct)).WithOpenApi();
projects.MapDelete("/{id}", (int id, IProjectsService svc, CancellationToken ct) => svc.DeleteAsync(id, ct)).WithOpenApi();

// Healthcheck
app.MapGet("/ping", () => "pong");

app.Run();
