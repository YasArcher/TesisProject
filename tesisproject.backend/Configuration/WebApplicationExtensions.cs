using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Identity;

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

            await EnsureRegistrationMatrixModuleTablesAsync(db, logger);

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

    private static async Task EnsureRegistrationMatrixModuleTablesAsync(AppDbContext db, ILogger logger)
    {
        const string sql = @"
IF OBJECT_ID(N'[dbo].[RegistrationMatrix]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationMatrix](
        [RegistrationMatrixId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [EntityName] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [Notes] NVARCHAR(1000) NULL,
        [LastImportBatchId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_RegistrationMatrix_ImportBatch_LastImportBatchId]
            FOREIGN KEY ([LastImportBatchId]) REFERENCES [dbo].[ImportBatch]([ImportBatchId]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_RegistrationMatrix_LastImportBatchId] ON [dbo].[RegistrationMatrix]([LastImportBatchId]);
END;

IF OBJECT_ID(N'[dbo].[RegistrationMatrixColumn]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationMatrixColumn](
        [RegistrationMatrixColumnId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RegistrationMatrixId] INT NOT NULL,
        [FieldId] INT NOT NULL,
        [DisplayOrder] INT NOT NULL,
        [WidthUnits] INT NOT NULL CONSTRAINT [DF_RegistrationMatrixColumn_WidthUnits] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [FK_RegistrationMatrixColumn_FieldCatalog_FieldId]
            FOREIGN KEY ([FieldId]) REFERENCES [dbo].[FieldCatalog]([FieldId]),
        CONSTRAINT [FK_RegistrationMatrixColumn_RegistrationMatrix_RegistrationMatrixId]
            FOREIGN KEY ([RegistrationMatrixId]) REFERENCES [dbo].[RegistrationMatrix]([RegistrationMatrixId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_RegistrationMatrixColumn_FieldId] ON [dbo].[RegistrationMatrixColumn]([FieldId]);
    CREATE UNIQUE INDEX [IX_RegistrationMatrixColumn_RegistrationMatrixId_FieldId] ON [dbo].[RegistrationMatrixColumn]([RegistrationMatrixId], [FieldId]);
END;

IF OBJECT_ID(N'[dbo].[RegistrationMatrixRow]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationMatrixRow](
        [RegistrationMatrixRowId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RegistrationMatrixId] INT NOT NULL,
        [RowNumber] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_RegistrationMatrixRow_RegistrationMatrix_RegistrationMatrixId]
            FOREIGN KEY ([RegistrationMatrixId]) REFERENCES [dbo].[RegistrationMatrix]([RegistrationMatrixId]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_RegistrationMatrixRow_RegistrationMatrixId_RowNumber] ON [dbo].[RegistrationMatrixRow]([RegistrationMatrixId], [RowNumber]);
END;

IF OBJECT_ID(N'[dbo].[RegistrationMatrixCell]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationMatrixCell](
        [RegistrationMatrixCellId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RegistrationMatrixRowId] INT NOT NULL,
        [FieldId] INT NOT NULL,
        [RawValue] NVARCHAR(4000) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_RegistrationMatrixCell_FieldCatalog_FieldId]
            FOREIGN KEY ([FieldId]) REFERENCES [dbo].[FieldCatalog]([FieldId]),
        CONSTRAINT [FK_RegistrationMatrixCell_RegistrationMatrixRow_RegistrationMatrixRowId]
            FOREIGN KEY ([RegistrationMatrixRowId]) REFERENCES [dbo].[RegistrationMatrixRow]([RegistrationMatrixRowId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_RegistrationMatrixCell_FieldId] ON [dbo].[RegistrationMatrixCell]([FieldId]);
    CREATE UNIQUE INDEX [IX_RegistrationMatrixCell_RegistrationMatrixRowId_FieldId] ON [dbo].[RegistrationMatrixCell]([RegistrationMatrixRowId], [FieldId]);
END;
";

        await db.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Registration matrix module tables verified.");
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapControllers();

        app.MapGet("/ping", () => "pong").AllowAnonymous();
        return app;
    }
}
