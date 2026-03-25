using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Identity;
using tesisproject.shared.Abstractions.Project;
using tesisproject.shared.DTOs.Project;

namespace tesisproject.backend.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static async Task ValidateConfiguredDatabaseAsync(this WebApplication app, string connectionString)
    {
        using var scope = app.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("StartupDatabaseValidation");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogWarning("EF connecting to: {cs}", connectionString);

        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                try
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                }
                catch (Exception sqlEx)
                {
                    throw new InvalidOperationException(
                        $"No se pudo establecer conexion con la base de datos configurada. SQL error: {sqlEx.Message}",
                        sqlEx);
                }

                throw new InvalidOperationException("No se pudo establecer conexion con la base de datos configurada.");
            }

            var tables = await db.Database
                .SqlQueryRaw<string>("SELECT t.name FROM sys.tables t ORDER BY t.name")
                .ToListAsync();

            logger.LogWarning("EF existing tables: {tables}", string.Join(", ", tables));

            var identityTablesExist = tables.Any(x => x == "AspNetUsers") && tables.Any(x => x == "AspNetRoles");
            if (!identityTablesExist)
            {
                logger.LogWarning("Identity tables were not found in the configured database. Startup seed was skipped.");
                return;
            }

            try
            {
                var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                foreach (var role in new[] { "Admin", "Editor", "Viewer", "SuperAdmin" })
                {
                    if (!await roleMgr.RoleExistsAsync(role))
                    {
                        await roleMgr.CreateAsync(new ApplicationRole { Name = role });
                    }
                }

                const string adminEmail = "admin@local.test";
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
                logger.LogWarning(ex, "Identity startup seed failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating configured database");
            throw;
        }
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapControllers();

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
        return app;
    }
}
