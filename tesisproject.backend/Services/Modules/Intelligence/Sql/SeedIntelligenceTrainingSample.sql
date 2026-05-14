SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @SeedSource nvarchar(50) = N'IA_TEST_SEED_V1';
DECLARE @StartedAt datetime2 = SYSUTCDATETIME();

DECLARE @ExistingDwArticles TABLE (ArticleKey int PRIMARY KEY);

INSERT INTO @ExistingDwArticles (ArticleKey)
SELECT ArticleKey
FROM [TesisDW_Extensible].[dw].[DimArticle]
WHERE ExternalSource = @SeedSource;

DELETE fai
FROM [TesisDW_Extensible].[dw].[FactArticleIndexing] fai
JOIN @ExistingDwArticles seed ON seed.ArticleKey = fai.ArticleKey;

DELETE faa
FROM [TesisDW_Extensible].[dw].[FactArticleAuthor] faa
JOIN @ExistingDwArticles seed ON seed.ArticleKey = faa.ArticleKey;

DELETE fap
FROM [TesisDW_Extensible].[dw].[FactArticlePublication] fap
JOIN @ExistingDwArticles seed ON seed.ArticleKey = fap.ArticleKey;

DELETE FROM [TesisDW_Extensible].[dw].[DimAuthor]
WHERE ExternalAuthorId LIKE N'IA-SEED-%';

DELETE FROM [TesisDW_Extensible].[dw].[DimArticle]
WHERE ExternalSource = @SeedSource;

DELETE FROM dbo.Articles
WHERE ExternalSource = @SeedSource;

DECLARE @AcademicTermId int = (SELECT TOP 1 AcademicTermId FROM dbo.AcademicTerms ORDER BY AcademicTermId);
DECLARE @PublicationStatusId tinyint = (SELECT TOP 1 PublicationStatusId FROM dbo.PublicationStatuses WHERE Name LIKE N'%Publicado%' ORDER BY PublicationStatusId);
DECLARE @ResearchLineId int = (SELECT TOP 1 ResearchLineId FROM dbo.ResearchLines WHERE Name LIKE N'%Inteligencia%' ORDER BY ResearchLineId);
DECLARE @BroadFieldId int = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields ORDER BY BroadFieldId);
DECLARE @SpecificFieldId int = (SELECT TOP 1 SpecificFieldId FROM dbo.SpecificFields WHERE BroadFieldId = @BroadFieldId ORDER BY SpecificFieldId);
DECLARE @DetailedFieldId int = (SELECT TOP 1 DetailedFieldId FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificFieldId ORDER BY DetailedFieldId);
DECLARE @VenueId int = (SELECT TOP 1 VenueId FROM dbo.Venues WHERE Type = N'Journal' ORDER BY VenueId);
DECLARE @IndexingSourceId int = (SELECT TOP 1 IndexingSourceId FROM dbo.IndexingSources ORDER BY IndexingSourceId);
DECLARE @FacultyId int = (SELECT TOP 1 FacultyId FROM dbo.Faculties ORDER BY FacultyId);

DECLARE @AcademicTermKey int = (SELECT TOP 1 AcademicTermKey FROM [TesisDW_Extensible].[dw].[DimAcademicTerm] ORDER BY AcademicTermKey);
DECLARE @PublicationStatusKey int = (SELECT TOP 1 PublicationStatusKey FROM [TesisDW_Extensible].[dw].[DimPublicationStatus] WHERE Name LIKE N'%Publicado%' ORDER BY PublicationStatusKey);
DECLARE @ResearchLineKey int = (SELECT TOP 1 ResearchLineKey FROM [TesisDW_Extensible].[dw].[DimResearchLine] WHERE Name LIKE N'%Inteligencia%' ORDER BY ResearchLineKey);
DECLARE @FieldHierarchyKey int = (SELECT TOP 1 FieldHierarchyKey FROM [TesisDW_Extensible].[dw].[DimField] ORDER BY FieldHierarchyKey);
DECLARE @VenueKey int = (SELECT TOP 1 VenueKey FROM [TesisDW_Extensible].[dw].[DimVenue] WHERE VenueType = N'Journal' ORDER BY VenueKey);
DECLARE @IndexingSourceKey int = (SELECT TOP 1 IndexingSourceKey FROM [TesisDW_Extensible].[dw].[DimIndexingSource] ORDER BY IndexingSourceKey);
DECLARE @RegistrationSourceKey int = (SELECT TOP 1 RegistrationSourceKey FROM [TesisDW_Extensible].[dw].[DimRegistrationSource] WHERE SourceCode IN (N'MASS_IMPORT', N'BULK_IMPORT') ORDER BY RegistrationSourceKey DESC);
DECLARE @FacultyKey int = (SELECT TOP 1 FacultyKey FROM [TesisDW_Extensible].[dw].[DimFaculty] ORDER BY FacultyKey);

IF @PublicationStatusId IS NULL SET @PublicationStatusId = (SELECT TOP 1 PublicationStatusId FROM dbo.PublicationStatuses ORDER BY PublicationStatusId);
IF @ResearchLineId IS NULL SET @ResearchLineId = (SELECT TOP 1 ResearchLineId FROM dbo.ResearchLines ORDER BY ResearchLineId);
IF @RegistrationSourceKey IS NULL SET @RegistrationSourceKey = 1;

DECLARE @ArticleMap TABLE
(
    Seq int PRIMARY KEY,
    ArticleId int NOT NULL,
    PublishedAt date NOT NULL,
    CreatedAt datetime2 NOT NULL,
    Title nvarchar(500) NOT NULL,
    ExternalId nvarchar(150) NOT NULL
);

DECLARE @i int = 0;

WHILE @i < 24
BEGIN
    DECLARE @PublishedAt date = DATEFROMPARTS(2024 + (@i / 12), (@i % 12) + 1, 15);
    DECLARE @CreatedAt datetime2 = DATEADD(day, 1, CAST(@PublishedAt AS datetime2));
    DECLARE @ExternalId nvarchar(150) = CONCAT(N'IA-SEED-ARTICLE-', FORMAT(@i + 1, '00'));
    DECLARE @Title nvarchar(500) = CONCAT(N'[IA-TEST] Produccion cientifica institucional ', FORMAT(@PublishedAt, 'yyyy-MM'));

    INSERT INTO dbo.Articles
    (
        Title,
        Doi,
        Year,
        PublishedAt,
        PageCount,
        PublicationUrl,
        IsProjectResult,
        HasInterculturalComponent,
        Filiacion,
        VenueId,
        AcademicTermId,
        PublicationStatusId,
        ResearchLineId,
        BroadFieldId,
        SpecificFieldId,
        DetailedFieldId,
        FacultyId,
        IsOpenAccess,
        ExternalSource,
        ExternalId,
        CreatedAt
    )
    VALUES
    (
        @Title,
        CONCAT(N'10.9999/ia-test-', FORMAT(@PublishedAt, 'yyyyMM')),
        YEAR(@PublishedAt),
        @PublishedAt,
        10 + (@i % 9),
        CONCAT(N'https://demo.local/ia-test/', FORMAT(@PublishedAt, 'yyyyMM')),
        CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END,
        CASE WHEN @i % 5 = 0 THEN 1 ELSE 0 END,
        N'Universidad Tecnica de Ambato',
        @VenueId,
        @AcademicTermId,
        @PublicationStatusId,
        @ResearchLineId,
        @BroadFieldId,
        @SpecificFieldId,
        @DetailedFieldId,
        @FacultyId,
        CASE WHEN @i % 4 <> 0 THEN 1 ELSE 0 END,
        @SeedSource,
        @ExternalId,
        @CreatedAt
    );

    INSERT INTO @ArticleMap (Seq, ArticleId, PublishedAt, CreatedAt, Title, ExternalId)
    VALUES (@i + 1, SCOPE_IDENTITY(), @PublishedAt, @CreatedAt, @Title, @ExternalId);

    SET @i += 1;
END;

DECLARE @ParticipantTemplates TABLE
(
    AuthorOrder int PRIMARY KEY,
    Identificacion nvarchar(100),
    Nombre nvarchar(300),
    Participacion nvarchar(150),
    ParticipantType nvarchar(50),
    IsPrimaryAuthor bit,
    Email nvarchar(200),
    Orcid nvarchar(50),
    Affiliation nvarchar(300)
);

INSERT INTO @ParticipantTemplates
VALUES
(1, N'1805358643', N'Christopher Santamaria', N'Autor', N'Docente', 1, N'christopher.santamaria@uta.edu.ec', N'0000-0002-1000-0001', N'Universidad Tecnica de Ambato'),
(2, N'1800000201', N'Ana Rivera Molina', N'Coautor', N'Docente', 0, N'ana.rivera@uta.edu.ec', N'0000-0002-1000-0002', N'Universidad Tecnica de Ambato'),
(3, N'1800000202', N'Luis Paredes Vega', N'Coautor', N'Investigador', 0, N'luis.paredes@uta.edu.ec', N'0000-0002-1000-0003', N'Universidad Tecnica de Ambato'),
(4, N'1800000203', N'Maria Salazar Torres', N'Coautor', N'Docente', 0, N'maria.salazar@uta.edu.ec', N'0000-0002-1000-0004', N'Universidad Tecnica de Ambato'),
(5, N'1800000204', N'Diego Castro Leon', N'Coautor', N'Estudiante', 0, N'diego.castro@uta.edu.ec', N'0000-0002-1000-0005', N'Universidad Tecnica de Ambato');

INSERT INTO dbo.ArticleParticipants
(
    ArticleId,
    [Index],
    Identificacion,
    Nombre,
    Participacion,
    ParticipantType,
    IsPrimaryAuthor,
    Email,
    Orcid,
    Affiliation,
    ExternalAuthorId,
    CreatedAt
)
SELECT
    article.ArticleId,
    template.AuthorOrder,
    template.Identificacion,
    template.Nombre,
    template.Participacion,
    template.ParticipantType,
    template.IsPrimaryAuthor,
    template.Email,
    template.Orcid,
    template.Affiliation,
    CONCAT(N'IA-SEED-', template.AuthorOrder),
    article.CreatedAt
FROM @ArticleMap article
CROSS JOIN @ParticipantTemplates template;

INSERT INTO dbo.ArticleIndexings (ArticleId, IndexingSourceId)
SELECT ArticleId, @IndexingSourceId
FROM @ArticleMap;

INSERT INTO [TesisDW_Extensible].[dw].[DimArticle]
(
    ArticleId_OLTP,
    Title,
    Doi,
    ArticleYear,
    PublicationUrl,
    IsProjectResult,
    HasInterculturalComponent,
    ProceedingsName,
    Proceedings,
    EventName,
    GroupName,
    Filiacion,
    IsOpenAccess,
    ExternalSource,
    ExternalId,
    CreatedAtSource,
    UpdatedAtSource,
    ValidFrom,
    ValidTo,
    IsCurrent
)
SELECT
    article.ArticleId,
    oltp.Title,
    oltp.Doi,
    oltp.Year,
    oltp.PublicationUrl,
    oltp.IsProjectResult,
    oltp.HasInterculturalComponent,
    oltp.ProceedingsName,
    oltp.Proceedings,
    oltp.EventName,
    oltp.GroupName,
    oltp.Filiacion,
    oltp.IsOpenAccess,
    oltp.ExternalSource,
    oltp.ExternalId,
    oltp.CreatedAt,
    oltp.UpdatedAt,
    @StartedAt,
    NULL,
    1
FROM @ArticleMap article
JOIN dbo.Articles oltp ON oltp.Id = article.ArticleId;

INSERT INTO [TesisDW_Extensible].[dw].[DimAuthor]
(
    ArticleParticipantId_OLTP,
    Identificacion,
    Nombre,
    Participacion,
    ParticipantType,
    InstitutionalPersonId,
    Email,
    Orcid,
    Affiliation,
    ExternalAuthorId,
    IsPrimaryAuthor,
    CreatedAtSource,
    UpdatedAtSource,
    ValidFrom,
    ValidTo,
    IsCurrent
)
SELECT
    participant.Id,
    participant.Identificacion,
    participant.Nombre,
    participant.Participacion,
    participant.ParticipantType,
    participant.InstitutionalPersonId,
    participant.Email,
    participant.Orcid,
    participant.Affiliation,
    CONCAT(N'IA-SEED-', participant.ArticleId, N'-', participant.[Index]),
    participant.IsPrimaryAuthor,
    participant.CreatedAt,
    participant.UpdatedAt,
    @StartedAt,
    NULL,
    1
FROM dbo.ArticleParticipants participant
JOIN @ArticleMap article ON article.ArticleId = participant.ArticleId;

INSERT INTO [TesisDW_Extensible].[dw].[FactArticlePublication]
(
    ArticleKey,
    CreatedDateKey,
    PublishedDateKey,
    VenueKey,
    AcademicTermKey,
    PublicationStatusKey,
    ResearchLineKey,
    FieldHierarchyKey,
    RegistrationSourceKey,
    ArticleCount,
    PageCount,
    IsOpenAccessFlag,
    IsProjectResultFlag,
    HasInterculturalFlag,
    FacultyKey
)
SELECT
    dim.ArticleKey,
    CONVERT(int, CONVERT(char(8), CAST(article.CreatedAt AS date), 112)),
    CONVERT(int, CONVERT(char(8), article.PublishedAt, 112)),
    @VenueKey,
    @AcademicTermKey,
    @PublicationStatusKey,
    @ResearchLineKey,
    @FieldHierarchyKey,
    @RegistrationSourceKey,
    1,
    oltp.PageCount,
    oltp.IsOpenAccess,
    oltp.IsProjectResult,
    oltp.HasInterculturalComponent,
    @FacultyKey
FROM @ArticleMap article
JOIN dbo.Articles oltp ON oltp.Id = article.ArticleId
JOIN [TesisDW_Extensible].[dw].[DimArticle] dim ON dim.ArticleId_OLTP = article.ArticleId AND dim.ExternalSource = @SeedSource;

INSERT INTO [TesisDW_Extensible].[dw].[FactArticleAuthor]
(
    ArticleKey,
    AuthorKey,
    DateKey,
    ResearchLineKey,
    FieldHierarchyKey,
    RegistrationSourceKey,
    AuthorCount,
    IsPrimaryAuthorFlag,
    AuthorOrder
)
SELECT
    dim.ArticleKey,
    authorDim.AuthorKey,
    CONVERT(int, CONVERT(char(8), article.PublishedAt, 112)),
    @ResearchLineKey,
    @FieldHierarchyKey,
    @RegistrationSourceKey,
    1,
    participant.IsPrimaryAuthor,
    participant.[Index]
FROM @ArticleMap article
JOIN dbo.ArticleParticipants participant ON participant.ArticleId = article.ArticleId
JOIN [TesisDW_Extensible].[dw].[DimArticle] dim ON dim.ArticleId_OLTP = article.ArticleId AND dim.ExternalSource = @SeedSource
JOIN [TesisDW_Extensible].[dw].[DimAuthor] authorDim ON authorDim.ArticleParticipantId_OLTP = participant.Id;

INSERT INTO [TesisDW_Extensible].[dw].[FactArticleIndexing]
(
    ArticleKey,
    IndexingSourceKey,
    DateKey,
    IndexingCount
)
SELECT
    dim.ArticleKey,
    @IndexingSourceKey,
    CONVERT(int, CONVERT(char(8), article.PublishedAt, 112)),
    1
FROM @ArticleMap article
JOIN [TesisDW_Extensible].[dw].[DimArticle] dim ON dim.ArticleId_OLTP = article.ArticleId AND dim.ExternalSource = @SeedSource;

COMMIT TRANSACTION;

SELECT
    COUNT(*) AS SeededArticles,
    MIN(PublishedAt) AS FirstPublishedAt,
    MAX(PublishedAt) AS LastPublishedAt
FROM @ArticleMap;
