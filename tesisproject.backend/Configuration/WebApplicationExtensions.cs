using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;

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

            await EnsureIdentitySchemaAsync(db, logger);
            await EnsureRegistrationMatrixModuleTablesAsync(db, logger);
            await EnsureWorkflowModuleSchemaAsync(db, logger);

            var tables = await db.Database
                .SqlQueryRaw<string>("SELECT t.name FROM sys.tables t ORDER BY t.name")
                .ToListAsync();

            logger.LogWarning("EF existing tables: {tables}", string.Join(", ", tables));

            try
            {
                var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

                foreach (var role in AppRoles.All.Concat(["Editor", "Viewer", "SuperAdmin"]))
                {
                    if (!await roleMgr.RoleExistsAsync(role))
                    {
                        await roleMgr.CreateAsync(new ApplicationRole { Name = role });
                    }
                }

                await EnsureSeedUserAsync(
                    userMgr,
                    "admin@local.test",
                    "Admin#1234",
                    "Administrador del sistema",
                    [AppRoles.Admin]);

                await EnsureSeedUserAsync(
                    userMgr,
                    "autor.demo@uta.edu.ec",
                    "Autor#1234",
                    "Docente Autor Demo",
                    [AppRoles.Author]);

                await EnsureSeedUserAsync(
                    userMgr,
                    "uodide.demo@uta.edu.ec",
                    "Uodide#1234",
                    "Revisor UODIDE Demo",
                    [AppRoles.WorkflowReviewerUodide]);

                await EnsureSeedUserAsync(
                    userMgr,
                    "tecnica.demo@uta.edu.ec",
                    "Tecnica#1234",
                    "Revisor Área Técnica Demo",
                    [AppRoles.WorkflowReviewerAreaTecnica, AppRoles.WorkflowProcessorAreaTecnica]);

                await workflowService.EnsureSeedDataAsync();
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

    private static async Task EnsureIdentitySchemaAsync(AppDbContext db, ILogger logger)
    {
        const string sql = @"
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoles](
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(256) NULL,
        [NormalizedName] NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END;

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUsers](
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserName] NVARCHAR(256) NULL,
        [NormalizedUserName] NVARCHAR(256) NULL,
        [Email] NVARCHAR(256) NULL,
        [NormalizedEmail] NVARCHAR(256) NULL,
        [EmailConfirmed] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_EmailConfirmed] DEFAULT (0),
        [PasswordHash] NVARCHAR(MAX) NULL,
        [SecurityStamp] NVARCHAR(MAX) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_PhoneNumberConfirmed] DEFAULT (0),
        [TwoFactorEnabled] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_TwoFactorEnabled] DEFAULT (0),
        [LockoutEnd] DATETIMEOFFSET NULL,
        [LockoutEnabled] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_LockoutEnabled] DEFAULT (0),
        [AccessFailedCount] INT NOT NULL CONSTRAINT [DF_AspNetUsers_AccessFailedCount] DEFAULT (0),
        [FullName] NVARCHAR(MAX) NULL
    );
    CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers]([NormalizedEmail]);
    CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'FullName') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [FullName] NVARCHAR(MAX) NULL;
END;

IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
            FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]([RoleId]);
END;

IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]([UserId]);
END;

IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins](
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [ProviderKey] NVARCHAR(450) NOT NULL,
        [ProviderDisplayName] NVARCHAR(MAX) NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]([UserId]);
END;

IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles](
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
            FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]([RoleId]);
END;

IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens](
        [UserId] NVARCHAR(450) NOT NULL,
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [Name] NVARCHAR(450) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
END;
";

        await db.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Identity schema verified.");
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
        [CreatedByUserId] NVARCHAR(450) NULL,
        [LastImportBatchId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_RegistrationMatrix_ImportBatch_LastImportBatchId]
            FOREIGN KEY ([LastImportBatchId]) REFERENCES [dbo].[ImportBatch]([ImportBatchId]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_RegistrationMatrix_LastImportBatchId] ON [dbo].[RegistrationMatrix]([LastImportBatchId]);
END;

IF OBJECT_ID(N'[dbo].[RegistrationMatrix]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[RegistrationMatrix]', N'CreatedByUserId') IS NULL
        ALTER TABLE [dbo].[RegistrationMatrix] ADD [CreatedByUserId] NVARCHAR(450) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RegistrationMatrix_CreatedByUserId' AND object_id = OBJECT_ID(N'[dbo].[RegistrationMatrix]'))
        CREATE INDEX [IX_RegistrationMatrix_CreatedByUserId] ON [dbo].[RegistrationMatrix]([CreatedByUserId]);
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

    private static async Task EnsureWorkflowModuleSchemaAsync(AppDbContext db, ILogger logger)
    {
        const string sql = @"
IF COL_LENGTH(N'[dbo].[ImportBatch]', N'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ImportBatch] ADD [CreatedByUserId] NVARCHAR(450) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ImportBatch_CreatedByUserId' AND object_id = OBJECT_ID(N'[dbo].[ImportBatch]'))
BEGIN
    CREATE INDEX [IX_ImportBatch_CreatedByUserId] ON [dbo].[ImportBatch]([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ImportBatch_AspNetUsers_CreatedByUserId'
)
BEGIN
    ALTER TABLE [dbo].[ImportBatch] WITH CHECK
    ADD CONSTRAINT [FK_ImportBatch_AspNetUsers_CreatedByUserId]
        FOREIGN KEY([CreatedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL;
END;

IF OBJECT_ID(N'[dbo].[WorkflowDefinition]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkflowDefinition](
        [WorkflowDefinitionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [EntityName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_WorkflowDefinition_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL
    );
    CREATE UNIQUE INDEX [IX_WorkflowDefinition_Key] ON [dbo].[WorkflowDefinition]([Key]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowStageDefinition]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkflowStageDefinition](
        [WorkflowStageDefinitionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WorkflowDefinitionId] INT NOT NULL,
        [StageKey] NVARCHAR(100) NOT NULL,
        [StageName] NVARCHAR(200) NOT NULL,
        [DisplayOrder] INT NOT NULL,
        [StageGroupKey] NVARCHAR(100) NULL,
        [StageGroupName] NVARCHAR(200) NULL,
        [ResponsibleRoleId] NVARCHAR(450) NULL,
        [CanEditData] BIT NOT NULL,
        [CanReturn] BIT NOT NULL,
        [CanApprove] BIT NOT NULL,
        [CanProcessBatch] BIT NOT NULL,
        [IsFinalStage] BIT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_WorkflowStageDefinition_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_WorkflowStageDefinition_WorkflowDefinition_WorkflowDefinitionId]
            FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [dbo].[WorkflowDefinition]([WorkflowDefinitionId]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_WorkflowStageDefinition_WorkflowDefinitionId_DisplayOrder] ON [dbo].[WorkflowStageDefinition]([WorkflowDefinitionId], [DisplayOrder]);
    CREATE UNIQUE INDEX [IX_WorkflowStageDefinition_WorkflowDefinitionId_StageKey] ON [dbo].[WorkflowStageDefinition]([WorkflowDefinitionId], [StageKey]);
    CREATE INDEX [IX_WorkflowStageDefinition_ResponsibleRoleId] ON [dbo].[WorkflowStageDefinition]([ResponsibleRoleId]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowStageDefinition]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_WorkflowStageDefinition_AspNetRoles_ResponsibleRoleId')
        ALTER TABLE [dbo].[WorkflowStageDefinition] DROP CONSTRAINT [FK_WorkflowStageDefinition_AspNetRoles_ResponsibleRoleId];
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'StageGroupKey') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [StageGroupKey] NVARCHAR(100) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'StageGroupName') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [StageGroupName] NVARCHAR(200) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'ResponsibleRoleId') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [ResponsibleRoleId] NVARCHAR(450) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'CanReturn') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [CanReturn] BIT NOT NULL CONSTRAINT [DF_WorkflowStageDefinition_CanReturn] DEFAULT (1);
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'CanApprove') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [CanApprove] BIT NOT NULL CONSTRAINT [DF_WorkflowStageDefinition_CanApprove] DEFAULT (1);
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'CanProcessBatch') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [CanProcessBatch] BIT NOT NULL CONSTRAINT [DF_WorkflowStageDefinition_CanProcessBatch] DEFAULT (0);
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'IsFinalStage') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [IsFinalStage] BIT NOT NULL CONSTRAINT [DF_WorkflowStageDefinition_IsFinalStage] DEFAULT (0);
    IF COL_LENGTH(N'[dbo].[WorkflowStageDefinition]', N'UpdatedAt') IS NULL
        ALTER TABLE [dbo].[WorkflowStageDefinition] ADD [UpdatedAt] DATETIME2 NULL;
END;

IF OBJECT_ID(N'[dbo].[WorkflowStageDefinition]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkflowStageDefinition_ResponsibleRoleId' AND object_id = OBJECT_ID(N'[dbo].[WorkflowStageDefinition]'))
BEGIN
    CREATE INDEX [IX_WorkflowStageDefinition_ResponsibleRoleId] ON [dbo].[WorkflowStageDefinition]([ResponsibleRoleId]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowInstance]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkflowInstance](
        [WorkflowInstanceId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WorkflowDefinitionId] INT NOT NULL,
        [ImportBatchId] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [CurrentStageDefinitionId] INT NULL,
        [SubmittedByUserId] NVARCHAR(450) NULL,
        [SubmittedAt] DATETIME2 NULL,
        [CompletedAt] DATETIME2 NULL,
        [LastActionAt] DATETIME2 NULL,
        CONSTRAINT [FK_WorkflowInstance_WorkflowDefinition_WorkflowDefinitionId]
            FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [dbo].[WorkflowDefinition]([WorkflowDefinitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowInstance_ImportBatch_ImportBatchId]
            FOREIGN KEY ([ImportBatchId]) REFERENCES [dbo].[ImportBatch]([ImportBatchId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkflowInstance_WorkflowStageDefinition_CurrentStageDefinitionId]
            FOREIGN KEY ([CurrentStageDefinitionId]) REFERENCES [dbo].[WorkflowStageDefinition]([WorkflowStageDefinitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowInstance_AspNetUsers_SubmittedByUserId]
            FOREIGN KEY ([SubmittedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_WorkflowInstance_ImportBatchId] ON [dbo].[WorkflowInstance]([ImportBatchId]);
    CREATE INDEX [IX_WorkflowInstance_WorkflowDefinitionId] ON [dbo].[WorkflowInstance]([WorkflowDefinitionId]);
    CREATE INDEX [IX_WorkflowInstance_CurrentStageDefinitionId] ON [dbo].[WorkflowInstance]([CurrentStageDefinitionId]);
    CREATE INDEX [IX_WorkflowInstance_SubmittedByUserId] ON [dbo].[WorkflowInstance]([SubmittedByUserId]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowInstance]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[WorkflowInstance]', N'CurrentStageDefinitionId') IS NULL
        ALTER TABLE [dbo].[WorkflowInstance] ADD [CurrentStageDefinitionId] INT NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowInstance]', N'SubmittedByUserId') IS NULL
        ALTER TABLE [dbo].[WorkflowInstance] ADD [SubmittedByUserId] NVARCHAR(450) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowInstance]', N'SubmittedAt') IS NULL
        ALTER TABLE [dbo].[WorkflowInstance] ADD [SubmittedAt] DATETIME2 NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowInstance]', N'CompletedAt') IS NULL
        ALTER TABLE [dbo].[WorkflowInstance] ADD [CompletedAt] DATETIME2 NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowInstance]', N'LastActionAt') IS NULL
        ALTER TABLE [dbo].[WorkflowInstance] ADD [LastActionAt] DATETIME2 NULL;
END;

IF OBJECT_ID(N'[dbo].[WorkflowStageInstance]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkflowStageInstance](
        [WorkflowStageInstanceId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WorkflowInstanceId] INT NOT NULL,
        [WorkflowStageDefinitionId] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [AssignedToUserId] NVARCHAR(450) NULL,
        [ApprovedByUserId] NVARCHAR(450) NULL,
        [StartedAt] DATETIME2 NULL,
        [CompletedAt] DATETIME2 NULL,
        [ReturnedAt] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL,
        CONSTRAINT [FK_WorkflowStageInstance_WorkflowInstance_WorkflowInstanceId]
            FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [dbo].[WorkflowInstance]([WorkflowInstanceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkflowStageInstance_WorkflowStageDefinition_WorkflowStageDefinitionId]
            FOREIGN KEY ([WorkflowStageDefinitionId]) REFERENCES [dbo].[WorkflowStageDefinition]([WorkflowStageDefinitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowStageInstance_AspNetUsers_AssignedToUserId]
            FOREIGN KEY ([AssignedToUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WorkflowStageInstance_AspNetUsers_ApprovedByUserId]
            FOREIGN KEY ([ApprovedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_WorkflowStageInstance_WorkflowInstanceId_WorkflowStageDefinitionId] ON [dbo].[WorkflowStageInstance]([WorkflowInstanceId], [WorkflowStageDefinitionId]);
    CREATE INDEX [IX_WorkflowStageInstance_AssignedToUserId] ON [dbo].[WorkflowStageInstance]([AssignedToUserId]);
    CREATE INDEX [IX_WorkflowStageInstance_ApprovedByUserId] ON [dbo].[WorkflowStageInstance]([ApprovedByUserId]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowStageInstance]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[WorkflowStageInstance]', N'AssignedToUserId') IS NULL
        ALTER TABLE [dbo].[WorkflowStageInstance] ADD [AssignedToUserId] NVARCHAR(450) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageInstance]', N'ApprovedByUserId') IS NULL
        ALTER TABLE [dbo].[WorkflowStageInstance] ADD [ApprovedByUserId] NVARCHAR(450) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageInstance]', N'ReturnedAt') IS NULL
        ALTER TABLE [dbo].[WorkflowStageInstance] ADD [ReturnedAt] DATETIME2 NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowStageInstance]', N'Notes') IS NULL
        ALTER TABLE [dbo].[WorkflowStageInstance] ADD [Notes] NVARCHAR(1000) NULL;
END;

IF OBJECT_ID(N'[dbo].[WorkflowActionLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkflowActionLog](
        [WorkflowActionLogId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WorkflowInstanceId] INT NOT NULL,
        [WorkflowStageInstanceId] INT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [FromStatus] NVARCHAR(30) NULL,
        [ToStatus] NVARCHAR(30) NULL,
        [PerformedByUserId] NVARCHAR(450) NULL,
        [PerformedAt] DATETIME2 NOT NULL,
        [Comments] NVARCHAR(2000) NULL,
        [PayloadJson] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_WorkflowActionLog_WorkflowInstance_WorkflowInstanceId]
            FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [dbo].[WorkflowInstance]([WorkflowInstanceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkflowActionLog_WorkflowStageInstance_WorkflowStageInstanceId]
            FOREIGN KEY ([WorkflowStageInstanceId]) REFERENCES [dbo].[WorkflowStageInstance]([WorkflowStageInstanceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowActionLog_AspNetUsers_PerformedByUserId]
            FOREIGN KEY ([PerformedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_WorkflowActionLog_WorkflowStageInstanceId] ON [dbo].[WorkflowActionLog]([WorkflowStageInstanceId]);
    CREATE INDEX [IX_WorkflowActionLog_PerformedByUserId] ON [dbo].[WorkflowActionLog]([PerformedByUserId]);
END;

IF OBJECT_ID(N'[dbo].[WorkflowActionLog]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[WorkflowActionLog]', N'FromStatus') IS NULL
        ALTER TABLE [dbo].[WorkflowActionLog] ADD [FromStatus] NVARCHAR(30) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowActionLog]', N'ToStatus') IS NULL
        ALTER TABLE [dbo].[WorkflowActionLog] ADD [ToStatus] NVARCHAR(30) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowActionLog]', N'PerformedByUserId') IS NULL
        ALTER TABLE [dbo].[WorkflowActionLog] ADD [PerformedByUserId] NVARCHAR(450) NULL;
    IF COL_LENGTH(N'[dbo].[WorkflowActionLog]', N'PayloadJson') IS NULL
        ALTER TABLE [dbo].[WorkflowActionLog] ADD [PayloadJson] NVARCHAR(MAX) NULL;
END;
";

        await db.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Workflow module schema verified.");
    }

    private static async Task EnsureSeedUserAsync(
        UserManager<ApplicationUser> userMgr,
        string email,
        string password,
        string fullName,
        IReadOnlyCollection<string> roles)
    {
        var user = await userMgr.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var create = await userMgr.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario semilla {email}: {string.Join("; ", create.Errors.Select(x => x.Description))}");
            }
        }
        else
        {
            var changed = false;
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
            {
                user.FullName = fullName;
                changed = true;
            }

            if (changed)
            {
                var update = await userMgr.UpdateAsync(user);
                if (!update.Succeeded)
                {
                    throw new InvalidOperationException($"No se pudo actualizar el usuario semilla {email}: {string.Join("; ", update.Errors.Select(x => x.Description))}");
                }
            }
        }

        var currentRoles = await userMgr.GetRolesAsync(user);
        var missingRoles = roles
            .Where(role => !currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (missingRoles.Length > 0)
        {
            var addRoles = await userMgr.AddToRolesAsync(user, missingRoles);
            if (!addRoles.Succeeded)
            {
                throw new InvalidOperationException($"No se pudieron asignar roles al usuario semilla {email}: {string.Join("; ", addRoles.Errors.Select(x => x.Description))}");
            }
        }
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapControllers();

        app.MapGet("/ping", () => "pong").AllowAnonymous();
        return app;
    }
}
