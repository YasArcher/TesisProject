/*
    OLAPDW v1 - Extension de facultades
    Ejecutar sobre la base analitica/DW despues de aplicar:
    docs/database/oltp/20260415_add_faculties_to_oltp.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dw.DimFaculty', N'U') IS NULL
BEGIN
    CREATE TABLE dw.DimFaculty
    (
        FacultyKey INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DimFaculty PRIMARY KEY,
        FacultyId_OLTP INT NOT NULL,
        Code NVARCHAR(40) NULL,
        Name NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtSource DATETIME2 NULL,
        ValidFrom DATETIME2 NOT NULL
            CONSTRAINT DF_DimFaculty_ValidFrom DEFAULT (SYSUTCDATETIME()),
        ValidTo DATETIME2 NULL,
        IsCurrent BIT NOT NULL
            CONSTRAINT DF_DimFaculty_IsCurrent DEFAULT (1)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DimFaculty_Current'
      AND object_id = OBJECT_ID(N'dw.DimFaculty')
)
BEGIN
    CREATE UNIQUE INDEX UX_DimFaculty_Current
        ON dw.DimFaculty(FacultyId_OLTP)
        WHERE IsCurrent = 1;
END;
GO

IF COL_LENGTH(N'dw.FactArticlePublication', N'FacultyKey') IS NULL
BEGIN
    ALTER TABLE dw.FactArticlePublication
        ADD FacultyKey INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FactArticlePublication_FacultyKey'
      AND object_id = OBJECT_ID(N'dw.FactArticlePublication')
)
BEGIN
    CREATE INDEX IX_FactArticlePublication_FacultyKey
        ON dw.FactArticlePublication(FacultyKey);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_FactArticlePublication_DimFaculty'
)
BEGIN
    ALTER TABLE dw.FactArticlePublication WITH CHECK
        ADD CONSTRAINT FK_FactArticlePublication_DimFaculty
        FOREIGN KEY (FacultyKey)
        REFERENCES dw.DimFaculty(FacultyKey);
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Load_DimFaculty
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimFaculty AS T
    USING (
        SELECT
            f.FacultyId,
            f.Code,
            f.Name,
            f.IsActive,
            f.CreatedAt
        FROM [TesisDB_Extensible].dbo.Faculties f
    ) AS S
    ON T.FacultyId_OLTP = S.FacultyId
       AND T.IsCurrent = 1
    WHEN MATCHED THEN
        UPDATE SET
            Code = S.Code,
            Name = S.Name,
            IsActive = S.IsActive,
            CreatedAtSource = S.CreatedAt
    WHEN NOT MATCHED THEN
        INSERT (FacultyId_OLTP, Code, Name, IsActive, CreatedAtSource)
        VALUES (S.FacultyId, S.Code, S.Name, S.IsActive, S.CreatedAt);
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Load_FactArticlePublication
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactArticlePublication;

    INSERT INTO dw.FactArticlePublication (
        ArticleKey, CreatedDateKey, PublishedDateKey, VenueKey, AcademicTermKey,
        PublicationStatusKey, ResearchLineKey, FacultyKey, FieldHierarchyKey, RegistrationSourceKey,
        ArticleCount, PageCount, IsOpenAccessFlag, IsProjectResultFlag, HasInterculturalFlag
    )
    SELECT
        da.ArticleKey,
        etl.fn_DateKey(a.CreatedAt),
        etl.fn_DateKey(a.PublishedAt),
        dv.VenueKey,
        dat.AcademicTermKey,
        dps.PublicationStatusKey,
        drl.ResearchLineKey,
        dfac.FacultyKey,
        df.FieldHierarchyKey,
        drs.RegistrationSourceKey,
        1,
        a.PageCount,
        a.IsOpenAccess,
        a.IsProjectResult,
        a.HasInterculturalComponent
    FROM [TesisDB_Extensible].dbo.Articles a
    INNER JOIN dw.DimArticle da ON da.ArticleId_OLTP = a.Id AND da.IsCurrent = 1
    LEFT JOIN dw.DimVenue dv ON dv.VenueId_OLTP = a.VenueId
    LEFT JOIN dw.DimAcademicTerm dat ON dat.AcademicTermId_OLTP = a.AcademicTermId
    LEFT JOIN dw.DimPublicationStatus dps ON dps.PublicationStatusId_OLTP = a.PublicationStatusId
    LEFT JOIN dw.DimResearchLine drl ON drl.ResearchLineId_OLTP = a.ResearchLineId
    LEFT JOIN dw.DimFaculty dfac ON dfac.FacultyId_OLTP = a.FacultyId AND dfac.IsCurrent = 1
    LEFT JOIN dw.DimField df
        ON ISNULL(df.BroadFieldId_OLTP,-1)=ISNULL(a.BroadFieldId,-1)
       AND ISNULL(df.SpecificFieldId_OLTP,-1)=ISNULL(a.SpecificFieldId,-1)
       AND ISNULL(df.DetailedFieldId_OLTP,-1)=ISNULL(a.DetailedFieldId,-1)
    OUTER APPLY (
        SELECT TOP 1 ib.SourceType
        FROM [TesisDB_Extensible].dbo.ImportBatchRow ibr
        INNER JOIN [TesisDB_Extensible].dbo.ImportBatch ib
            ON ib.ImportBatchId = ibr.ImportBatchId
        WHERE ibr.TargetArticleId = a.Id
        ORDER BY COALESCE(ibr.UpdatedAt, ibr.CreatedAt) DESC
    ) articleSource
    LEFT JOIN dw.DimRegistrationSource drs
        ON drs.SourceCode =
            CASE
                WHEN articleSource.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                WHEN articleSource.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                WHEN articleSource.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                WHEN articleSource.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                WHEN articleSource.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                WHEN a.ExternalSource IS NOT NULL THEN N'EXTERNAL_API'
                ELSE N'MANUAL'
            END;
END;
GO

CREATE OR ALTER VIEW dw.vw_Articles_Detail
AS
SELECT
    f.FactArticlePublicationId,
    da.ArticleKey,
    da.ArticleId_OLTP,
    da.Title,
    da.Doi,
    da.ArticleYear,
    da.PublicationUrl,
    da.IsOpenAccess,
    da.IsProjectResult,
    da.HasInterculturalComponent,
    dv.Name AS VenueName,
    dv.VenueType,
    dps.Name AS PublicationStatus,
    dat.Name AS AcademicTerm,
    drl.Name AS ResearchLine,
    dfac.Name AS FacultyName,
    df.BroadFieldName,
    df.SpecificFieldName,
    df.DetailedFieldName,
    dc.FullDate AS CreatedDate,
    dp.FullDate AS PublishedDate,
    f.PageCount,
    f.ArticleCount
FROM dw.FactArticlePublication f
INNER JOIN dw.DimArticle da
    ON da.ArticleKey = f.ArticleKey
LEFT JOIN dw.DimVenue dv
    ON dv.VenueKey = f.VenueKey
LEFT JOIN dw.DimPublicationStatus dps
    ON dps.PublicationStatusKey = f.PublicationStatusKey
LEFT JOIN dw.DimAcademicTerm dat
    ON dat.AcademicTermKey = f.AcademicTermKey
LEFT JOIN dw.DimResearchLine drl
    ON drl.ResearchLineKey = f.ResearchLineKey
LEFT JOIN dw.DimFaculty dfac
    ON dfac.FacultyKey = f.FacultyKey
LEFT JOIN dw.DimField df
    ON df.FieldHierarchyKey = f.FieldHierarchyKey
LEFT JOIN dw.DimDate dc
    ON dc.DateKey = f.CreatedDateKey
LEFT JOIN dw.DimDate dp
    ON dp.DateKey = f.PublishedDateKey;
GO

CREATE OR ALTER VIEW dw.vw_Articles_ByFaculty
AS
SELECT
    ISNULL(NULLIF(LTRIM(RTRIM(dfac.Name)), N''), N'Sin facultad') AS Name,
    SUM(f.ArticleCount) AS TotalArticles
FROM dw.FactArticlePublication f
LEFT JOIN dw.DimFaculty dfac
    ON dfac.FacultyKey = f.FacultyKey
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(dfac.Name)), N''), N'Sin facultad');
GO

CREATE OR ALTER PROCEDURE etl.sp_RunFullLoad
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
        EXEC etl.sp_Load_DimFaculty;
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
            Status = N'Success',
            Notes = N'Carga completa finalizada correctamente'
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
