/*
    Semilla focal para validar el panel de autores y coautoria.

    Crea 12 articulos completos en OLTP. Cada articulo contiene 5 participantes:
    Christopher Santamaria como autor principal y 4 coautores rotados.

    Base destino: TesisDB_Extensible.
    Idempotente por ExternalSource + ExternalId.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Source NVARCHAR(80) = N'SEED_CHRISTOPHER_AUTHOR_20260508';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

MERGE dbo.AcademicTerms AS T
USING (VALUES (N'2026-I'), (N'2026-II')) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

MERGE dbo.PublicationStatuses AS T
USING (VALUES (1, N'Registrado'), (2, N'Publicado'), (3, N'En revision')) AS S(PublicationStatusId, Name)
ON T.PublicationStatusId = S.PublicationStatusId
WHEN MATCHED THEN UPDATE SET Name = S.Name
WHEN NOT MATCHED THEN INSERT (PublicationStatusId, Name) VALUES (S.PublicationStatusId, S.Name);

MERGE dbo.ResearchLines AS T
USING (VALUES
    (N'Tecnologias de la informacion y transformacion digital'),
    (N'Produccion cientifica y desarrollo institucional'),
    (N'Innovacion educativa y evaluacion docente')
) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

MERGE dbo.IndexingSources AS T
USING (VALUES (N'Scopus'), (N'Web of Science'), (N'SciELO'), (N'DOAJ')) AS S(Name)
ON T.Name = S.Name
WHEN MATCHED THEN UPDATE SET IsActive = 1
WHEN NOT MATCHED THEN INSERT (Name, IsActive) VALUES (S.Name, 1);

MERGE dbo.Faculties AS T
USING (VALUES
    (N'FISEI', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial'),
    (N'DI-DIDE', N'Direccion de Investigacion y Desarrollo'),
    (N'FCHE', N'Facultad de Ciencias Humanas y de la Educacion')
) AS S(Code, Name)
ON T.Code = S.Code
WHEN MATCHED THEN UPDATE SET Name = S.Name, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Code, Name, IsActive, CreatedAt) VALUES (S.Code, S.Name, 1, @Now);

MERGE dbo.BroadFields AS T
USING (VALUES
    (N'Ciencias de la computacion e informacion'),
    (N'Ciencias de la educacion'),
    (N'Ciencias sociales')
) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

DECLARE @BroadComputing INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias de la computacion e informacion');
DECLARE @BroadEducation INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias de la educacion');
DECLARE @BroadSocial INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias sociales');

MERGE dbo.SpecificFields AS T
USING (VALUES
    (@BroadComputing, N'CHS-CS01', N'Analitica institucional de datos'),
    (@BroadEducation, N'CHS-ED01', N'Evaluacion educativa institucional'),
    (@BroadSocial, N'CHS-SS01', N'Gestion institucional de investigacion')
) AS S(BroadFieldId, Code, Name)
ON T.BroadFieldId = S.BroadFieldId AND T.Code = S.Code
WHEN MATCHED THEN UPDATE SET Name = S.Name
WHEN NOT MATCHED THEN INSERT (BroadFieldId, Code, Name) VALUES (S.BroadFieldId, S.Code, S.Name);

MERGE dbo.DetailedFields AS T
USING (
    SELECT sf.SpecificFieldId, v.Code, v.Name
    FROM dbo.SpecificFields sf
    INNER JOIN (VALUES
        (N'CHS-CS01', N'CHS-CS0101', N'Modelos de analitica para investigacion institucional'),
        (N'CHS-ED01', N'CHS-ED0101', N'Evaluacion docente basada en evidencias institucionales'),
        (N'CHS-SS01', N'CHS-SS0101', N'Gestion de la produccion cientifica institucional')
    ) v(SpecificCode, Code, Name) ON v.SpecificCode = sf.Code
) AS S(SpecificFieldId, Code, Name)
ON T.SpecificFieldId = S.SpecificFieldId AND T.Code = S.Code
WHEN MATCHED THEN UPDATE SET Name = S.Name
WHEN NOT MATCHED THEN INSERT (SpecificFieldId, Code, Name) VALUES (S.SpecificFieldId, S.Code, S.Name);

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
    (N'Journal of Data Analytics for Higher Education', N'2800-5001', N'Journal', N'https://journals.example.edu/jdahe', N'Q1', 1.210, 5.800, 52),
    (N'Revista de Gestion Cientifica Universitaria', N'2800-5002', N'Journal', N'https://revistas.example.edu/gcu', N'Q2', 0.860, 3.900, 37),
    (N'Proceedings de Innovacion y Analitica Institucional', N'2800-5003', N'Conference', N'https://conferences.example.edu/iai', N'Q3', 0.430, 2.100, 24);

MERGE dbo.Venues AS T
USING @Venues AS S
ON T.Name = S.Name
WHEN MATCHED THEN UPDATE SET IssnCode = S.IssnCode, Type = S.VenueType, JournalUrl = S.JournalUrl
WHEN NOT MATCHED THEN INSERT (Name, IssnCode, Type, JournalUrl) VALUES (S.Name, S.IssnCode, S.VenueType, S.JournalUrl);

INSERT INTO dbo.VenueMetrics (VenueId, Year, SJR, Quartile, CiteScore, HIndex, SourceNote, CreatedAt)
SELECT v.VenueId, y.[Year], seed.Sjr, seed.Quartile, seed.CiteScore, seed.HIndex, N'Semilla Christopher Santamaria 2026', @Now
FROM @Venues seed
INNER JOIN dbo.Venues v ON v.Name = seed.Name
CROSS JOIN (VALUES (2024), (2025), (2026)) y([Year])
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.VenueMetrics vm WHERE vm.VenueId = v.VenueId AND vm.Year = y.[Year]
);

DECLARE @Authors TABLE
(
    RowNum INT IDENTITY(1,1),
    Identificacion NVARCHAR(100),
    Nombre NVARCHAR(300),
    Email NVARCHAR(200),
    Orcid NVARCHAR(50),
    Affiliation NVARCHAR(300),
    ExternalAuthorId NVARCHAR(150),
    ParticipantType NVARCHAR(50)
);

INSERT INTO @Authors (Identificacion, Nombre, Email, Orcid, Affiliation, ExternalAuthorId, ParticipantType)
VALUES
    (N'UTA-DOC-CHS-001', N'Christopher Santamaria', N'christopher.santamaria@uta.edu.ec', N'0000-0002-6000-0001', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-CHRISTOPHER-SANTAMARIA', N'Docente investigador'),
    (N'UTA-DOC-CHS-002', N'Ana Belen Morales Paredes', N'ana.morales@uta.edu.ec', N'0000-0002-6000-0002', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-CHS-COAUTHOR-002', N'Docente investigador'),
    (N'UTA-DOC-CHS-003', N'Carlos Javier Naranjo Ruiz', N'carlos.naranjo@uta.edu.ec', N'0000-0002-6000-0003', N'Direccion de Investigacion y Desarrollo', N'UTA-CHS-COAUTHOR-003', N'Docente investigador'),
    (N'UTA-DOC-CHS-004', N'Sofia Isabel Quispe Lema', N'sofia.quispe@uta.edu.ec', N'0000-0002-6000-0004', N'Facultad de Ciencias Humanas y de la Educacion', N'UTA-CHS-COAUTHOR-004', N'Docente investigador'),
    (N'UTA-DOC-CHS-005', N'Diego Fernando Salazar Viteri', N'diego.salazar@uta.edu.ec', N'0000-0002-6000-0005', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-CHS-COAUTHOR-005', N'Docente investigador'),
    (N'UTA-DOC-CHS-006', N'Gabriela Alejandra Paredes Mena', N'gabriela.paredes@uta.edu.ec', N'0000-0002-6000-0006', N'Direccion de Investigacion y Desarrollo', N'UTA-CHS-COAUTHOR-006', N'Docente investigador'),
    (N'UTA-DOC-CHS-007', N'Mateo Sebastian Herrera Cruz', N'mateo.herrera@uta.edu.ec', N'0000-0002-6000-0007', N'Facultad de Ciencias Humanas y de la Educacion', N'UTA-CHS-COAUTHOR-007', N'Docente investigador'),
    (N'UTA-DOC-CHS-008', N'Valentina Lucia Cardenas Rojas', N'valentina.cardenas@uta.edu.ec', N'0000-0002-6000-0008', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-CHS-COAUTHOR-008', N'Docente investigador'),
    (N'UTA-DOC-CHS-009', N'Nicolas Andres Tapia Flores', N'nicolas.tapia@uta.edu.ec', N'0000-0002-6000-0009', N'Direccion de Investigacion y Desarrollo', N'UTA-CHS-COAUTHOR-009', N'Docente investigador');

DECLARE @AcademicTerms TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Statuses TABLE (RowNum INT IDENTITY(1,1), Id TINYINT);
DECLARE @ResearchLines TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Faculties TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200));
DECLARE @FieldCombos TABLE (RowNum INT IDENTITY(1,1), BroadId INT, SpecificId INT, DetailedId INT);
DECLARE @IndexingSources TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @VenueIds TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200));
DECLARE @AuthorSlots TABLE (SlotIndex INT, AuthorRow INT, Participation NVARCHAR(150), IsPrimary BIT);

INSERT INTO @AcademicTerms (Id) SELECT AcademicTermId FROM dbo.AcademicTerms ORDER BY Name;
INSERT INTO @Statuses (Id) SELECT PublicationStatusId FROM dbo.PublicationStatuses ORDER BY PublicationStatusId;
INSERT INTO @ResearchLines (Id) SELECT ResearchLineId FROM dbo.ResearchLines ORDER BY Name;
INSERT INTO @Faculties (Id, Name) SELECT FacultyId, Name FROM dbo.Faculties WHERE IsActive = 1 ORDER BY Name;
INSERT INTO @IndexingSources (Id) SELECT IndexingSourceId FROM dbo.IndexingSources WHERE IsActive = 1 ORDER BY Name;
INSERT INTO @VenueIds (Id, Name) SELECT v.VenueId, v.Name FROM @Venues seed INNER JOIN dbo.Venues v ON v.Name = seed.Name ORDER BY seed.RowNum;
INSERT INTO @FieldCombos (BroadId, SpecificId, DetailedId)
SELECT bf.BroadFieldId, sf.SpecificFieldId, df.DetailedFieldId
FROM dbo.BroadFields bf
INNER JOIN dbo.SpecificFields sf ON sf.BroadFieldId = bf.BroadFieldId
INNER JOIN dbo.DetailedFields df ON df.SpecificFieldId = sf.SpecificFieldId
ORDER BY bf.Name, sf.Name, df.Name;

DECLARE @i INT = 1;

WHILE @i <= 12
BEGIN
    DECLARE @ExternalId NVARCHAR(80) = CONCAT(N'CHRISTOPHER-ART-', FORMAT(@i, '000'));

    IF NOT EXISTS (SELECT 1 FROM dbo.Articles WHERE ExternalSource = @Source AND ExternalId = @ExternalId)
    BEGIN
        DECLARE @VenueCount INT = (SELECT COUNT(*) FROM @VenueIds);
        DECLARE @TermCount INT = (SELECT COUNT(*) FROM @AcademicTerms);
        DECLARE @StatusCount INT = (SELECT COUNT(*) FROM @Statuses);
        DECLARE @LineCount INT = (SELECT COUNT(*) FROM @ResearchLines);
        DECLARE @FacultyCount INT = (SELECT COUNT(*) FROM @Faculties);
        DECLARE @FieldCount INT = (SELECT COUNT(*) FROM @FieldCombos);
        DECLARE @IndexingCount INT = (SELECT COUNT(*) FROM @IndexingSources);
        DECLARE @CoauthorCount INT = (SELECT COUNT(*) FROM @Authors) - 1;

        DECLARE @VenueId INT = (SELECT Id FROM @VenueIds WHERE RowNum = ((@i - 1) % @VenueCount) + 1);
        DECLARE @AcademicTermId INT = (SELECT Id FROM @AcademicTerms WHERE RowNum = ((@i - 1) % @TermCount) + 1);
        DECLARE @StatusId TINYINT = (SELECT Id FROM @Statuses WHERE RowNum = ((@i - 1) % @StatusCount) + 1);
        DECLARE @ResearchLineId INT = (SELECT Id FROM @ResearchLines WHERE RowNum = ((@i - 1) % @LineCount) + 1);
        DECLARE @FacultyId INT = (SELECT Id FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @FacultyName NVARCHAR(200) = (SELECT Name FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @BroadId INT = (SELECT BroadId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @SpecificId INT = (SELECT SpecificId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @DetailedId INT = (SELECT DetailedId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @ArticleYear SMALLINT = CAST(2024 + ((@i - 1) % 3) AS SMALLINT);
        DECLARE @PublishedAt DATETIME2 = DATEFROMPARTS(@ArticleYear, ((@i - 1) % 12) + 1, 10 + ((@i - 1) % 12));
        DECLARE @CreatedAt DATETIME2 = DATEADD(DAY, -@i, @Now);

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
            CONCAT(N'Analitica institucional y produccion cientifica con Christopher Santamaria ', FORMAT(@i, '000')),
            CONCAT(N'10.UTA/CHRISTOPHER.', @ArticleYear, N'.', FORMAT(@i, '000')),
            @ArticleYear,
            @PublishedAt,
            8 + (@i % 10),
            CONCAT(N'https://repositorio.uta.edu.ec/christopher-santamaria/articulo-', FORMAT(@i, '000')),
            CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 4 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 5 = 0 THEN N'Congreso de Analitica Institucional' ELSE NULL END,
            CASE WHEN @i % 5 = 0 THEN N'Memorias de investigacion aplicada' ELSE NULL END,
            CASE WHEN @i % 5 = 0 THEN N'Encuentro de Gestion Cientifica' ELSE NULL END,
            N'Red de analitica de autores',
            @FacultyName,
            @VenueId,
            @AcademicTermId,
            @StatusId,
            @ResearchLineId,
            @BroadId,
            @SpecificId,
            @DetailedId,
            @FacultyId,
            CASE WHEN @i % 3 <> 0 THEN 1 ELSE 0 END,
            @Source,
            @ExternalId,
            @CreatedAt,
            @Now
        );

        DECLARE @ArticleId INT = SCOPE_IDENTITY();

        DELETE FROM @AuthorSlots;

        INSERT INTO @AuthorSlots (SlotIndex, AuthorRow, Participation, IsPrimary)
        VALUES
            (1, 1, N'Autor principal', 1),
            (2, 2 + ((@i - 1) % @CoauthorCount), N'Coautor metodologico', 0),
            (3, 2 + ((@i + 1) % @CoauthorCount), N'Coautor analitico', 0),
            (4, 2 + ((@i + 3) % @CoauthorCount), N'Coautor de validacion', 0),
            (5, 2 + ((@i + 5) % @CoauthorCount), N'Coautor de revision', 0);

        INSERT INTO dbo.ArticleParticipants
        (
            ArticleId, [Index], Identificacion, Nombre, Participacion, ParticipantType,
            IsPrimaryAuthor, Email, Orcid, Affiliation, ExternalAuthorId, CreatedAt, UpdatedAt
        )
        SELECT
            @ArticleId,
            slots.SlotIndex,
            a.Identificacion,
            a.Nombre,
            slots.Participation,
            a.ParticipantType,
            slots.IsPrimary,
            a.Email,
            a.Orcid,
            a.Affiliation,
            a.ExternalAuthorId,
            @CreatedAt,
            @Now
        FROM @AuthorSlots slots
        INNER JOIN @Authors a ON a.RowNum = slots.AuthorRow
        ORDER BY slots.SlotIndex;

        DECLARE @FirstIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i - 1) % @IndexingCount) + 1);
        DECLARE @SecondIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i + 1) % @IndexingCount) + 1);

        INSERT INTO dbo.ArticleIndexings (ArticleId, IndexingSourceId)
        SELECT @ArticleId, SourceId
        FROM (
            SELECT @FirstIndexingId AS SourceId
            UNION
            SELECT @SecondIndexingId
        ) s
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.ArticleIndexings ai
            WHERE ai.ArticleId = @ArticleId AND ai.IndexingSourceId = s.SourceId
        );
    END;

    SET @i += 1;
END;

COMMIT TRANSACTION;

SELECT COUNT(*) AS SeededArticles
FROM dbo.Articles
WHERE ExternalSource = @Source;

SELECT COUNT(*) AS SeededParticipants
FROM dbo.ArticleParticipants ap
INNER JOIN dbo.Articles a ON a.Id = ap.ArticleId
WHERE a.ExternalSource = @Source;

SELECT
    ap.Nombre AS Autor,
    COUNT(DISTINCT ap.ArticleId) AS Articulos,
    SUM(CASE WHEN ap.IsPrimaryAuthor = 1 THEN 1 ELSE 0 END) AS AutorPrincipal,
    SUM(CASE WHEN ap.IsPrimaryAuthor = 0 THEN 1 ELSE 0 END) AS Coautorias
FROM dbo.ArticleParticipants ap
INNER JOIN dbo.Articles a ON a.Id = ap.ArticleId
WHERE a.ExternalSource = @Source
GROUP BY ap.Nombre
ORDER BY Articulos DESC, Autor;
GO
