namespace tesisproject.backend.Migrations.Unified;

// Historical migration payload: keep literal IDs and SQL frozen. Never reference runtime enums here.
// Future view changes belong to a new migration, not this private payload.
public partial class AddArticleReadView
{
private static class ArticleReadViewSql
{
    internal const string TransferCanonicalValues = """
        -- Never discard an extension outside the two explicitly selected article product types.
        IF EXISTS (SELECT 1 FROM dbo.Articles a JOIN dbo.Products p ON p.Id=a.ProductId
            WHERE p.ProductTypeId NOT IN (1,2) AND (a.Doi IS NOT NULL OR a.[Year] IS NOT NULL OR a.PublicationUrl IS NOT NULL))
            THROW 51000, 'Article attributes on an unsupported product type require explicit reconciliation.', 1;
        
        SELECT a.ProductId, p.ProductTypeId, x.AttributeId, x.Value
        INTO #ArticleAttributes
        FROM dbo.Articles a JOIN dbo.Products p ON p.Id=a.ProductId
        CROSS APPLY (VALUES (8, CONVERT(nvarchar(max), a.Doi)),
            (9, CONVERT(nvarchar(max), a.[Year])), (10, CONVERT(nvarchar(max), a.PublicationUrl))) x(AttributeId, Value)
        WHERE p.ProductTypeId IN (1,2) AND NULLIF(LTRIM(RTRIM(x.Value)),N'') IS NOT NULL;
        
        IF EXISTS (SELECT 1 FROM #ArticleAttributes x LEFT JOIN dbo.ProductAttributes pa ON pa.Id=x.AttributeId WHERE pa.Id IS NULL)
            THROW 51001, 'Canonical Article attributes 8/9/10 must exist before transferring existing values.', 1;
        IF EXISTS (SELECT 1 FROM #ArticleAttributes x JOIN dbo.ProductAttributeDefinitions d
            ON d.ProductTypeId=x.ProductTypeId AND d.ProductAttributeId=x.AttributeId
            GROUP BY x.ProductId,x.AttributeId HAVING COUNT(*)>1)
            THROW 51002, 'Duplicate Article attribute definitions require reconciliation.', 1;
        
        INSERT dbo.ProductAttributeDefinitions(ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT DISTINCT x.ProductTypeId,x.AttributeId,0,x.AttributeId
        FROM #ArticleAttributes x
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductAttributeDefinitions d
            WHERE d.ProductTypeId=x.ProductTypeId AND d.ProductAttributeId=x.AttributeId);
        
        IF EXISTS (SELECT 1 FROM #ArticleAttributes x JOIN dbo.ProductAttributeDefinitions d
            ON d.ProductTypeId=x.ProductTypeId AND d.ProductAttributeId=x.AttributeId
            JOIN dbo.ProductValues pv ON pv.ProductId=x.ProductId AND pv.AttributeDefinitionId=d.Id
            WHERE NULLIF(LTRIM(RTRIM(pv.Value)),N'') IS NOT NULL
              AND pv.Value COLLATE Latin1_General_100_BIN2 <> x.Value COLLATE Latin1_General_100_BIN2)
            THROW 51003, 'Article and canonical ProductValues disagree; resolve the conflict before migration.', 1;
        
        UPDATE pv SET Value=x.Value, UpdatedAt=SYSUTCDATETIME()
        FROM dbo.ProductValues pv JOIN dbo.ProductAttributeDefinitions d ON d.Id=pv.AttributeDefinitionId
        JOIN #ArticleAttributes x ON x.ProductId=pv.ProductId AND x.ProductTypeId=d.ProductTypeId AND x.AttributeId=d.ProductAttributeId
        WHERE NULLIF(LTRIM(RTRIM(pv.Value)),N'') IS NULL;
        
        INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt)
        SELECT x.ProductId,d.Id,x.Value,SYSUTCDATETIME()
        FROM #ArticleAttributes x JOIN dbo.ProductAttributeDefinitions d
        ON d.ProductTypeId=x.ProductTypeId AND d.ProductAttributeId=x.AttributeId
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductValues pv WHERE pv.ProductId=x.ProductId AND pv.AttributeDefinitionId=d.Id);
        DROP TABLE #ArticleAttributes;
        """;

    internal const string CreateView = """
        CREATE VIEW dbo.ArticleReadView
        AS
        WITH RankedValues AS
        (
            SELECT pv.ProductId, pa.Id AS ProductAttributeId, pv.Value,
                ROW_NUMBER() OVER
                (
                    PARTITION BY pv.ProductId, pa.Id
                    ORDER BY COALESCE(pv.UpdatedAt, pv.CreatedAt) DESC, pv.Id DESC
                ) AS ValueRank
            FROM dbo.ProductValues pv
            JOIN dbo.ProductAttributeDefinitions d ON d.Id = pv.AttributeDefinitionId
            JOIN dbo.ProductAttributes pa ON pa.Id = d.ProductAttributeId
            JOIN dbo.Products p ON p.Id = pv.ProductId AND p.ProductTypeId = d.ProductTypeId
            WHERE p.ProductTypeId IN (1, 2) AND pa.Id BETWEEN 3 AND 10
        ),
        Attributes AS
        (
            SELECT ProductId,
                MAX(CASE WHEN ProductAttributeId = 3 THEN Value END) AS Journal,
                MAX(CASE WHEN ProductAttributeId = 4 THEN Value END) AS IndexingDatabase,
                MAX(CASE WHEN ProductAttributeId = 5 THEN Value END) AS SjrRaw,
                MAX(CASE WHEN ProductAttributeId = 6 THEN Value END) AS Quartile,
                MAX(CASE WHEN ProductAttributeId = 7 THEN Value END) AS Issn,
                MAX(CASE WHEN ProductAttributeId = 8 THEN Value END) AS Doi,
                MAX(CASE WHEN ProductAttributeId = 9 THEN Value END) AS YearRaw,
                MAX(CASE WHEN ProductAttributeId = 10 THEN Value END) AS PublicationUrl
            FROM RankedValues WHERE ValueRank = 1
            GROUP BY ProductId
        )
        -- DISTINCT also prevents SQL Server from updating base tables through this view.
        SELECT DISTINCT p.Id AS ProductId, p.ProjectId, p.ProductTypeId, p.Title,
            p.IsActive, p.CreatedAt, v.Doi, v.Journal, v.IndexingDatabase,
            TRY_CONVERT(decimal(18,6), NULLIF(LTRIM(RTRIM(v.SjrRaw)), N'')) AS Sjr,
            v.SjrRaw, v.Quartile, v.Issn,
            TRY_CONVERT(smallint, NULLIF(LTRIM(RTRIM(v.YearRaw)), N'')) AS [Year],
            v.YearRaw, v.PublicationUrl,
            a.Id AS ArticleId, a.FacultyId, pr.FacultyId AS ProjectFacultyId,
            a.AcademicTermId, a.ResearchLineId, a.PublicationStatusId,
            a.BroadFieldId, a.SpecificFieldId, a.DetailedFieldId, a.VenueId,
            a.ExternalSource, a.ExternalId, a.PublishedAt, a.PageCount,
            a.HasInterculturalComponent, a.IsOpenAccess, a.ProceedingsName,
            a.Proceedings, a.EventName, a.GroupName, a.Filiacion
        FROM dbo.Products p
        LEFT JOIN dbo.Articles a ON a.ProductId = p.Id
        LEFT JOIN dbo.Projects pr ON pr.ProjectId = p.ProjectId
        LEFT JOIN Attributes v ON v.ProductId = p.Id
        WHERE p.ProductTypeId IN (1, 2);
        """;
}
}
