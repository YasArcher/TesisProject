SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @QaProducts TABLE (ProductId int PRIMARY KEY);
INSERT @QaProducts (ProductId)
SELECT Id FROM dbo.Products
WHERE Title IN (N'QA-DW-PRODUCT-001', N'QA-DW-PRODUCT-002', N'QA-DW-PRODUCT-003');

DELETE ai
FROM dbo.ArticleIndexings ai
JOIN dbo.Articles a ON a.Id = ai.ArticleId
JOIN @QaProducts p ON p.ProductId = a.ProductId
JOIN dbo.IndexingSources s ON s.Id = ai.IndexingSourceId
WHERE s.Name IN (N'QA-INDEXING-DW-ONE', N'QA-INDEXING-DW-TWO');

DELETE a
FROM dbo.Articles a
JOIN @QaProducts p ON p.ProductId = a.ProductId
WHERE a.ExternalSource = N'QA-ARTICLE-DW'
  AND a.ExternalId IN (N'QA-ARTICLE-DW-001', N'QA-ARTICLE-DW-002', N'QA-ARTICLE-DW-003');

DELETE vm FROM dbo.VenueMetrics vm
JOIN dbo.Venues v ON v.VenueId = vm.VenueId
WHERE v.Name IN (N'QA-VENUE-DW-PRIMARY', N'QA-VENUE-DW-SECONDARY')
  AND v.IssnCode IN (N'QA-ISSN-001', N'QA-ISSN-002');
DELETE v FROM dbo.Venues v
WHERE v.Name IN (N'QA-VENUE-DW-PRIMARY', N'QA-VENUE-DW-SECONDARY')
  AND v.IssnCode IN (N'QA-ISSN-001', N'QA-ISSN-002')
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.VenueId = v.VenueId);

DELETE s FROM dbo.IndexingSources s
WHERE s.Name IN (N'QA-INDEXING-DW-ONE', N'QA-INDEXING-DW-TWO')
  AND NOT EXISTS (SELECT 1 FROM dbo.ArticleIndexings ai WHERE ai.IndexingSourceId = s.Id);

DELETE d FROM dbo.DetailedFields d
WHERE d.Code = N'QA-DW-DETAIL'
  AND d.Name = N'QA-ARTICLE-DW-DETAILED'
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.DetailedFieldId = d.DetailedFieldId);
DELETE s FROM dbo.SpecificFields s
WHERE s.Code = N'QA-DW-SPEC'
  AND s.Name = N'QA-ARTICLE-DW-SPECIFIC'
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.SpecificFieldId = s.SpecificFieldId)
  AND NOT EXISTS (SELECT 1 FROM dbo.DetailedFields d WHERE d.SpecificFieldId = s.SpecificFieldId);
DELETE b FROM dbo.BroadFields b
WHERE b.Name = N'QA-ARTICLE-DW-BROAD'
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.BroadFieldId = b.BroadFieldId)
  AND NOT EXISTS (SELECT 1 FROM dbo.SpecificFields s WHERE s.BroadFieldId = b.BroadFieldId);

DELETE r FROM dbo.ResearchLines r
WHERE r.Name = N'QA-ARTICLE-DW-RESEARCH-LINE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.ResearchLineId = r.ResearchLineId);
DELETE s FROM dbo.PublicationStatuses s
WHERE s.PublicationStatusId IN (240, 241)
  AND s.Name IN (N'QA-DW-DRAFT', N'QA-DW-PUBLISHED')
  AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.PublicationStatusId = s.PublicationStatusId);

COMMIT TRANSACTION;

SELECT N'Cleanup completed. Run POST /api/etl/articles/full-load to refresh ArticlesDW.' AS Result;
