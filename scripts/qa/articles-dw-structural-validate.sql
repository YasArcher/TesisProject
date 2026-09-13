SET NOCOUNT ON;

SELECT N'SOURCE_PRODUCTS' AS Metric, COUNT(*) AS Value FROM dbo.ArticleReadView
UNION ALL SELECT N'SOURCE_ARTICLE_EXTENSIONS', COUNT(*) FROM dbo.Articles WHERE ExternalSource = N'QA-ARTICLE-DW'
UNION ALL SELECT N'SOURCE_PRODUCT_AUTHORS', COUNT(*) FROM dbo.ProductAuthors pa JOIN dbo.Products p ON p.Id=pa.ProductId WHERE p.ProductTypeId IN (1,2)
UNION ALL SELECT N'SOURCE_ARTICLE_INDEXINGS', COUNT(*) FROM dbo.ArticleIndexings ai JOIN dbo.Articles a ON a.Id=ai.ArticleId WHERE a.ExternalSource=N'QA-ARTICLE-DW'
UNION ALL SELECT N'SOURCE_QA_VENUES', COUNT(*) FROM dbo.Venues WHERE Name LIKE N'QA-VENUE-DW-%'
UNION ALL SELECT N'SOURCE_QA_VENUE_METRICS', COUNT(*) FROM dbo.VenueMetrics vm JOIN dbo.Venues v ON v.VenueId=vm.VenueId WHERE v.Name LIKE N'QA-VENUE-DW-%'
UNION ALL SELECT N'DW_DIM_ARTICLES', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.DimArticles
UNION ALL SELECT N'DW_FACT_PUBLICATIONS', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticlePublications
UNION ALL SELECT N'DW_FACT_AUTHORS', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticleAuthors
UNION ALL SELECT N'DW_FACT_INDEXINGS', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticleIndexings
UNION ALL SELECT N'DW_DIM_VENUES', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.DimVenues
UNION ALL SELECT N'DW_FACT_VENUE_METRICS', COUNT(*) FROM [__OPERATIONAL_DB__].ArticlesDW.FactVenueMetricYears;

SELECT
    d.ProductId, d.ArticleId, pt.ProductTypeId, d.Title,
    j.Name AS Journal, idb.Name AS IndexingDatabase, q.Code AS Quartile,
    v.Name AS Venue, at.AcademicTermId, ps.PublicationStatusId, rl.ResearchLineId,
    fld.BroadFieldId, fld.SpecificFieldId, fld.DetailedFieldId,
    af.FacultyId AS ArticleFacultyId, pf.FacultyId AS ProjectFacultyId,
    f.ProjectId, f.CreatedDateKey, f.PublishedDateKey, f.ArticleCount,
    f.AuthorCount, f.IndexingCount, f.PageCount, f.Sjr,
    f.IsProjectResultFlag, f.IsOpenAccessFlag, f.HasInterculturalFlag
FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticlePublications f
JOIN [__OPERATIONAL_DB__].ArticlesDW.DimArticles d ON d.ArticleKey=f.ArticleKey
JOIN [__OPERATIONAL_DB__].ArticlesDW.DimProductTypes pt ON pt.ProductTypeKey=f.ProductTypeKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimJournals j ON j.JournalKey=f.JournalKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimIndexingDatabases idb ON idb.IndexingDatabaseKey=f.IndexingDatabaseKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimQuartiles q ON q.QuartileKey=f.QuartileKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimVenues v ON v.VenueKey=f.VenueKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimAcademicTerms at ON at.AcademicTermKey=f.AcademicTermKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimPublicationStatuses ps ON ps.PublicationStatusKey=f.PublicationStatusKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimResearchLines rl ON rl.ResearchLineKey=f.ResearchLineKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimFields fld ON fld.FieldKey=f.FieldKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimFaculties af ON af.FacultyKey=f.ArticleFacultyKey
LEFT JOIN [__OPERATIONAL_DB__].ArticlesDW.DimFaculties pf ON pf.FacultyKey=f.ProjectFacultyKey
ORDER BY d.ProductId;

WITH SourceAuthors AS (
    SELECT pa.Id ProductAuthorId, pa.ProductId, pa.AuthorId, pa.AuthorOrder, pa.IsPrimaryAuthor,
           NULLIF(LTRIM(RTRIM(pa.Participation)),N'') Participation,
           NULLIF(LTRIM(RTRIM(pa.NameSnapshot)),N'') NameSnapshot,
           NULLIF(LTRIM(RTRIM(pa.AffiliationSnapshot)),N'') AffiliationSnapshot
    FROM dbo.ProductAuthors pa JOIN dbo.Products p ON p.Id=pa.ProductId WHERE p.ProductTypeId IN (1,2)
), DwAuthors AS (
    SELECT f.ProductAuthorId,d.ProductId,a.AuthorId,f.AuthorOrder,f.IsPrimaryAuthor,
           f.Participation,f.NameSnapshot,f.AffiliationSnapshot
    FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticleAuthors f
    JOIN [__OPERATIONAL_DB__].ArticlesDW.DimArticles d ON d.ArticleKey=f.ArticleKey
    JOIN [__OPERATIONAL_DB__].ArticlesDW.DimAuthors a ON a.AuthorKey=f.AuthorKey
)
SELECT N'AUTHOR_SYMMETRIC_DIFFERENCES' Metric, COUNT(*) Value FROM (
    SELECT * FROM SourceAuthors EXCEPT SELECT * FROM DwAuthors
    UNION ALL
    SELECT * FROM DwAuthors EXCEPT SELECT * FROM SourceAuthors
) x;

WITH SourceIndexings AS (
    SELECT a.ProductId,ai.IndexingSourceId FROM dbo.ArticleIndexings ai
    JOIN dbo.Articles a ON a.Id=ai.ArticleId JOIN dbo.Products p ON p.Id=a.ProductId
    WHERE p.ProductTypeId IN (1,2)
), DwIndexings AS (
    SELECT a.ProductId,s.IndexingSourceId FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticleIndexings f
    JOIN [__OPERATIONAL_DB__].ArticlesDW.DimArticles a ON a.ArticleKey=f.ArticleKey
    JOIN [__OPERATIONAL_DB__].ArticlesDW.DimIndexingSources s ON s.IndexingSourceKey=f.IndexingSourceKey
)
SELECT N'INDEXING_SYMMETRIC_DIFFERENCES' Metric, COUNT(*) Value FROM (
    SELECT * FROM SourceIndexings EXCEPT SELECT * FROM DwIndexings
    UNION ALL
    SELECT * FROM DwIndexings EXCEPT SELECT * FROM SourceIndexings
) x;

WITH SourceMetrics AS (
    SELECT vm.VenueId,vm.Year,vm.SJR,vm.Quartile FROM dbo.VenueMetrics vm
    WHERE EXISTS (SELECT 1 FROM dbo.Articles a JOIN dbo.Products p ON p.Id=a.ProductId
                  WHERE a.VenueId=vm.VenueId AND p.ProductTypeId IN (1,2))
), DwMetrics AS (
    SELECT v.VenueId,f.Year,f.Sjr,f.Quartile FROM [__OPERATIONAL_DB__].ArticlesDW.FactVenueMetricYears f
    JOIN [__OPERATIONAL_DB__].ArticlesDW.DimVenues v ON v.VenueKey=f.VenueKey
)
SELECT N'VENUE_METRIC_SYMMETRIC_DIFFERENCES' Metric, COUNT(*) Value FROM (
    SELECT * FROM SourceMetrics EXCEPT SELECT * FROM DwMetrics
    UNION ALL
    SELECT * FROM DwMetrics EXCEPT SELECT * FROM SourceMetrics
) x;

WITH SourceDates AS (
    SELECT DISTINCT CONVERT(date,CreatedAt) [Date] FROM dbo.ArticleReadView
    UNION SELECT DISTINCT CONVERT(date,PublishedAt) FROM dbo.ArticleReadView WHERE PublishedAt IS NOT NULL
), DwDates AS (SELECT CONVERT(date,[Date]) [Date] FROM [__OPERATIONAL_DB__].ArticlesDW.DimDates)
SELECT N'DATE_SYMMETRIC_DIFFERENCES' Metric, COUNT(*) Value FROM (
    SELECT * FROM SourceDates EXCEPT SELECT * FROM DwDates
    UNION ALL
    SELECT * FROM DwDates EXCEPT SELECT * FROM SourceDates
) x;

SELECT N'TYPE2_NON_NULL_FORBIDDEN_VALUES' Metric, COUNT(*) Value
FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticlePublications f
JOIN [__OPERATIONAL_DB__].ArticlesDW.DimArticles a ON a.ArticleKey=f.ArticleKey
JOIN [__OPERATIONAL_DB__].ArticlesDW.DimProductTypes pt ON pt.ProductTypeKey=f.ProductTypeKey
WHERE pt.ProductTypeId=2 AND (a.Doi IS NOT NULL OR a.PublicationYear IS NOT NULL OR a.YearRaw IS NOT NULL OR f.Sjr IS NOT NULL OR f.QuartileKey IS NOT NULL);

SELECT N'ARTICLE_PROJECT_FACULTY_EQUAL' Metric, COUNT(*) Value
FROM [__OPERATIONAL_DB__].ArticlesDW.FactArticlePublications
WHERE ArticleFacultyKey IS NOT NULL AND ProjectFacultyKey IS NOT NULL AND ArticleFacultyKey=ProjectFacultyKey;

SELECT TOP(1)
    N'LEDGER' Metric, OperationType, OperationCode, Status,
    JSON_VALUE(ResultJson,'$.counts.dimArticles') DimArticles,
    JSON_VALUE(ResultJson,'$.counts.factArticleIndexings') FactArticleIndexings,
    JSON_VALUE(ResultJson,'$.counts.factVenueMetricYears') FactVenueMetricYears,
    JSON_VALUE(ResultJson,'$.warnings[0].code') WarningCode,
    JSON_VALUE(ResultJson,'$.warnings[0].count') WarningCount
FROM dbo.OperationExecutionHistory
WHERE OperationType=N'ETL' AND OperationCode=N'ARTICLES_DW_FULL_LOAD'
ORDER BY Id DESC;
