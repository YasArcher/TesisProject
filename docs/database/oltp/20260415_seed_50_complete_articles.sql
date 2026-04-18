/*
    Semilla institucional para reportería
    Crea 50 artículos completos con venues, métricas, facultades,
    participantes e indexaciones.

    Base destino: TesisDB_Extensible.
    Idempotente por ExternalSource + ExternalId.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Source NVARCHAR(80) = N'SEED_REPORTING_20260415';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

/* Catalogos mínimos de respaldo si la base está incompleta. */
IF NOT EXISTS (SELECT 1 FROM dbo.AcademicTerms WHERE Name = N'2026-I')
    INSERT INTO dbo.AcademicTerms (Name) VALUES (N'2026-I');

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicTerms WHERE Name = N'2026-II')
    INSERT INTO dbo.AcademicTerms (Name) VALUES (N'2026-II');

IF NOT EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 1)
    INSERT INTO dbo.PublicationStatuses (PublicationStatusId, Name) VALUES (1, N'Registrado');

IF NOT EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 2)
    INSERT INTO dbo.PublicationStatuses (PublicationStatusId, Name) VALUES (2, N'Publicado');

IF NOT EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 3)
    INSERT INTO dbo.PublicationStatuses (PublicationStatusId, Name) VALUES (3, N'En revision');

IF NOT EXISTS (SELECT 1 FROM dbo.ResearchLines WHERE Name = N'Tecnologias de la informacion y transformacion digital')
    INSERT INTO dbo.ResearchLines (Name) VALUES (N'Tecnologias de la informacion y transformacion digital');

IF NOT EXISTS (SELECT 1 FROM dbo.ResearchLines WHERE Name = N'Salud, bienestar y sociedad')
    INSERT INTO dbo.ResearchLines (Name) VALUES (N'Salud, bienestar y sociedad');

IF NOT EXISTS (SELECT 1 FROM dbo.ResearchLines WHERE Name = N'Innovacion productiva y sostenibilidad')
    INSERT INTO dbo.ResearchLines (Name) VALUES (N'Innovacion productiva y sostenibilidad');

IF NOT EXISTS (SELECT 1 FROM dbo.BroadFields WHERE Name = N'Ciencias de la computacion e informacion')
    INSERT INTO dbo.BroadFields (Name) VALUES (N'Ciencias de la computacion e informacion');

IF NOT EXISTS (SELECT 1 FROM dbo.BroadFields WHERE Name = N'Ciencias medicas y de la salud')
    INSERT INTO dbo.BroadFields (Name) VALUES (N'Ciencias medicas y de la salud');

IF NOT EXISTS (SELECT 1 FROM dbo.BroadFields WHERE Name = N'Ciencias sociales')
    INSERT INTO dbo.BroadFields (Name) VALUES (N'Ciencias sociales');

DECLARE @BroadComputing INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias de la computacion e informacion');
DECLARE @BroadHealth INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias medicas y de la salud');
DECLARE @BroadSocial INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias sociales');

IF NOT EXISTS (SELECT 1 FROM dbo.SpecificFields WHERE BroadFieldId = @BroadComputing AND Code = N'CS01')
    INSERT INTO dbo.SpecificFields (BroadFieldId, Code, Name) VALUES (@BroadComputing, N'CS01', N'Sistemas de informacion');

IF NOT EXISTS (SELECT 1 FROM dbo.SpecificFields WHERE BroadFieldId = @BroadHealth AND Code = N'HS01')
    INSERT INTO dbo.SpecificFields (BroadFieldId, Code, Name) VALUES (@BroadHealth, N'HS01', N'Salud publica');

IF NOT EXISTS (SELECT 1 FROM dbo.SpecificFields WHERE BroadFieldId = @BroadSocial AND Code = N'SS01')
    INSERT INTO dbo.SpecificFields (BroadFieldId, Code, Name) VALUES (@BroadSocial, N'SS01', N'Educacion y sociedad');

DECLARE @SpecificComputing INT = (SELECT TOP 1 SpecificFieldId FROM dbo.SpecificFields WHERE BroadFieldId = @BroadComputing AND Code = N'CS01');
DECLARE @SpecificHealth INT = (SELECT TOP 1 SpecificFieldId FROM dbo.SpecificFields WHERE BroadFieldId = @BroadHealth AND Code = N'HS01');
DECLARE @SpecificSocial INT = (SELECT TOP 1 SpecificFieldId FROM dbo.SpecificFields WHERE BroadFieldId = @BroadSocial AND Code = N'SS01');

IF NOT EXISTS (SELECT 1 FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificComputing AND Code = N'CS0101')
    INSERT INTO dbo.DetailedFields (SpecificFieldId, Code, Name) VALUES (@SpecificComputing, N'CS0101', N'Analitica de datos e inteligencia artificial');

IF NOT EXISTS (SELECT 1 FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificHealth AND Code = N'HS0101')
    INSERT INTO dbo.DetailedFields (SpecificFieldId, Code, Name) VALUES (@SpecificHealth, N'HS0101', N'Epidemiologia aplicada');

IF NOT EXISTS (SELECT 1 FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificSocial AND Code = N'SS0101')
    INSERT INTO dbo.DetailedFields (SpecificFieldId, Code, Name) VALUES (@SpecificSocial, N'SS0101', N'Innovacion educativa');

DECLARE @Venues TABLE
(
    RowNum INT IDENTITY(1,1),
    Name NVARCHAR(200),
    IssnCode NVARCHAR(20),
    VenueType NVARCHAR(20),
    JournalUrl NVARCHAR(400),
    Quartile NVARCHAR(10),
    Sjr DECIMAL(6,3),
    CiteScore DECIMAL(8,3),
    HIndex INT
);

INSERT INTO @Venues (Name, IssnCode, VenueType, JournalUrl, Quartile, Sjr, CiteScore, HIndex)
VALUES
    (N'Revista Andina de Investigacion Aplicada', N'2600-1001', N'Journal', N'https://revistas.uta.edu.ec/andina-aplicada', N'Q2', 0.842, 3.900, 42),
    (N'Journal of Digital Transformation in Higher Education', N'2600-1002', N'Journal', N'https://journals.example.edu/digital-higher-ed', N'Q1', 1.214, 5.600, 58),
    (N'Latin American Journal of Data Science', N'2600-1003', N'Journal', N'https://journals.example.edu/lajds', N'Q2', 0.936, 4.200, 47),
    (N'Revista de Salud Publica e Innovacion Social', N'2600-1004', N'Journal', N'https://revistas.example.edu/salud-innovacion', N'Q3', 0.511, 2.100, 29),
    (N'Ingenieria, Industria y Sostenibilidad', N'2600-1005', N'Journal', N'https://revistas.example.edu/iis', N'Q2', 0.768, 3.400, 36),
    (N'Educacion, Cultura y Territorio', N'2600-1006', N'Journal', N'https://revistas.example.edu/educacion-territorio', N'Q4', 0.231, 1.300, 18),
    (N'Computacion Aplicada y Sistemas Inteligentes', N'2600-1007', N'Journal', N'https://revistas.example.edu/casi', N'Q1', 1.487, 6.300, 64),
    (N'Biotecnologia y Produccion Cientifica', N'2600-1008', N'Journal', N'https://revistas.example.edu/biotec', N'Q2', 0.889, 3.700, 39),
    (N'Revista Latinoamericana de Gestion del Conocimiento', N'2600-1009', N'Journal', N'https://revistas.example.edu/gestion-conocimiento', N'Q3', 0.402, 2.000, 24),
    (N'Open Science and Regional Development', N'2600-1010', N'Journal', N'https://journals.example.edu/osrd', N'Q1', 1.102, 5.000, 51),
    (N'Proceedings de Innovacion Universitaria', N'2600-1011', N'Conference', N'https://conferences.example.edu/innovacion-universitaria', N'Q4', 0.198, 1.100, 12),
    (N'Revista Institucional de Investigacion UTA', N'2600-1012', N'Journal', N'https://revistas.uta.edu.ec/investigacion', N'Q3', 0.456, 2.400, 27);

MERGE dbo.Venues AS T
USING @Venues AS S
ON T.Name = S.Name
WHEN MATCHED THEN
    UPDATE SET
        IssnCode = S.IssnCode,
        Type = S.VenueType,
        JournalUrl = S.JournalUrl
WHEN NOT MATCHED THEN
    INSERT (Name, IssnCode, Type, JournalUrl)
    VALUES (S.Name, S.IssnCode, S.VenueType, S.JournalUrl);

INSERT INTO dbo.VenueMetrics (VenueId, Year, SJR, Quartile, CiteScore, HIndex, SourceNote, CreatedAt)
SELECT
    v.VenueId,
    y.[Year],
    CAST(seed.Sjr + ((y.[Year] - 2022) * 0.031) AS DECIMAL(6,3)),
    seed.Quartile,
    CAST(seed.CiteScore + ((y.[Year] - 2022) * 0.180) AS DECIMAL(8,3)),
    seed.HIndex + (y.[Year] - 2022),
    N'Semilla reportería institucional 2026',
    @Now
FROM @Venues seed
INNER JOIN dbo.Venues v ON v.Name = seed.Name
CROSS JOIN (VALUES (2022), (2023), (2024), (2025), (2026)) y([Year])
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.VenueMetrics vm
    WHERE vm.VenueId = v.VenueId
      AND vm.Year = y.[Year]
);

DECLARE @AcademicTerms TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Statuses TABLE (RowNum INT IDENTITY(1,1), Id TINYINT);
DECLARE @ResearchLines TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Faculties TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200));
DECLARE @FieldCombos TABLE (RowNum INT IDENTITY(1,1), BroadId INT, SpecificId INT, DetailedId INT);
DECLARE @IndexingSources TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(120));
DECLARE @VenueIds TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200), Quartile NVARCHAR(10));

INSERT INTO @AcademicTerms (Id)
SELECT AcademicTermId FROM dbo.AcademicTerms ORDER BY Name;

INSERT INTO @Statuses (Id)
SELECT PublicationStatusId FROM dbo.PublicationStatuses ORDER BY PublicationStatusId;

INSERT INTO @ResearchLines (Id)
SELECT ResearchLineId FROM dbo.ResearchLines ORDER BY Name;

INSERT INTO @Faculties (Id, Name)
SELECT FacultyId, Name FROM dbo.Faculties WHERE IsActive = 1 ORDER BY Name;

INSERT INTO @FieldCombos (BroadId, SpecificId, DetailedId)
SELECT TOP 20
    bf.BroadFieldId,
    sf.SpecificFieldId,
    df.DetailedFieldId
FROM dbo.BroadFields bf
INNER JOIN dbo.SpecificFields sf ON sf.BroadFieldId = bf.BroadFieldId
INNER JOIN dbo.DetailedFields df ON df.SpecificFieldId = sf.SpecificFieldId
ORDER BY bf.Name, sf.Name, df.Name;

INSERT INTO @IndexingSources (Id, Name)
SELECT IndexingSourceId, Name FROM dbo.IndexingSources WHERE IsActive = 1 ORDER BY Name;

INSERT INTO @VenueIds (Id, Name, Quartile)
SELECT v.VenueId, v.Name, seed.Quartile
FROM @Venues seed
INNER JOIN dbo.Venues v ON v.Name = seed.Name
ORDER BY seed.RowNum;

DECLARE @i INT = 1;
DECLARE @InsertedArticles TABLE (Seq INT, ArticleId INT);

WHILE @i <= 50
BEGIN
    DECLARE @ExternalId NVARCHAR(80) = CONCAT(N'SEED-ART-', FORMAT(@i, '000'));

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Articles
        WHERE ExternalSource = @Source
          AND ExternalId = @ExternalId
    )
    BEGIN
        DECLARE @VenueCount INT = (SELECT COUNT(*) FROM @VenueIds);
        DECLARE @TermCount INT = (SELECT COUNT(*) FROM @AcademicTerms);
        DECLARE @StatusCount INT = (SELECT COUNT(*) FROM @Statuses);
        DECLARE @LineCount INT = (SELECT COUNT(*) FROM @ResearchLines);
        DECLARE @FacultyCount INT = (SELECT COUNT(*) FROM @Faculties);
        DECLARE @FieldCount INT = (SELECT COUNT(*) FROM @FieldCombos);

        DECLARE @VenueId INT = (SELECT Id FROM @VenueIds WHERE RowNum = ((@i - 1) % @VenueCount) + 1);
        DECLARE @AcademicTermId INT = (SELECT Id FROM @AcademicTerms WHERE RowNum = ((@i - 1) % @TermCount) + 1);
        DECLARE @StatusId TINYINT = (SELECT Id FROM @Statuses WHERE RowNum = ((@i - 1) % @StatusCount) + 1);
        DECLARE @ResearchLineId INT = (SELECT Id FROM @ResearchLines WHERE RowNum = ((@i - 1) % @LineCount) + 1);
        DECLARE @FacultyId INT = (SELECT Id FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @FacultyName NVARCHAR(200) = (SELECT Name FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @BroadId INT = (SELECT BroadId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @SpecificId INT = (SELECT SpecificId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @DetailedId INT = (SELECT DetailedId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @ArticleYear SMALLINT = CAST(2022 + ((@i - 1) % 5) AS SMALLINT);
        DECLARE @PublishedAt DATETIME2 = DATEFROMPARTS(@ArticleYear, ((@i - 1) % 12) + 1, ((@i - 1) % 25) + 1);
        DECLARE @CreatedAt DATETIME2 = DATEADD(DAY, @i * -2, @Now);

        INSERT INTO dbo.Articles
        (
            Title, Doi, Year, PublishedAt, PageCount, PublicationUrl,
            IsProjectResult, HasInterculturalComponent, ProceedingsName, Proceedings,
            EventName, GroupName, Filiacion, VenueId, AcademicTermId, PublicationStatusId,
            ResearchLineId, BroadFieldId, SpecificFieldId, DetailedFieldId, FacultyId,
            IsOpenAccess, ExternalSource, ExternalId, CreatedAt, UpdatedAt
        )
        VALUES
        (
            CONCAT(N'Produccion cientifica institucional ', FORMAT(@i, '000'), N': analisis aplicado para decision academica'),
            CONCAT(N'10.UTA/SEED.', @ArticleYear, N'.', FORMAT(@i, '000')),
            @ArticleYear,
            @PublishedAt,
            8 + (@i % 12),
            CONCAT(N'https://repositorio.uta.edu.ec/articulos/seed-', FORMAT(@i, '000')),
            CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 4 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 5 = 0 THEN N'Congreso Institucional de Investigacion' ELSE NULL END,
            CASE WHEN @i % 5 = 0 THEN N'Memorias academicas institucionales' ELSE NULL END,
            CASE WHEN @i % 5 = 0 THEN N'Encuentro de Investigacion UTA' ELSE NULL END,
            CONCAT(N'Grupo de investigacion ', ((@i - 1) % 6) + 1),
            @FacultyName,
            @VenueId,
            @AcademicTermId,
            @StatusId,
            @ResearchLineId,
            @BroadId,
            @SpecificId,
            @DetailedId,
            @FacultyId,
            CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END,
            @Source,
            @ExternalId,
            @CreatedAt,
            @Now
        );

        DECLARE @ArticleId INT = SCOPE_IDENTITY();
        INSERT INTO @InsertedArticles (Seq, ArticleId) VALUES (@i, @ArticleId);

        INSERT INTO dbo.ArticleParticipants
        (
            ArticleId, [Index], Identificacion, Nombre, Participacion, ParticipantType,
            IsPrimaryAuthor, Email, Orcid, Affiliation, ExternalAuthorId, CreatedAt, UpdatedAt
        )
        VALUES
        (
            @ArticleId,
            1,
            CONCAT(N'1800', FORMAT(@i, '000000')),
            CONCAT(N'Investigador Principal ', FORMAT(@i, '000')),
            N'Autor principal',
            N'Autor',
            1,
            CONCAT(N'autor.principal', FORMAT(@i, '000'), N'@uta.edu.ec'),
            CONCAT(N'0000-0002-', FORMAT((@i * 37) % 10000, '0000'), N'-', FORMAT((@i * 53) % 10000, '0000')),
            @FacultyName,
            CONCAT(N'UTA-AUTH-', FORMAT(@i, '000'), N'-01'),
            @CreatedAt,
            @Now
        ),
        (
            @ArticleId,
            2,
            CONCAT(N'1801', FORMAT(@i, '000000')),
            CONCAT(N'Coautor Academico ', FORMAT(@i, '000')),
            N'Coautor',
            N'Autor',
            0,
            CONCAT(N'coautor', FORMAT(@i, '000'), N'@uta.edu.ec'),
            CONCAT(N'0000-0003-', FORMAT((@i * 41) % 10000, '0000'), N'-', FORMAT((@i * 59) % 10000, '0000')),
            @FacultyName,
            CONCAT(N'UTA-AUTH-', FORMAT(@i, '000'), N'-02'),
            @CreatedAt,
            @Now
        ),
        (
            @ArticleId,
            3,
            CONCAT(N'1802', FORMAT(@i, '000000')),
            CONCAT(N'Colaborador Externo ', FORMAT(@i, '000')),
            N'Colaborador',
            N'Colaborador',
            0,
            CONCAT(N'colaborador', FORMAT(@i, '000'), N'@redacademica.org'),
            CONCAT(N'0000-0001-', FORMAT((@i * 43) % 10000, '0000'), N'-', FORMAT((@i * 61) % 10000, '0000')),
            N'Red academica colaboradora',
            CONCAT(N'EXT-AUTH-', FORMAT(@i, '000'), N'-03'),
            @CreatedAt,
            @Now
        );

        DECLARE @IndexingCount INT = (SELECT COUNT(*) FROM @IndexingSources);
        DECLARE @FirstIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i - 1) % @IndexingCount) + 1);
        DECLARE @SecondIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i) % @IndexingCount) + 1);
        DECLARE @ThirdIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i + 1) % @IndexingCount) + 1);

        INSERT INTO dbo.ArticleIndexings (ArticleId, IndexingSourceId)
        SELECT @ArticleId, SourceId
        FROM (
            SELECT @FirstIndexingId AS SourceId
            UNION
            SELECT @SecondIndexingId
            UNION
            SELECT @ThirdIndexingId WHERE @i % 3 = 0
        ) s
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.ArticleIndexings ai
            WHERE ai.ArticleId = @ArticleId
              AND ai.IndexingSourceId = s.SourceId
        );
    END

    SET @i += 1;
END;

COMMIT TRANSACTION;

SELECT
    COUNT(*) AS SeededArticles
FROM dbo.Articles
WHERE ExternalSource = @Source;

SELECT
    COUNT(*) AS SeededParticipants
FROM dbo.ArticleParticipants ap
INNER JOIN dbo.Articles a ON a.Id = ap.ArticleId
WHERE a.ExternalSource = @Source;

SELECT
    COUNT(*) AS SeededIndexingLinks
FROM dbo.ArticleIndexings ai
INNER JOIN dbo.Articles a ON a.Id = ai.ArticleId
WHERE a.ExternalSource = @Source;
GO
