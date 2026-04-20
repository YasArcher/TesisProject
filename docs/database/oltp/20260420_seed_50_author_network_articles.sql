/*
    Semilla de red de autores para reportería y evaluación docente.

    Crea 50 artículos completos. Cada artículo contiene 5 participantes:
    1 autor principal y 4 coautores. Los docentes se repiten entre artículos
    con la misma identificación, ORCID, correo y ExternalAuthorId para permitir
    agrupar producción, coautorías y filiaciones en reportería.

    Base destino: TesisDB_Extensible.
    Idempotente por ExternalSource + ExternalId.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Source NVARCHAR(80) = N'SEED_AUTHOR_NETWORK_20260420';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

/* Catálogos base necesarios. */
MERGE dbo.AcademicTerms AS T
USING (VALUES (N'2024-II'), (N'2025-I'), (N'2025-II'), (N'2026-I'), (N'2026-II')) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

MERGE dbo.PublicationStatuses AS T
USING (VALUES
    (1, N'Registrado'),
    (2, N'Publicado'),
    (3, N'En revision')
) AS S(PublicationStatusId, Name)
ON T.PublicationStatusId = S.PublicationStatusId
WHEN MATCHED THEN UPDATE SET Name = S.Name
WHEN NOT MATCHED THEN INSERT (PublicationStatusId, Name) VALUES (S.PublicationStatusId, S.Name);

MERGE dbo.ResearchLines AS T
USING (VALUES
    (N'Tecnologias de la informacion y transformacion digital'),
    (N'Innovacion educativa y evaluacion docente'),
    (N'Produccion cientifica y desarrollo institucional'),
    (N'Salud, bienestar y sociedad'),
    (N'Innovacion productiva y sostenibilidad')
) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

MERGE dbo.IndexingSources AS T
USING (VALUES
    (N'Scopus'),
    (N'Web of Science'),
    (N'Latindex'),
    (N'SciELO'),
    (N'DOAJ'),
    (N'Redalyc'),
    (N'Dialnet'),
    (N'ERIC')
) AS S(Name)
ON T.Name = S.Name
WHEN MATCHED THEN UPDATE SET IsActive = 1
WHEN NOT MATCHED THEN INSERT (Name, IsActive) VALUES (S.Name, 1);

MERGE dbo.Faculties AS T
USING (VALUES
    (N'FISEI', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial'),
    (N'FCHE', N'Facultad de Ciencias Humanas y de la Educacion'),
    (N'FCS', N'Facultad de Ciencias de la Salud'),
    (N'FCAUD', N'Facultad de Contabilidad y Auditoria'),
    (N'JCS', N'Facultad de Jurisprudencia y Ciencias Sociales'),
    (N'FCAGP', N'Facultad de Ciencias Agropecuarias'),
    (N'FICM', N'Facultad de Ingenieria Civil y Mecanica'),
    (N'FCIAL', N'Facultad de Ciencia e Ingenieria en Alimentos y Biotecnologia'),
    (N'DI-DIDE', N'Direccion de Investigacion y Desarrollo')
) AS S(Code, Name)
ON T.Code = S.Code
WHEN MATCHED THEN UPDATE SET Name = S.Name, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Code, Name, IsActive, CreatedAt) VALUES (S.Code, S.Name, 1, @Now);

MERGE dbo.BroadFields AS T
USING (VALUES
    (N'Ciencias de la computacion e informacion'),
    (N'Ciencias de la educacion'),
    (N'Ciencias medicas y de la salud'),
    (N'Ingenieria y tecnologia'),
    (N'Ciencias sociales')
) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

DECLARE @BroadComputing INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias de la computacion e informacion');
DECLARE @BroadEducation INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias de la educacion');
DECLARE @BroadHealth INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias medicas y de la salud');
DECLARE @BroadEngineering INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ingenieria y tecnologia');
DECLARE @BroadSocial INT = (SELECT TOP 1 BroadFieldId FROM dbo.BroadFields WHERE Name = N'Ciencias sociales');

MERGE dbo.SpecificFields AS T
USING (VALUES
    (@BroadComputing, N'CS01', N'Sistemas de informacion'),
    (@BroadComputing, N'CS02', N'Analitica de datos'),
    (@BroadEducation, N'ED01', N'Evaluacion educativa'),
    (@BroadHealth, N'HS01', N'Salud publica'),
    (@BroadEngineering, N'EN01', N'Sistemas industriales'),
    (@BroadSocial, N'SS01', N'Gestion institucional')
) AS S(BroadFieldId, Code, Name)
ON T.BroadFieldId = S.BroadFieldId AND T.Code = S.Code
WHEN MATCHED THEN UPDATE SET Name = S.Name
WHEN NOT MATCHED THEN INSERT (BroadFieldId, Code, Name) VALUES (S.BroadFieldId, S.Code, S.Name);

MERGE dbo.DetailedFields AS T
USING (
    SELECT sf.SpecificFieldId, v.Code, v.Name
    FROM dbo.SpecificFields sf
    INNER JOIN (VALUES
        (N'CS01', N'CS0101', N'Arquitecturas de sistemas academicos'),
        (N'CS02', N'CS0201', N'Modelos predictivos para investigacion'),
        (N'ED01', N'ED0101', N'Evaluacion docente basada en evidencias'),
        (N'HS01', N'HS0101', N'Epidemiologia aplicada'),
        (N'EN01', N'EN0101', N'Optimizacion de procesos institucionales'),
        (N'SS01', N'SS0101', N'Gestion de la produccion cientifica')
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
    (N'Journal of Academic Analytics and Faculty Evaluation', N'2700-2001', N'Journal', N'https://journals.example.edu/jaafe', N'Q1', 1.432, 6.100, 67),
    (N'Revista Andina de Evaluacion Docente', N'2700-2002', N'Journal', N'https://revistas.uta.edu.ec/evaluacion-docente', N'Q2', 0.891, 4.200, 45),
    (N'Latin American Journal of Scientific Production', N'2700-2003', N'Journal', N'https://journals.example.edu/lajsp', N'Q1', 1.221, 5.400, 59),
    (N'Revista de Innovacion Educativa y Datos', N'2700-2004', N'Journal', N'https://revistas.example.edu/ried', N'Q2', 0.774, 3.700, 38),
    (N'Computacion Aplicada a la Gestion Universitaria', N'2700-2005', N'Journal', N'https://revistas.example.edu/cagu', N'Q2', 0.832, 3.950, 41),
    (N'Science Metrics and Institutional Development', N'2700-2006', N'Journal', N'https://journals.example.edu/smid', N'Q1', 1.509, 6.800, 72),
    (N'Revista de Salud, Sociedad y Universidad', N'2700-2007', N'Journal', N'https://revistas.example.edu/ssu', N'Q3', 0.502, 2.500, 31),
    (N'Ingenieria y Sostenibilidad Aplicada', N'2700-2008', N'Journal', N'https://revistas.example.edu/isa', N'Q2', 0.706, 3.300, 34),
    (N'Proceedings de Analitica Institucional', N'2700-2009', N'Conference', N'https://conferences.example.edu/pai', N'Q4', 0.243, 1.400, 18),
    (N'Revista UTA de Investigacion Interdisciplinaria', N'2700-2010', N'Journal', N'https://revistas.uta.edu.ec/interdisciplinaria', N'Q3', 0.459, 2.200, 27);

MERGE dbo.Venues AS T
USING @Venues AS S
ON T.Name = S.Name
WHEN MATCHED THEN
    UPDATE SET IssnCode = S.IssnCode, Type = S.VenueType, JournalUrl = S.JournalUrl
WHEN NOT MATCHED THEN
    INSERT (Name, IssnCode, Type, JournalUrl)
    VALUES (S.Name, S.IssnCode, S.VenueType, S.JournalUrl);

INSERT INTO dbo.VenueMetrics (VenueId, Year, SJR, Quartile, CiteScore, HIndex, SourceNote, CreatedAt)
SELECT
    v.VenueId,
    y.[Year],
    CAST(seed.Sjr + ((y.[Year] - 2023) * 0.027) AS DECIMAL(6,3)),
    seed.Quartile,
    CAST(seed.CiteScore + ((y.[Year] - 2023) * 0.160) AS DECIMAL(8,3)),
    seed.HIndex + (y.[Year] - 2023),
    N'Semilla red de autores para evaluacion docente 2026',
    @Now
FROM @Venues seed
INNER JOIN dbo.Venues v ON v.Name = seed.Name
CROSS JOIN (VALUES (2023), (2024), (2025), (2026)) y([Year])
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.VenueMetrics vm
    WHERE vm.VenueId = v.VenueId AND vm.Year = y.[Year]
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
    (N'UTA-DOC-001', N'Dra. Ana Belen Morales Paredes', N'ana.morales@uta.edu.ec', N'0000-0002-1101-2101', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-TEACHER-001', N'Docente investigador'),
    (N'UTA-DOC-002', N'Dr. Carlos Javier Naranjo Ruiz', N'carlos.naranjo@uta.edu.ec', N'0000-0002-1102-2102', N'Facultad de Ciencias Humanas y de la Educacion', N'UTA-TEACHER-002', N'Docente investigador'),
    (N'UTA-DOC-003', N'Dra. Sofia Isabel Quispe Lema', N'sofia.quispe@uta.edu.ec', N'0000-0002-1103-2103', N'Facultad de Ciencias de la Salud', N'UTA-TEACHER-003', N'Docente investigador'),
    (N'UTA-DOC-004', N'Mg. Diego Fernando Salazar Viteri', N'diego.salazar@uta.edu.ec', N'0000-0002-1104-2104', N'Facultad de Contabilidad y Auditoria', N'UTA-TEACHER-004', N'Docente investigador'),
    (N'UTA-DOC-005', N'Dra. Gabriela Alejandra Paredes Mena', N'gabriela.paredes@uta.edu.ec', N'0000-0002-1105-2105', N'Facultad de Jurisprudencia y Ciencias Sociales', N'UTA-TEACHER-005', N'Docente investigador'),
    (N'UTA-DOC-006', N'Dr. Mateo Sebastian Herrera Cruz', N'mateo.herrera@uta.edu.ec', N'0000-0002-1106-2106', N'Facultad de Ciencias Agropecuarias', N'UTA-TEACHER-006', N'Docente investigador'),
    (N'UTA-DOC-007', N'Dra. Valentina Lucia Cardenas Rojas', N'valentina.cardenas@uta.edu.ec', N'0000-0002-1107-2107', N'Facultad de Ingenieria Civil y Mecanica', N'UTA-TEACHER-007', N'Docente investigador'),
    (N'UTA-DOC-008', N'Dr. Nicolas Andres Tapia Flores', N'nicolas.tapia@uta.edu.ec', N'0000-0002-1108-2108', N'Facultad de Ciencia e Ingenieria en Alimentos y Biotecnologia', N'UTA-TEACHER-008', N'Docente investigador'),
    (N'UTA-DOC-009', N'Dra. Elena Maribel Cueva Arias', N'elena.cueva@uta.edu.ec', N'0000-0002-1109-2109', N'Direccion de Investigacion y Desarrollo', N'UTA-TEACHER-009', N'Docente investigador'),
    (N'UTA-DOC-010', N'Mg. Andres Esteban Molina Vera', N'andres.molina@uta.edu.ec', N'0000-0002-1110-2110', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial', N'UTA-TEACHER-010', N'Docente investigador'),
    (N'UTA-DOC-011', N'Dra. Paola Fernanda Rios Cando', N'paola.rios@uta.edu.ec', N'0000-0002-1111-2111', N'Facultad de Ciencias Humanas y de la Educacion', N'UTA-TEACHER-011', N'Docente investigador'),
    (N'UTA-DOC-012', N'Dr. Luis Patricio Guamán Ortega', N'luis.guaman@uta.edu.ec', N'0000-0002-1112-2112', N'Facultad de Ciencias de la Salud', N'UTA-TEACHER-012', N'Docente investigador'),
    (N'EXT-INV-013', N'Dra. Camila Torres Benitez', N'camila.torres@redacademica.org', N'0000-0003-1113-2113', N'Red academica colaboradora', N'EXT-RESEARCHER-013', N'Investigador externo'),
    (N'EXT-INV-014', N'Dr. Martin Alejandro Soto Peña', N'martin.soto@redacademica.org', N'0000-0003-1114-2114', N'Red academica colaboradora', N'EXT-RESEARCHER-014', N'Investigador externo'),
    (N'EXT-INV-015', N'Dra. Laura Daniela Vega Castro', N'laura.vega@redacademica.org', N'0000-0003-1115-2115', N'Red academica colaboradora', N'EXT-RESEARCHER-015', N'Investigador externo');

DECLARE @AcademicTerms TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Statuses TABLE (RowNum INT IDENTITY(1,1), Id TINYINT);
DECLARE @ResearchLines TABLE (RowNum INT IDENTITY(1,1), Id INT);
DECLARE @Faculties TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200));
DECLARE @FieldCombos TABLE (RowNum INT IDENTITY(1,1), BroadId INT, SpecificId INT, DetailedId INT);
DECLARE @IndexingSources TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(120));
DECLARE @VenueIds TABLE (RowNum INT IDENTITY(1,1), Id INT, Name NVARCHAR(200));
DECLARE @AuthorSlots TABLE (SlotIndex INT, AuthorRow INT, Participation NVARCHAR(150), IsPrimary BIT);

INSERT INTO @AcademicTerms (Id) SELECT AcademicTermId FROM dbo.AcademicTerms ORDER BY Name;
INSERT INTO @Statuses (Id) SELECT PublicationStatusId FROM dbo.PublicationStatuses ORDER BY PublicationStatusId;
INSERT INTO @ResearchLines (Id) SELECT ResearchLineId FROM dbo.ResearchLines ORDER BY Name;
INSERT INTO @Faculties (Id, Name) SELECT FacultyId, Name FROM dbo.Faculties WHERE IsActive = 1 ORDER BY Name;
INSERT INTO @IndexingSources (Id, Name) SELECT IndexingSourceId, Name FROM dbo.IndexingSources WHERE IsActive = 1 ORDER BY Name;
INSERT INTO @VenueIds (Id, Name) SELECT v.VenueId, v.Name FROM @Venues seed INNER JOIN dbo.Venues v ON v.Name = seed.Name ORDER BY seed.RowNum;
INSERT INTO @FieldCombos (BroadId, SpecificId, DetailedId)
SELECT bf.BroadFieldId, sf.SpecificFieldId, df.DetailedFieldId
FROM dbo.BroadFields bf
INNER JOIN dbo.SpecificFields sf ON sf.BroadFieldId = bf.BroadFieldId
INNER JOIN dbo.DetailedFields df ON df.SpecificFieldId = sf.SpecificFieldId
ORDER BY bf.Name, sf.Name, df.Name;

DECLARE @i INT = 1;

WHILE @i <= 50
BEGIN
    DECLARE @ExternalId NVARCHAR(80) = CONCAT(N'AUTH-NET-ART-', FORMAT(@i, '000'));

    IF NOT EXISTS (SELECT 1 FROM dbo.Articles WHERE ExternalSource = @Source AND ExternalId = @ExternalId)
    BEGIN
        DECLARE @VenueCount INT = (SELECT COUNT(*) FROM @VenueIds);
        DECLARE @TermCount INT = (SELECT COUNT(*) FROM @AcademicTerms);
        DECLARE @StatusCount INT = (SELECT COUNT(*) FROM @Statuses);
        DECLARE @LineCount INT = (SELECT COUNT(*) FROM @ResearchLines);
        DECLARE @FacultyCount INT = (SELECT COUNT(*) FROM @Faculties);
        DECLARE @FieldCount INT = (SELECT COUNT(*) FROM @FieldCombos);
        DECLARE @IndexingCount INT = (SELECT COUNT(*) FROM @IndexingSources);
        DECLARE @AuthorCount INT = (SELECT COUNT(*) FROM @Authors);

        DECLARE @VenueId INT = (SELECT Id FROM @VenueIds WHERE RowNum = ((@i - 1) % @VenueCount) + 1);
        DECLARE @AcademicTermId INT = (SELECT Id FROM @AcademicTerms WHERE RowNum = ((@i - 1) % @TermCount) + 1);
        DECLARE @StatusId TINYINT = (SELECT Id FROM @Statuses WHERE RowNum = ((@i - 1) % @StatusCount) + 1);
        DECLARE @ResearchLineId INT = (SELECT Id FROM @ResearchLines WHERE RowNum = ((@i - 1) % @LineCount) + 1);
        DECLARE @FacultyId INT = (SELECT Id FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @FacultyName NVARCHAR(200) = (SELECT Name FROM @Faculties WHERE RowNum = ((@i - 1) % @FacultyCount) + 1);
        DECLARE @BroadId INT = (SELECT BroadId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @SpecificId INT = (SELECT SpecificId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @DetailedId INT = (SELECT DetailedId FROM @FieldCombos WHERE RowNum = ((@i - 1) % @FieldCount) + 1);
        DECLARE @ArticleYear SMALLINT = CAST(2023 + ((@i - 1) % 4) AS SMALLINT);
        DECLARE @PublishedAt DATETIME2 = DATEFROMPARTS(@ArticleYear, ((@i - 1) % 12) + 1, ((@i - 1) % 25) + 1);
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
            CONCAT(N'Evaluacion docente y produccion cientifica ', FORMAT(@i, '000'), N': red de autoria institucional'),
            CONCAT(N'10.UTA/AUTHNET.', @ArticleYear, N'.', FORMAT(@i, '000')),
            @ArticleYear,
            @PublishedAt,
            9 + (@i % 14),
            CONCAT(N'https://repositorio.uta.edu.ec/evaluacion-docente/auth-net-', FORMAT(@i, '000')),
            CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 5 = 0 THEN 1 ELSE 0 END,
            CASE WHEN @i % 6 = 0 THEN N'Congreso de Analitica Institucional y Evaluacion Docente' ELSE NULL END,
            CASE WHEN @i % 6 = 0 THEN N'Memorias de produccion cientifica institucional' ELSE NULL END,
            CASE WHEN @i % 6 = 0 THEN N'Encuentro Internacional de Evaluacion Docente' ELSE NULL END,
            CONCAT(N'Red docente de investigacion ', ((@i - 1) % 5) + 1),
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
            (1, ((@i - 1) % @AuthorCount) + 1, N'Autor principal', 1),
            (2, ((@i + 1) % @AuthorCount) + 1, N'Coautor metodologico', 0),
            (3, ((@i + 4) % @AuthorCount) + 1, N'Coautor analitico', 0),
            (4, ((@i + 7) % @AuthorCount) + 1, N'Coautor de validacion', 0),
            (5, ((@i + 10) % @AuthorCount) + 1, N'Coautor de revision', 0);

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
        DECLARE @SecondIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i + 2) % @IndexingCount) + 1);
        DECLARE @ThirdIndexingId INT = (SELECT Id FROM @IndexingSources WHERE RowNum = ((@i + 4) % @IndexingCount) + 1);

        INSERT INTO dbo.ArticleIndexings (ArticleId, IndexingSourceId)
        SELECT @ArticleId, SourceId
        FROM (
            SELECT @FirstIndexingId AS SourceId
            UNION
            SELECT @SecondIndexingId
            UNION
            SELECT @ThirdIndexingId WHERE @i % 2 = 0
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
    ap.Identificacion,
    MAX(ap.Nombre) AS Autor,
    COUNT(DISTINCT ap.ArticleId) AS Articulos,
    SUM(CASE WHEN ap.IsPrimaryAuthor = 1 THEN 1 ELSE 0 END) AS AutorPrincipal,
    SUM(CASE WHEN ap.IsPrimaryAuthor = 0 THEN 1 ELSE 0 END) AS Coautorias
FROM dbo.ArticleParticipants ap
INNER JOIN dbo.Articles a ON a.Id = ap.ArticleId
WHERE a.ExternalSource = @Source
GROUP BY ap.Identificacion
ORDER BY Articulos DESC, Autor;

SELECT COUNT(*) AS SeededIndexingLinks
FROM dbo.ArticleIndexings ai
INNER JOIN dbo.Articles a ON a.Id = ai.ArticleId
WHERE a.ExternalSource = @Source;
GO
