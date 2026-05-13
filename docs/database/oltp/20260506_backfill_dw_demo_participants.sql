USE [TesisDB_Extensible];
GO

SET NOCOUNT ON;

;WITH MissingArticles AS
(
    SELECT
        a.Id AS ArticleId,
        ROW_NUMBER() OVER (ORDER BY a.Id) AS Seq,
        COALESCE(a.CreatedAt, SYSUTCDATETIME()) AS SourceCreatedAt
    FROM dbo.Articles a
    WHERE a.ExternalSource = N'DW_DEMO'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ArticleParticipants ap
          WHERE ap.ArticleId = a.Id
      )
)
INSERT INTO dbo.ArticleParticipants
(
    ArticleId,
    [Index],
    Identificacion,
    Nombre,
    Participacion,
    ParticipantType,
    InstitutionalPersonId,
    IsPrimaryAuthor,
    Email,
    Orcid,
    Affiliation,
    ExternalAuthorId,
    CreatedAt,
    UpdatedAt
)
SELECT
    m.ArticleId,
    p.AuthorOrder,
    CONCAT(N'18', RIGHT(CONCAT(N'0000', m.Seq), 4), RIGHT(CONCAT(N'00', p.AuthorOrder), 2)),
    CONCAT(p.AuthorNamePrefix, RIGHT(CONCAT(N'00', m.Seq), 2)),
    p.Participation,
    p.ParticipantType,
    NULL,
    p.IsPrimaryAuthor,
    CONCAT(N'dw.demo.', RIGHT(CONCAT(N'00', m.Seq), 2), N'.', p.AuthorOrder, N'@uta.edu.ec'),
    CONCAT(N'0000-0002-', RIGHT(CONCAT(N'0000', m.Seq), 4), N'-', RIGHT(CONCAT(N'000', p.AuthorOrder), 3), N'X'),
    N'Universidad Técnica de Ambato',
    CONCAT(N'DW-DEMO-', RIGHT(CONCAT(N'0000', m.Seq), 4), N'-', p.AuthorOrder),
    m.SourceCreatedAt,
    SYSUTCDATETIME()
FROM MissingArticles m
CROSS APPLY
(
    VALUES
        (1, N'Autor demostrativo principal ', N'Autor', N'Docente investigador', CAST(1 AS bit)),
        (2, N'Coautor demostrativo ', N'Coautor', N'Colaborador', CAST(0 AS bit))
) p(AuthorOrder, AuthorNamePrefix, Participation, ParticipantType, IsPrimaryAuthor);

SELECT
    @@ROWCOUNT AS InsertedParticipants,
    (
        SELECT COUNT(*)
        FROM dbo.Articles a
        WHERE a.ExternalSource = N'DW_DEMO'
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.ArticleParticipants ap
              WHERE ap.ArticleId = a.Id
          )
    ) AS RemainingDwDemoArticlesWithoutParticipants;
GO
