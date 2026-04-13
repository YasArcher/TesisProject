USE [TesisDW_Extensible]
GO

/* =========================================================
   OLAPDW v1 - Incremental para una base DW existente
   Uso: aplicar cuando TesisDW_Extensible ya tiene una version previa.
   ========================================================= */

IF OBJECT_ID(N'dw.DimIndexingSource', N'U') IS NULL
BEGIN
    CREATE TABLE [dw].[DimIndexingSource](
        [IndexingSourceKey] [int] IDENTITY(1,1) NOT NULL,
        [IndexingSourceId_OLTP] [int] NOT NULL,
        [Name] [nvarchar](120) NOT NULL,
        [IsActive] [bit] NOT NULL,
        CONSTRAINT [PK_DimIndexingSource] PRIMARY KEY CLUSTERED ([IndexingSourceKey] ASC)
    );
END
GO

IF OBJECT_ID(N'dw.FactArticleIndexing', N'U') IS NULL
BEGIN
    CREATE TABLE [dw].[FactArticleIndexing](
        [FactArticleIndexingId] [bigint] IDENTITY(1,1) NOT NULL,
        [ArticleKey] [int] NOT NULL,
        [IndexingSourceKey] [int] NOT NULL,
        [DateKey] [int] NOT NULL,
        [IndexingCount] [int] NOT NULL,
        CONSTRAINT [PK_FactArticleIndexing] PRIMARY KEY CLUSTERED ([FactArticleIndexingId] ASC)
    );

    ALTER TABLE [dw].[FactArticleIndexing]
        ADD CONSTRAINT [DF_FactArticleIndexing_IndexingCount] DEFAULT ((1)) FOR [IndexingCount];

    ALTER TABLE [dw].[FactArticleIndexing] WITH CHECK
        ADD CONSTRAINT [FK_FactArticleIndexing_DimArticle] FOREIGN KEY([ArticleKey])
        REFERENCES [dw].[DimArticle] ([ArticleKey]);

    ALTER TABLE [dw].[FactArticleIndexing] WITH CHECK
        ADD CONSTRAINT [FK_FactArticleIndexing_DimDate] FOREIGN KEY([DateKey])
        REFERENCES [dw].[DimDate] ([DateKey]);

    ALTER TABLE [dw].[FactArticleIndexing] WITH CHECK
        ADD CONSTRAINT [FK_FactArticleIndexing_DimIndexingSource] FOREIGN KEY([IndexingSourceKey])
        REFERENCES [dw].[DimIndexingSource] ([IndexingSourceKey]);
END
GO

IF COL_LENGTH(N'dw.FactWorkflowStage', N'BatchId_OLTP') IS NULL
BEGIN
    ALTER TABLE [dw].[FactWorkflowStage] ADD [BatchId_OLTP] [int] NULL;
END
GO

IF COL_LENGTH(N'dw.FactWorkflowAction', N'BatchId_OLTP') IS NULL
BEGIN
    ALTER TABLE [dw].[FactWorkflowAction] ADD [BatchId_OLTP] [int] NULL;
END
GO

IF COL_LENGTH(N'dw.FactWorkflowStage', N'BatchId_OLTP') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dw].[FactWorkflowStage] WHERE [BatchId_OLTP] IS NULL)
BEGIN
    ALTER TABLE [dw].[FactWorkflowStage] ALTER COLUMN [BatchId_OLTP] [int] NOT NULL;
END
GO

IF COL_LENGTH(N'dw.FactWorkflowAction', N'BatchId_OLTP') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dw].[FactWorkflowAction] WHERE [BatchId_OLTP] IS NULL)
BEGIN
    ALTER TABLE [dw].[FactWorkflowAction] ALTER COLUMN [BatchId_OLTP] [int] NOT NULL;
END
GO

CREATE OR ALTER VIEW [dw].[vw_Articles_ByIndexingSource]
AS
SELECT
    dis.Name AS IndexingSourceName,
    SUM(f.IndexingCount) AS TotalArticles
FROM dw.FactArticleIndexing f
INNER JOIN dw.DimIndexingSource dis
    ON dis.IndexingSourceKey = f.IndexingSourceKey
GROUP BY dis.Name;
GO

CREATE OR ALTER VIEW [dw].[vw_Authors_UniqueProduction]
AS
SELECT
    COALESCE(NULLIF(LTRIM(RTRIM(da.Orcid)), ''),
             NULLIF(LTRIM(RTRIM(da.Identificacion)), ''),
             NULLIF(LTRIM(RTRIM(da.Email)), ''),
             UPPER(LTRIM(RTRIM(da.Nombre)))) AS AuthorIdentity,
    MAX(da.Nombre) AS AuthorName,
    MAX(da.Email) AS Email,
    MAX(da.Orcid) AS Orcid,
    COUNT(DISTINCT f.ArticleKey) AS TotalArticles,
    SUM(CASE WHEN f.IsPrimaryAuthorFlag = 1 THEN 1 ELSE 0 END) AS AsPrimaryAuthor
FROM dw.FactArticleAuthor f
INNER JOIN dw.DimAuthor da
    ON da.AuthorKey = f.AuthorKey
GROUP BY
    COALESCE(NULLIF(LTRIM(RTRIM(da.Orcid)), ''),
             NULLIF(LTRIM(RTRIM(da.Identificacion)), ''),
             NULLIF(LTRIM(RTRIM(da.Email)), ''),
             UPPER(LTRIM(RTRIM(da.Nombre))));
GO

CREATE OR ALTER VIEW [dw].[vw_Workflow_CurrentPipeline]
AS
SELECT
    f.BatchId_OLTP,
    dwf.WorkflowName,
    dws.StageName,
    dws.StageGroupName,
    dbs.StatusName AS StageStatus,
    f.StartDateKey,
    f.EndDateKey,
    assigned.FullName AS AssignedTo,
    approved.FullName AS ApprovedBy,
    f.StageDurationSeconds,
    f.ApprovedFlag,
    f.ReturnedFlag
FROM dw.FactWorkflowStage f
INNER JOIN dw.DimWorkflow dwf
    ON dwf.WorkflowKey = f.WorkflowKey
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = f.WorkflowStageKey
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = f.BatchStatusKey
LEFT JOIN dw.DimUser assigned
    ON assigned.UserKey = f.AssignedUserKey
LEFT JOIN dw.DimUser approved
    ON approved.UserKey = f.ApprovedByUserKey;
GO

CREATE OR ALTER VIEW [dw].[vw_Workflow_Batches_ByCurrentStage]
AS
WITH RankedStages AS (
    SELECT
        f.*,
        ROW_NUMBER() OVER (
            PARTITION BY f.BatchId_OLTP
            ORDER BY
                CASE WHEN f.EndDateKey IS NULL THEN 0 ELSE 1 END,
                f.EndDateKey DESC,
                f.StartDateKey DESC,
                f.FactWorkflowStageId DESC
        ) AS rn
    FROM dw.FactWorkflowStage f
)
SELECT
    rs.BatchId_OLTP,
    dwf.WorkflowName,
    dws.StageName,
    dws.StageGroupName,
    dbs.StatusName AS StageStatus,
    rs.StartDateKey,
    rs.EndDateKey,
    rs.StageDurationSeconds,
    rs.ApprovedFlag,
    rs.ReturnedFlag
FROM RankedStages rs
INNER JOIN dw.DimWorkflow dwf
    ON dwf.WorkflowKey = rs.WorkflowKey
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = rs.WorkflowStageKey
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = rs.BatchStatusKey
WHERE rs.rn = 1;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_PopulateDimDate]
    @StartDate DATE = '20000101',
    @EndDate   DATE = '20501231'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH d AS (
        SELECT @StartDate AS FullDate
        UNION ALL
        SELECT DATEADD(DAY, 1, FullDate)
        FROM d
        WHERE FullDate < @EndDate
    )
    INSERT INTO dw.DimDate (
        DateKey, FullDate, DayNumber, DayName, WeekNumber, MonthNumber,
        MonthName, QuarterNumber, SemesterNumber, YearNumber, IsWeekend
    )
    SELECT
        CAST(CONVERT(CHAR(8), d.FullDate, 112) AS INT),
        d.FullDate,
        DATEPART(DAY, d.FullDate),
        DATENAME(WEEKDAY, d.FullDate),
        DATEPART(ISO_WEEK, d.FullDate),
        DATEPART(MONTH, d.FullDate),
        DATENAME(MONTH, d.FullDate),
        DATEPART(QUARTER, d.FullDate),
        CASE WHEN DATEPART(MONTH, d.FullDate) <= 6 THEN 1 ELSE 2 END,
        DATEPART(YEAR, d.FullDate),
        CASE WHEN DATEPART(WEEKDAY, d.FullDate) IN (1,7) THEN 1 ELSE 0 END
    FROM d
    WHERE NOT EXISTS (
        SELECT 1
        FROM dw.DimDate x
        WHERE x.FullDate = d.FullDate
    )
    OPTION (MAXRECURSION 0);
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_DimIndexingSource]
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'[TesisDB_Extensible].dbo.IndexingSources', N'U') IS NULL
        RETURN;

    MERGE dw.DimIndexingSource AS T
    USING (
        SELECT IndexingSourceId, Name, IsActive
        FROM [TesisDB_Extensible].dbo.IndexingSources
    ) AS S
    ON T.IndexingSourceId_OLTP = S.IndexingSourceId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name, IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (IndexingSourceId_OLTP, Name, IsActive)
        VALUES (S.IndexingSourceId, S.Name, S.IsActive);
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_DimRegistrationSource]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimRegistrationSource AS T
    USING (
        SELECT
            SourceCode,
            MAX(SourceName) AS SourceName,
            MAX(SourceCategory) AS SourceCategory
        FROM (
            SELECT N'MANUAL' AS SourceCode, N'Registro manual' AS SourceName, N'Manual' AS SourceCategory
            UNION ALL SELECT N'API', N'API externa', N'Automatizado'
            UNION ALL SELECT N'AUTHOR_SINGLE', N'Registro individual de autor', N'Autor'
            UNION ALL SELECT N'AUTHOR_MATRIX', N'Matriz de autor', N'Autor'
            UNION ALL SELECT N'ADMIN_BULK', N'Carga masiva administrativa', N'Administrador'
            UNION ALL SELECT N'MASS_IMPORT', N'Carga masiva', N'Masivo'
            UNION ALL SELECT N'BULK_IMPORT', N'Carga masiva', N'Masivo'
            UNION ALL SELECT N'EXTERNAL_API', N'API externa', N'Automatizado'
            UNION ALL SELECT N'MIGRATION', N'Migracion historica', N'Migracion'
            UNION ALL
            SELECT DISTINCT
                NULLIF(LTRIM(RTRIM(SourceType)), N'') AS SourceCode,
                NULLIF(LTRIM(RTRIM(SourceType)), N'') AS SourceName,
                N'OLTP' AS SourceCategory
            FROM [TesisDB_Extensible].dbo.ImportBatch
            WHERE NULLIF(LTRIM(RTRIM(SourceType)), N'') IS NOT NULL
        ) AS RawSources
        GROUP BY SourceCode
    ) AS S
    ON T.SourceCode = S.SourceCode
    WHEN MATCHED THEN
        UPDATE SET SourceName = S.SourceName, SourceCategory = S.SourceCategory
    WHEN NOT MATCHED THEN
        INSERT (SourceCode, SourceName, SourceCategory)
        VALUES (S.SourceCode, S.SourceName, S.SourceCategory);
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_DimBatchStatus]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimBatchStatus AS T
    USING (
        SELECT StatusCode, MAX(StatusName) AS StatusName
        FROM (
            SELECT N'Draft' AS StatusCode, N'Borrador' AS StatusName
            UNION ALL SELECT N'Pending', N'Pendiente'
            UNION ALL SELECT N'InReview', N'En revision'
            UNION ALL SELECT N'Approved', N'Aprobado'
            UNION ALL SELECT N'Returned', N'Devuelto'
            UNION ALL SELECT N'Processed', N'Procesado'
            UNION ALL SELECT N'Completed', N'Completado'
            UNION ALL SELECT N'Failed', N'Fallido'
            UNION ALL SELECT N'Valid', N'Valido'
            UNION ALL SELECT N'Error', N'Con error'
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.ImportBatch
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(RowStatus)), N''), NULLIF(LTRIM(RTRIM(RowStatus)), N'')
            FROM [TesisDB_Extensible].dbo.ImportBatchRow
            WHERE NULLIF(LTRIM(RTRIM(RowStatus)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.RegistrationMatrix
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.WorkflowInstance
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.WorkflowStageInstance
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
        ) AS RawStatuses
        WHERE StatusCode IS NOT NULL
        GROUP BY StatusCode
    ) AS S
    ON T.StatusCode = S.StatusCode
    WHEN MATCHED THEN
        UPDATE SET StatusName = S.StatusName
    WHEN NOT MATCHED THEN
        INSERT (StatusCode, StatusName)
        VALUES (S.StatusCode, S.StatusName);
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_FactArticleIndexing]
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'[TesisDB_Extensible].dbo.ArticleIndexings', N'U') IS NULL
        RETURN;

    TRUNCATE TABLE dw.FactArticleIndexing;

    INSERT INTO dw.FactArticleIndexing (
        ArticleKey, IndexingSourceKey, DateKey, IndexingCount
    )
    SELECT
        da.ArticleKey,
        dis.IndexingSourceKey,
        etl.fn_DateKey(a.CreatedAt),
        1
    FROM [TesisDB_Extensible].dbo.ArticleIndexings ai
    INNER JOIN [TesisDB_Extensible].dbo.Articles a
        ON a.Id = ai.ArticleId
    INNER JOIN dw.DimArticle da
        ON da.ArticleId_OLTP = ai.ArticleId AND da.IsCurrent = 1
    INNER JOIN dw.DimIndexingSource dis
        ON dis.IndexingSourceId_OLTP = ai.IndexingSourceId;
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_FactWorkflowAction]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactWorkflowAction AS T
    USING (
        SELECT
            wal.WorkflowActionLogId,
            wal.WorkflowInstanceId,
            wal.WorkflowStageInstanceId,
            wi.ImportBatchId,
            dwf.WorkflowKey,
            dws.WorkflowStageKey,
            etl.fn_DateKey(wal.PerformedAt) AS DateKey,
            du.UserKey,
            dbs.BatchStatusKey,
            wal.ActionType,
            wal.FromStatus,
            wal.ToStatus
        FROM [TesisDB_Extensible].dbo.WorkflowActionLog wal
        INNER JOIN [TesisDB_Extensible].dbo.WorkflowInstance wi
            ON wi.WorkflowInstanceId = wal.WorkflowInstanceId
        INNER JOIN dw.DimWorkflow dwf
            ON dwf.WorkflowDefinitionId_OLTP = wi.WorkflowDefinitionId
        LEFT JOIN [TesisDB_Extensible].dbo.WorkflowStageInstance wsi
            ON wsi.WorkflowStageInstanceId = wal.WorkflowStageInstanceId
        LEFT JOIN dw.DimWorkflowStage dws
            ON dws.WorkflowStageDefinitionId_OLTP = wsi.WorkflowStageDefinitionId
        LEFT JOIN dw.DimUser du
            ON du.UserId_OLTP = wal.PerformedByUserId
        LEFT JOIN dw.DimBatchStatus dbs
            ON dbs.StatusCode = wal.ToStatus
    ) AS S
    ON T.WorkflowActionLogId_OLTP = S.WorkflowActionLogId
    WHEN MATCHED THEN
        UPDATE SET WorkflowInstanceId_OLTP = S.WorkflowInstanceId,
                   WorkflowStageInstanceId_OLTP = S.WorkflowStageInstanceId,
                   BatchId_OLTP = S.ImportBatchId,
                   WorkflowKey = S.WorkflowKey,
                   WorkflowStageKey = S.WorkflowStageKey,
                   DateKey = S.DateKey,
                   UserKey = S.UserKey,
                   BatchStatusKey = S.BatchStatusKey,
                   ActionType = S.ActionType,
                   FromStatus = S.FromStatus,
                   ToStatus = S.ToStatus
    WHEN NOT MATCHED THEN
        INSERT (
            WorkflowActionLogId_OLTP, WorkflowInstanceId_OLTP, WorkflowStageInstanceId_OLTP,
            BatchId_OLTP, WorkflowKey, WorkflowStageKey, DateKey, UserKey, BatchStatusKey,
            ActionType, FromStatus, ToStatus, ActionCount
        )
        VALUES (
            S.WorkflowActionLogId, S.WorkflowInstanceId, S.WorkflowStageInstanceId,
            S.ImportBatchId, S.WorkflowKey, S.WorkflowStageKey, S.DateKey, S.UserKey, S.BatchStatusKey,
            S.ActionType, S.FromStatus, S.ToStatus, 1
        );
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_Load_FactWorkflowStage]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactWorkflowStage AS T
    USING (
        SELECT
            wsi.WorkflowStageInstanceId,
            wi.WorkflowInstanceId,
            wi.ImportBatchId,
            dwf.WorkflowKey,
            dws.WorkflowStageKey,
            etl.fn_DateKey(wsi.StartedAt) AS StartDateKey,
            etl.fn_DateKey(wsi.CompletedAt) AS EndDateKey,
            du1.UserKey AS AssignedUserKey,
            du2.UserKey AS ApprovedByUserKey,
            dbs.BatchStatusKey,
            DATEDIFF(SECOND, wsi.StartedAt, wsi.CompletedAt) AS StageDurationSeconds,
            CAST(CASE WHEN wsi.ApprovedByUserId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ApprovedFlag,
            CAST(CASE WHEN wsi.ReturnedAt IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ReturnedFlag
        FROM [TesisDB_Extensible].dbo.WorkflowStageInstance wsi
        INNER JOIN [TesisDB_Extensible].dbo.WorkflowInstance wi
            ON wi.WorkflowInstanceId = wsi.WorkflowInstanceId
        INNER JOIN dw.DimWorkflow dwf
            ON dwf.WorkflowDefinitionId_OLTP = wi.WorkflowDefinitionId
        INNER JOIN dw.DimWorkflowStage dws
            ON dws.WorkflowStageDefinitionId_OLTP = wsi.WorkflowStageDefinitionId
        LEFT JOIN dw.DimUser du1
            ON du1.UserId_OLTP = wsi.AssignedToUserId
        LEFT JOIN dw.DimUser du2
            ON du2.UserId_OLTP = wsi.ApprovedByUserId
        LEFT JOIN dw.DimBatchStatus dbs
            ON dbs.StatusCode = wsi.Status
    ) AS S
    ON T.WorkflowStageInstanceId_OLTP = S.WorkflowStageInstanceId
    WHEN MATCHED THEN
        UPDATE SET WorkflowInstanceId_OLTP = S.WorkflowInstanceId,
                   BatchId_OLTP = S.ImportBatchId,
                   WorkflowKey = S.WorkflowKey,
                   WorkflowStageKey = S.WorkflowStageKey,
                   StartDateKey = S.StartDateKey,
                   EndDateKey = S.EndDateKey,
                   AssignedUserKey = S.AssignedUserKey,
                   ApprovedByUserKey = S.ApprovedByUserKey,
                   BatchStatusKey = S.BatchStatusKey,
                   StageDurationSeconds = S.StageDurationSeconds,
                   ApprovedFlag = S.ApprovedFlag,
                   ReturnedFlag = S.ReturnedFlag
    WHEN NOT MATCHED THEN
        INSERT (
            WorkflowInstanceId_OLTP, WorkflowStageInstanceId_OLTP, BatchId_OLTP, WorkflowKey, WorkflowStageKey,
            StartDateKey, EndDateKey, AssignedUserKey, ApprovedByUserKey, BatchStatusKey,
            StageCount, StageDurationSeconds, ApprovedFlag, ReturnedFlag
        )
        VALUES (
            S.WorkflowInstanceId, S.WorkflowStageInstanceId, S.ImportBatchId, S.WorkflowKey, S.WorkflowStageKey,
            S.StartDateKey, S.EndDateKey, S.AssignedUserKey, S.ApprovedByUserKey, S.BatchStatusKey,
            1, S.StageDurationSeconds, S.ApprovedFlag, S.ReturnedFlag
        );
END;
GO

CREATE OR ALTER PROCEDURE [etl].[sp_RunFullLoad]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RunId BIGINT;

    INSERT INTO etl.EtlRun (ProcessName, StartedAt, Status, Notes)
    VALUES (N'FULL_DW_LOAD', SYSUTCDATETIME(), N'Running', N'Carga completa del DW');
    SET @RunId = SCOPE_IDENTITY();

    BEGIN TRY
        EXEC etl.sp_PopulateDimDate;

        EXEC etl.sp_Load_DimUser;
        EXEC etl.sp_Load_DimSystemRole;
        EXEC etl.sp_Load_DimRegistrationSource;
        EXEC etl.sp_Load_DimBatchStatus;
        EXEC etl.sp_Load_DimVenue;
        EXEC etl.sp_Load_DimAcademicTerm;
        EXEC etl.sp_Load_DimPublicationStatus;
        EXEC etl.sp_Load_DimIndexingSource;
        EXEC etl.sp_Load_DimResearchLine;
        EXEC etl.sp_Load_DimField;
        EXEC etl.sp_Load_DimWorkflow;
        EXEC etl.sp_Load_DimWorkflowStage;
        EXEC etl.sp_Load_DimDynamicField;
        EXEC etl.sp_Load_DimRegistrationMatrix;
        EXEC etl.sp_Load_DimValidationError;
        EXEC etl.sp_Load_DimArticle;
        EXEC etl.sp_Load_DimAuthor;

        EXEC etl.sp_Load_FactArticlePublication;
        EXEC etl.sp_Load_FactArticleAuthor;
        EXEC etl.sp_Load_FactArticleIndexing;
        EXEC etl.sp_Load_FactVenueMetricYear;
        EXEC etl.sp_Load_FactRegistrationBatch;
        EXEC etl.sp_Load_FactRegistrationRow;
        EXEC etl.sp_Load_FactValidationError;
        EXEC etl.sp_Load_FactWorkflowStage;
        EXEC etl.sp_Load_FactWorkflowAction;
        EXEC etl.sp_Load_FactRegistrationMatrix;
        EXEC etl.sp_Load_FactArticleDynamicAttribute;
        EXEC etl.sp_Load_FactParticipantDynamicAttribute;

        UPDATE etl.EtlRun
        SET FinishedAt = SYSUTCDATETIME(),
            Status = N'Success'
        WHERE EtlRunId = @RunId;
    END TRY
    BEGIN CATCH
        UPDATE etl.EtlRun
        SET FinishedAt = SYSUTCDATETIME(),
            Status = N'Failed',
            Notes = ERROR_MESSAGE()
        WHERE EtlRunId = @RunId;

        THROW;
    END CATCH
END;
GO

PRINT N'Incremental OLAPDW v1 aplicado.';
GO
