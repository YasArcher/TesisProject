SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Product1 int = (SELECT MIN(Id) FROM dbo.Products WHERE Title = N'QA-DW-PRODUCT-001');
DECLARE @Product2 int = (SELECT MIN(Id) FROM dbo.Products WHERE Title = N'QA-DW-PRODUCT-002');
DECLARE @Product3 int = (SELECT MIN(Id) FROM dbo.Products WHERE Title = N'QA-DW-PRODUCT-003');

IF @Product1 IS NULL OR @Product2 IS NULL OR @Product3 IS NULL
    THROW 51000, 'Expected QA-DW-PRODUCT-001/002/003 products were not found.', 1;
IF (SELECT COUNT(*) FROM dbo.Products WHERE Title IN (N'QA-DW-PRODUCT-001', N'QA-DW-PRODUCT-002', N'QA-DW-PRODUCT-003')) <> 3
    THROW 51001, 'QA product titles are not unique.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.Articles a
    WHERE a.ProductId IN (@Product1, @Product2, @Product3)
      AND ISNULL(a.ExternalSource, N'') <> N'QA-ARTICLE-DW')
    THROW 51002, 'A target QA product already has a non-QA Article extension.', 1;

DECLARE @TermId int = (SELECT MIN(AcademicTermId) FROM dbo.AcademicTerms);
DECLARE @ProjectFacultyId int = (
    SELECT p.FacultyId FROM dbo.Products pr JOIN dbo.Projects p ON p.ProjectId = pr.ProjectId WHERE pr.Id = @Product1);
DECLARE @ArticleFacultyId int = (
    SELECT MIN(FacultyId) FROM dbo.Faculties WHERE FacultyId <> @ProjectFacultyId AND IsActive = 1);
DECLARE @SecondArticleFacultyId int = (
    SELECT MIN(FacultyId) FROM dbo.Faculties WHERE FacultyId NOT IN (@ProjectFacultyId, @ArticleFacultyId) AND IsActive = 1);
IF @TermId IS NULL OR @ProjectFacultyId IS NULL OR @ArticleFacultyId IS NULL
    THROW 51003, 'Required real AcademicTerm/Project Faculty/Article Faculty is unavailable.', 1;

IF EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 240 AND Name <> N'QA-DW-DRAFT')
    THROW 51004, 'PublicationStatusId 240 is occupied by non-QA data.', 1;
IF EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 241 AND Name <> N'QA-DW-PUBLISHED')
    THROW 51005, 'PublicationStatusId 241 is occupied by non-QA data.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 240)
    INSERT dbo.PublicationStatuses (PublicationStatusId, Name) VALUES (240, N'QA-DW-DRAFT');
IF NOT EXISTS (SELECT 1 FROM dbo.PublicationStatuses WHERE PublicationStatusId = 241)
    INSERT dbo.PublicationStatuses (PublicationStatusId, Name) VALUES (241, N'QA-DW-PUBLISHED');

IF NOT EXISTS (SELECT 1 FROM dbo.ResearchLines WHERE Name = N'QA-ARTICLE-DW-RESEARCH-LINE')
    INSERT dbo.ResearchLines (Name) VALUES (N'QA-ARTICLE-DW-RESEARCH-LINE');
DECLARE @ResearchLineId int = (SELECT ResearchLineId FROM dbo.ResearchLines WHERE Name = N'QA-ARTICLE-DW-RESEARCH-LINE');

IF NOT EXISTS (SELECT 1 FROM dbo.BroadFields WHERE Name = N'QA-ARTICLE-DW-BROAD')
    INSERT dbo.BroadFields (Name) VALUES (N'QA-ARTICLE-DW-BROAD');
DECLARE @BroadFieldId int = (SELECT BroadFieldId FROM dbo.BroadFields WHERE Name = N'QA-ARTICLE-DW-BROAD');
IF NOT EXISTS (SELECT 1 FROM dbo.SpecificFields WHERE BroadFieldId = @BroadFieldId AND Code = N'QA-DW-SPEC')
    INSERT dbo.SpecificFields (BroadFieldId, Code, Name)
    VALUES (@BroadFieldId, N'QA-DW-SPEC', N'QA-ARTICLE-DW-SPECIFIC');
DECLARE @SpecificFieldId int = (
    SELECT SpecificFieldId FROM dbo.SpecificFields WHERE BroadFieldId = @BroadFieldId AND Code = N'QA-DW-SPEC');
IF NOT EXISTS (SELECT 1 FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificFieldId AND Code = N'QA-DW-DETAIL')
    INSERT dbo.DetailedFields (SpecificFieldId, Code, Name)
    VALUES (@SpecificFieldId, N'QA-DW-DETAIL', N'QA-ARTICLE-DW-DETAILED');
DECLARE @DetailedFieldId int = (
    SELECT DetailedFieldId FROM dbo.DetailedFields WHERE SpecificFieldId = @SpecificFieldId AND Code = N'QA-DW-DETAIL');

IF NOT EXISTS (SELECT 1 FROM dbo.Venues WHERE Name = N'QA-VENUE-DW-PRIMARY' AND IssnCode = N'QA-ISSN-001')
    INSERT dbo.Venues (Name, IssnCode, IssueNumber, VolumeNumber, JournalUrl, Type)
    VALUES (N'QA-VENUE-DW-PRIMARY', N'QA-ISSN-001', N'7', N'11', N'https://qa.invalid/venue/primary', N'Journal');
IF NOT EXISTS (SELECT 1 FROM dbo.Venues WHERE Name = N'QA-VENUE-DW-SECONDARY' AND IssnCode = N'QA-ISSN-002')
    INSERT dbo.Venues (Name, IssnCode, IssueNumber, VolumeNumber, JournalUrl, Type)
    VALUES (N'QA-VENUE-DW-SECONDARY', N'QA-ISSN-002', N'2', N'4', N'https://qa.invalid/venue/secondary', N'Proceedings');
DECLARE @Venue1 int = (SELECT VenueId FROM dbo.Venues WHERE Name = N'QA-VENUE-DW-PRIMARY' AND IssnCode = N'QA-ISSN-001');
DECLARE @Venue2 int = (SELECT VenueId FROM dbo.Venues WHERE Name = N'QA-VENUE-DW-SECONDARY' AND IssnCode = N'QA-ISSN-002');

IF NOT EXISTS (SELECT 1 FROM dbo.IndexingSources WHERE Name = N'QA-INDEXING-DW-ONE')
    INSERT dbo.IndexingSources (Name, Abbreviation, ReferenceUrl, IsActive, IsLocked)
    VALUES (N'QA-INDEXING-DW-ONE', N'QAIDX1', N'https://qa.invalid/index/one', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.IndexingSources WHERE Name = N'QA-INDEXING-DW-TWO')
    INSERT dbo.IndexingSources (Name, Abbreviation, ReferenceUrl, IsActive, IsLocked)
    VALUES (N'QA-INDEXING-DW-TWO', N'QAIDX2', N'https://qa.invalid/index/two', 1, 0);
DECLARE @Indexing1 int = (SELECT Id FROM dbo.IndexingSources WHERE Name = N'QA-INDEXING-DW-ONE');
DECLARE @Indexing2 int = (SELECT Id FROM dbo.IndexingSources WHERE Name = N'QA-INDEXING-DW-TWO');

MERGE dbo.Articles AS target
USING (VALUES
    (@Product1, CONVERT(datetime2, '2024-06-15T10:30:00'), 12, CONVERT(bit,1),
     N'QA Proceedings Name', N'QA Proceedings', N'QA Event', N'QA Group', N'QA Affiliation',
     N'QA-ARTICLE-DW', N'QA-ARTICLE-DW-001', @Venue1, @TermId, CONVERT(tinyint,241), @ResearchLineId,
     @BroadFieldId, @SpecificFieldId, @DetailedFieldId, @ArticleFacultyId, CONVERT(bit,1)),
    (@Product2, NULL, NULL, CONVERT(bit,0),
     NULL, NULL, NULL, NULL, NULL,
     N'QA-ARTICLE-DW', N'QA-ARTICLE-DW-002', @Venue1, NULL, CONVERT(tinyint,240), NULL,
     @BroadFieldId, NULL, NULL, NULL, CONVERT(bit,0)),
    (@Product3, CONVERT(datetime2, '2023-09-20T08:00:00'), 8, CONVERT(bit,0),
     NULL, NULL, N'QA Secondary Event', NULL, NULL,
     N'QA-ARTICLE-DW', N'QA-ARTICLE-DW-003', @Venue2, @TermId, CONVERT(tinyint,241), @ResearchLineId,
     @BroadFieldId, @SpecificFieldId, @DetailedFieldId, @SecondArticleFacultyId, CONVERT(bit,1))
) AS source(ProductId, PublishedAt, PageCount, HasInterculturalComponent,
            ProceedingsName, Proceedings, EventName, GroupName, Filiacion,
            ExternalSource, ExternalId, VenueId, AcademicTermId, PublicationStatusId, ResearchLineId,
            BroadFieldId, SpecificFieldId, DetailedFieldId, FacultyId, IsOpenAccess)
ON target.ProductId = source.ProductId
WHEN MATCHED THEN UPDATE SET
    PublishedAt=source.PublishedAt, PageCount=source.PageCount,
    HasInterculturalComponent=source.HasInterculturalComponent,
    ProceedingsName=source.ProceedingsName, Proceedings=source.Proceedings,
    EventName=source.EventName, GroupName=source.GroupName, Filiacion=source.Filiacion,
    ExternalSource=source.ExternalSource, ExternalId=source.ExternalId,
    VenueId=source.VenueId, AcademicTermId=source.AcademicTermId,
    PublicationStatusId=source.PublicationStatusId, ResearchLineId=source.ResearchLineId,
    BroadFieldId=source.BroadFieldId, SpecificFieldId=source.SpecificFieldId,
    DetailedFieldId=source.DetailedFieldId, FacultyId=source.FacultyId,
    IsOpenAccess=source.IsOpenAccess
WHEN NOT MATCHED THEN INSERT
    (ProductId, PublishedAt, PageCount, HasInterculturalComponent,
     ProceedingsName, Proceedings, EventName, GroupName, Filiacion,
     ExternalSource, ExternalId, VenueId, AcademicTermId, PublicationStatusId, ResearchLineId,
     BroadFieldId, SpecificFieldId, DetailedFieldId, FacultyId, IsOpenAccess)
VALUES
    (source.ProductId, source.PublishedAt, source.PageCount, source.HasInterculturalComponent,
     source.ProceedingsName, source.Proceedings, source.EventName, source.GroupName, source.Filiacion,
     source.ExternalSource, source.ExternalId, source.VenueId, source.AcademicTermId,
     source.PublicationStatusId, source.ResearchLineId, source.BroadFieldId,
     source.SpecificFieldId, source.DetailedFieldId, source.FacultyId, source.IsOpenAccess);

DECLARE @Article1 int = (SELECT Id FROM dbo.Articles WHERE ProductId = @Product1);
DECLARE @Article2 int = (SELECT Id FROM dbo.Articles WHERE ProductId = @Product2);
DECLARE @Article3 int = (SELECT Id FROM dbo.Articles WHERE ProductId = @Product3);

DELETE ai FROM dbo.ArticleIndexings ai
WHERE ai.ArticleId IN (@Article1, @Article2, @Article3)
  AND ai.IndexingSourceId IN (@Indexing1, @Indexing2);
INSERT dbo.ArticleIndexings (ArticleId, IndexingSourceId)
VALUES (@Article1, @Indexing1), (@Article1, @Indexing2), (@Article2, @Indexing1);

DELETE vm FROM dbo.VenueMetrics vm WHERE vm.VenueId IN (@Venue1, @Venue2);
INSERT dbo.VenueMetrics (VenueId, Year, SJR, Quartile)
VALUES
    (@Venue1, 2023, 8.888, N'Q3'),
    (@Venue1, 2024, 9.999, N'Q4'),
    (@Venue2, 2024, 7.777, N'Q2');

COMMIT TRANSACTION;

SELECT N'PRODUCT' AS Entity, p.Id AS EntityId, p.Title AS Code
FROM dbo.Products p WHERE p.Id IN (@Product1, @Product2, @Product3)
UNION ALL
SELECT N'ARTICLE', a.Id, a.ExternalId FROM dbo.Articles a WHERE a.ProductId IN (@Product1, @Product2, @Product3)
UNION ALL
SELECT N'VENUE', v.VenueId, v.Name FROM dbo.Venues v WHERE v.VenueId IN (@Venue1, @Venue2)
UNION ALL
SELECT N'INDEXING_SOURCE', i.Id, i.Name FROM dbo.IndexingSources i WHERE i.Id IN (@Indexing1, @Indexing2)
UNION ALL
SELECT N'RESEARCH_LINE', @ResearchLineId, N'QA-ARTICLE-DW-RESEARCH-LINE'
UNION ALL
SELECT N'BROAD_FIELD', @BroadFieldId, N'QA-ARTICLE-DW-BROAD'
UNION ALL
SELECT N'SPECIFIC_FIELD', @SpecificFieldId, N'QA-DW-SPEC'
UNION ALL
SELECT N'DETAILED_FIELD', @DetailedFieldId, N'QA-DW-DETAIL'
UNION ALL
SELECT N'PUBLICATION_STATUS', 240, N'QA-DW-DRAFT'
UNION ALL
SELECT N'PUBLICATION_STATUS', 241, N'QA-DW-PUBLISHED'
ORDER BY Entity, EntityId;
