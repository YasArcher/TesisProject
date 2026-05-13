USE [TesisDW_Extensible];
GO

SET NOCOUNT ON;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FactArticlePublication_ArticleKey'
      AND object_id = OBJECT_ID(N'dw.FactArticlePublication')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_FactArticlePublication_ArticleKey
    ON dw.FactArticlePublication (ArticleKey)
    INCLUDE
    (
        FactArticlePublicationId,
        VenueKey,
        CreatedDateKey,
        PublishedDateKey,
        PublicationStatusKey,
        ResearchLineKey,
        FieldHierarchyKey,
        FacultyKey,
        ArticleCount,
        IsOpenAccessFlag,
        IsProjectResultFlag,
        HasInterculturalFlag
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FactArticleIndexing_ArticleKey'
      AND object_id = OBJECT_ID(N'dw.FactArticleIndexing')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_FactArticleIndexing_ArticleKey
    ON dw.FactArticleIndexing (ArticleKey)
    INCLUDE (IndexingSourceKey, DateKey, IndexingCount);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FactArticleAuthor_ArticleKey_Order'
      AND object_id = OBJECT_ID(N'dw.FactArticleAuthor')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_FactArticleAuthor_ArticleKey_Order
    ON dw.FactArticleAuthor (ArticleKey, IsPrimaryAuthorFlag DESC, AuthorOrder, AuthorKey)
    INCLUDE (AuthorCount, DateKey, ResearchLineKey, FieldHierarchyKey, RegistrationSourceKey);
END;
GO

UPDATE STATISTICS dw.FactArticlePublication;
UPDATE STATISTICS dw.FactArticleIndexing;
UPDATE STATISTICS dw.FactArticleAuthor;
GO
