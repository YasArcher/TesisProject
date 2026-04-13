USE [TesisDW_Extensible]
GO
/****** Object:  Schema [dw]    Script Date: 13/04/2026 03:44:20 ******/
CREATE SCHEMA [dw]
GO
/****** Object:  Schema [etl]    Script Date: 13/04/2026 03:44:20 ******/
CREATE SCHEMA [etl]
GO
/****** Object:  UserDefinedFunction [etl].[fn_DateKey]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   HELPERS
   ========================================================= */
CREATE   FUNCTION [etl].[fn_DateKey](@Input DATETIME2)
RETURNS INT
AS
BEGIN
    IF @Input IS NULL RETURN NULL;
    RETURN CAST(CONVERT(CHAR(8), CAST(@Input AS DATE), 112) AS INT);
END;

GO
/****** Object:  Table [dw].[FactArticlePublication]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactArticlePublication](
	[FactArticlePublicationId] [bigint] IDENTITY(1,1) NOT NULL,
	[ArticleKey] [int] NOT NULL,
	[CreatedDateKey] [int] NOT NULL,
	[PublishedDateKey] [int] NULL,
	[VenueKey] [int] NULL,
	[AcademicTermKey] [int] NULL,
	[PublicationStatusKey] [int] NULL,
	[ResearchLineKey] [int] NULL,
	[FieldHierarchyKey] [int] NULL,
	[RegistrationSourceKey] [int] NULL,
	[ArticleCount] [int] NOT NULL,
	[PageCount] [int] NULL,
	[IsOpenAccessFlag] [bit] NOT NULL,
	[IsProjectResultFlag] [bit] NOT NULL,
	[HasInterculturalFlag] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactArticlePublicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_KPI_ProduccionCientifica]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   8. KPIS RÁPIDOS
   ========================================================= */

CREATE   VIEW [dw].[vw_KPI_ProduccionCientifica]
AS
SELECT
    COUNT(*) AS TotalArticles,
    SUM(CASE WHEN IsOpenAccessFlag = 1 THEN 1 ELSE 0 END) AS OpenAccessArticles,
    SUM(CASE WHEN IsProjectResultFlag = 1 THEN 1 ELSE 0 END) AS ProjectResultArticles,
    SUM(CASE WHEN HasInterculturalFlag = 1 THEN 1 ELSE 0 END) AS InterculturalArticles
FROM dw.FactArticlePublication;
GO
/****** Object:  Table [dw].[FactRegistrationBatch]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactRegistrationBatch](
	[FactRegistrationBatchId] [bigint] IDENTITY(1,1) NOT NULL,
	[BatchId_OLTP] [int] NOT NULL,
	[StartDateKey] [int] NOT NULL,
	[FinishDateKey] [int] NULL,
	[UserKey] [int] NULL,
	[RegistrationSourceKey] [int] NULL,
	[BatchStatusKey] [int] NOT NULL,
	[BatchCount] [int] NOT NULL,
	[TotalRows] [int] NOT NULL,
	[SuccessfulRows] [int] NOT NULL,
	[ErrorRows] [int] NOT NULL,
	[DurationSeconds] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[FactRegistrationBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_KPI_CalidadCarga]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_KPI_CalidadCarga]
AS
SELECT
    COUNT(*) AS TotalBatches,
    SUM(TotalRows) AS TotalRows,
    SUM(SuccessfulRows) AS SuccessfulRows,
    SUM(ErrorRows) AS ErrorRows
FROM dw.FactRegistrationBatch;
GO
/****** Object:  Table [dw].[DimDate]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimDate](
	[DateKey] [int] NOT NULL,
	[FullDate] [date] NOT NULL,
	[DayNumber] [tinyint] NOT NULL,
	[DayName] [nvarchar](20) NOT NULL,
	[WeekNumber] [tinyint] NOT NULL,
	[MonthNumber] [tinyint] NOT NULL,
	[MonthName] [nvarchar](20) NOT NULL,
	[QuarterNumber] [tinyint] NOT NULL,
	[SemesterNumber] [tinyint] NOT NULL,
	[YearNumber] [smallint] NOT NULL,
	[IsWeekend] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DateKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[FullDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimArticle]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimArticle](
	[ArticleKey] [int] IDENTITY(1,1) NOT NULL,
	[ArticleId_OLTP] [int] NOT NULL,
	[Title] [nvarchar](500) NULL,
	[Doi] [nvarchar](200) NULL,
	[ArticleYear] [smallint] NULL,
	[PublicationUrl] [nvarchar](500) NULL,
	[IsProjectResult] [bit] NOT NULL,
	[HasInterculturalComponent] [bit] NOT NULL,
	[ProceedingsName] [nvarchar](300) NULL,
	[Proceedings] [nvarchar](300) NULL,
	[EventName] [nvarchar](300) NULL,
	[GroupName] [nvarchar](300) NULL,
	[Filiacion] [nvarchar](300) NULL,
	[IsOpenAccess] [bit] NOT NULL,
	[ExternalSource] [nvarchar](50) NULL,
	[ExternalId] [nvarchar](150) NULL,
	[CreatedAtSource] [datetime2](7) NULL,
	[UpdatedAtSource] [datetime2](7) NULL,
	[ValidFrom] [datetime2](7) NOT NULL,
	[ValidTo] [datetime2](7) NULL,
	[IsCurrent] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ArticleKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimVenue]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimVenue](
	[VenueKey] [int] IDENTITY(1,1) NOT NULL,
	[VenueId_OLTP] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IssnCode] [nvarchar](20) NULL,
	[IssueNumber] [nvarchar](20) NULL,
	[VolumeNumber] [nvarchar](20) NULL,
	[JournalUrl] [nvarchar](400) NULL,
	[VenueType] [nvarchar](30) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtSource] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[VenueKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimAcademicTerm]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimAcademicTerm](
	[AcademicTermKey] [int] IDENTITY(1,1) NOT NULL,
	[AcademicTermId_OLTP] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtSource] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[AcademicTermKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimPublicationStatus]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimPublicationStatus](
	[PublicationStatusKey] [int] IDENTITY(1,1) NOT NULL,
	[PublicationStatusId_OLTP] [tinyint] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PublicationStatusKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimIndexingSource]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimIndexingSource](
	[IndexingSourceKey] [int] IDENTITY(1,1) NOT NULL,
	[IndexingSourceId_OLTP] [int] NOT NULL,
	[Name] [nvarchar](120) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IndexingSourceKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimResearchLine]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimResearchLine](
	[ResearchLineKey] [int] IDENTITY(1,1) NOT NULL,
	[ResearchLineId_OLTP] [int] NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtSource] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[ResearchLineKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimField]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimField](
	[FieldHierarchyKey] [int] IDENTITY(1,1) NOT NULL,
	[BroadFieldId_OLTP] [int] NULL,
	[BroadFieldName] [nvarchar](200) NULL,
	[SpecificFieldId_OLTP] [int] NULL,
	[SpecificFieldCode] [nvarchar](20) NULL,
	[SpecificFieldName] [nvarchar](200) NULL,
	[DetailedFieldId_OLTP] [int] NULL,
	[DetailedFieldCode] [nvarchar](20) NULL,
	[DetailedFieldName] [nvarchar](200) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FieldHierarchyKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_Articles_Detail]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   VISTAS INICIALES DE REPORTERÍA
   ========================================================= */

/* =========================================================
   1. PRODUCCIÓN CIENTÍFICA GENERAL
   ========================================================= */

CREATE   VIEW [dw].[vw_Articles_Detail]
AS
SELECT
    f.FactArticlePublicationId,
    da.ArticleKey,
    da.ArticleId_OLTP,
    da.Title,
    da.Doi,
    da.ArticleYear,
    da.PublicationUrl,
    da.IsOpenAccess,
    da.IsProjectResult,
    da.HasInterculturalComponent,
    dv.Name AS VenueName,
    dv.VenueType,
    dps.Name AS PublicationStatus,
    dat.Name AS AcademicTerm,
    drl.Name AS ResearchLine,
    df.BroadFieldName,
    df.SpecificFieldName,
    df.DetailedFieldName,
    dc.FullDate AS CreatedDate,
    dp.FullDate AS PublishedDate,
    f.PageCount,
    f.ArticleCount
FROM dw.FactArticlePublication f
INNER JOIN dw.DimArticle da
    ON da.ArticleKey = f.ArticleKey
LEFT JOIN dw.DimVenue dv
    ON dv.VenueKey = f.VenueKey
LEFT JOIN dw.DimPublicationStatus dps
    ON dps.PublicationStatusKey = f.PublicationStatusKey
LEFT JOIN dw.DimAcademicTerm dat
    ON dat.AcademicTermKey = f.AcademicTermKey
LEFT JOIN dw.DimResearchLine drl
    ON drl.ResearchLineKey = f.ResearchLineKey
LEFT JOIN dw.DimField df
    ON df.FieldHierarchyKey = f.FieldHierarchyKey
LEFT JOIN dw.DimDate dc
    ON dc.DateKey = f.CreatedDateKey
LEFT JOIN dw.DimDate dp
    ON dp.DateKey = f.PublishedDateKey;
GO
/****** Object:  View [dw].[vw_Articles_ByYear]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Articles_ByYear]
AS
SELECT
    d.YearNumber,
    COUNT(*) AS TotalArticles,
    SUM(CASE WHEN f.IsOpenAccessFlag = 1 THEN 1 ELSE 0 END) AS OpenAccessArticles,
    SUM(CASE WHEN f.IsProjectResultFlag = 1 THEN 1 ELSE 0 END) AS ProjectResultArticles,
    SUM(CASE WHEN f.HasInterculturalFlag = 1 THEN 1 ELSE 0 END) AS InterculturalArticles
FROM dw.FactArticlePublication f
INNER JOIN dw.DimDate d
    ON d.DateKey = f.CreatedDateKey
GROUP BY d.YearNumber;
GO
/****** Object:  View [dw].[vw_Articles_ByPublicationStatus]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Articles_ByPublicationStatus]
AS
SELECT
    ISNULL(dps.Name, N'SIN ESTADO') AS PublicationStatus,
    COUNT(*) AS TotalArticles
FROM dw.FactArticlePublication f
LEFT JOIN dw.DimPublicationStatus dps
    ON dps.PublicationStatusKey = f.PublicationStatusKey
GROUP BY dps.Name;
GO
/****** Object:  View [dw].[vw_Articles_ByResearchLine]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Articles_ByResearchLine]
AS
SELECT
    ISNULL(drl.Name, N'SIN LÍNEA') AS ResearchLine,
    COUNT(*) AS TotalArticles
FROM dw.FactArticlePublication f
LEFT JOIN dw.DimResearchLine drl
    ON drl.ResearchLineKey = f.ResearchLineKey
GROUP BY drl.Name;
GO
/****** Object:  View [dw].[vw_Articles_ByField]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Articles_ByField]
AS
SELECT
    ISNULL(df.BroadFieldName, N'SIN ÁREA AMPLIA') AS BroadField,
    ISNULL(df.SpecificFieldName, N'SIN ÁREA ESPECÍFICA') AS SpecificField,
    ISNULL(df.DetailedFieldName, N'SIN ÁREA DETALLADA') AS DetailedField,
    COUNT(*) AS TotalArticles
FROM dw.FactArticlePublication f
LEFT JOIN dw.DimField df
    ON df.FieldHierarchyKey = f.FieldHierarchyKey
GROUP BY
    df.BroadFieldName,
    df.SpecificFieldName,
    df.DetailedFieldName;
GO
/****** Object:  View [dw].[vw_Articles_ByVenue]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Articles_ByVenue]
AS
SELECT
    ISNULL(dv.Name, N'SIN VENUE') AS VenueName,
    ISNULL(dv.VenueType, N'SIN TIPO') AS VenueType,
    COUNT(*) AS TotalArticles
FROM dw.FactArticlePublication f
LEFT JOIN dw.DimVenue dv
    ON dv.VenueKey = f.VenueKey
GROUP BY dv.Name, dv.VenueType;
GO
/****** Object:  View [dw].[vw_OpenAccess_ByYear]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_OpenAccess_ByYear]
AS
SELECT
    d.YearNumber,
    SUM(CASE WHEN f.IsOpenAccessFlag = 1 THEN 1 ELSE 0 END) AS OpenAccessArticles,
    SUM(CASE WHEN f.IsOpenAccessFlag = 0 THEN 1 ELSE 0 END) AS NonOpenAccessArticles
FROM dw.FactArticlePublication f
INNER JOIN dw.DimDate d
    ON d.DateKey = f.CreatedDateKey
GROUP BY d.YearNumber;
GO
/****** Object:  Table [dw].[DimAuthor]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimAuthor](
	[AuthorKey] [int] IDENTITY(1,1) NOT NULL,
	[ArticleParticipantId_OLTP] [int] NOT NULL,
	[Identificacion] [nvarchar](100) NULL,
	[Nombre] [nvarchar](300) NOT NULL,
	[Participacion] [nvarchar](150) NULL,
	[ParticipantType] [nvarchar](50) NULL,
	[InstitutionalPersonId] [int] NULL,
	[Email] [nvarchar](200) NULL,
	[Orcid] [nvarchar](50) NULL,
	[Affiliation] [nvarchar](300) NULL,
	[ExternalAuthorId] [nvarchar](150) NULL,
	[IsPrimaryAuthor] [bit] NOT NULL,
	[CreatedAtSource] [datetime2](7) NULL,
	[UpdatedAtSource] [datetime2](7) NULL,
	[ValidFrom] [datetime2](7) NOT NULL,
	[ValidTo] [datetime2](7) NULL,
	[IsCurrent] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AuthorKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[FactArticleAuthor]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactArticleAuthor](
	[FactArticleAuthorId] [bigint] IDENTITY(1,1) NOT NULL,
	[ArticleKey] [int] NOT NULL,
	[AuthorKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[ResearchLineKey] [int] NULL,
	[FieldHierarchyKey] [int] NULL,
	[RegistrationSourceKey] [int] NULL,
	[AuthorCount] [int] NOT NULL,
	[IsPrimaryAuthorFlag] [bit] NOT NULL,
	[AuthorOrder] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[FactArticleAuthorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_Authors_Production]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   2. AUTORÍA Y PARTICIPACIÓN
   ========================================================= */

CREATE   VIEW [dw].[vw_Authors_Production]
AS
SELECT
    da.Nombre,
    da.Email,
    da.Orcid,
    da.Affiliation,
    COUNT(*) AS TotalParticipations,
    SUM(CASE WHEN f.IsPrimaryAuthorFlag = 1 THEN 1 ELSE 0 END) AS AsPrimaryAuthor
FROM dw.FactArticleAuthor f
INNER JOIN dw.DimAuthor da
    ON da.AuthorKey = f.AuthorKey
GROUP BY
    da.Nombre,
    da.Email,
    da.Orcid,
    da.Affiliation;
GO
/****** Object:  View [dw].[vw_Authors_WithOrcid]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Authors_WithOrcid]
AS
SELECT
    da.Nombre,
    da.Email,
    da.Orcid,
    da.Affiliation
FROM dw.DimAuthor da
WHERE da.Orcid IS NOT NULL
  AND LTRIM(RTRIM(da.Orcid)) <> N'';
GO
/****** Object:  View [dw].[vw_Authors_UniqueProduction]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Authors_UniqueProduction]
AS
SELECT
    COALESCE(NULLIF(LTRIM(RTRIM(da.Orcid)), ''),
             NULLIF(LTRIM(RTRIM(da.Identificacion)), ''),
             NULLIF(LTRIM(RTRIM(da.Email)), ''),
             UPPER(LTRIM(RTRIM(da.Nombre)))) AS AuthorIdentity,
    MAX(da.Nombre) AS AuthorName,
    MAX(da.Email) AS Email,
    MAX(da.Orcid) AS Orcid,
    COUNT(DISTINCT f.ArticleKey) AS TotalArticles,
    SUM(CASE WHEN f.IsPrimaryAuthorFlag = 1 THEN 1 ELSE 0 END) AS AsPrimaryAuthor
FROM dw.FactArticleAuthor f
INNER JOIN dw.DimAuthor da
    ON da.AuthorKey = f.AuthorKey
GROUP BY
    COALESCE(NULLIF(LTRIM(RTRIM(da.Orcid)), ''),
             NULLIF(LTRIM(RTRIM(da.Identificacion)), ''),
             NULLIF(LTRIM(RTRIM(da.Email)), ''),
             UPPER(LTRIM(RTRIM(da.Nombre))));
GO
/****** Object:  View [dw].[vw_Authors_ByResearchLine]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Authors_ByResearchLine]
AS
SELECT
    ISNULL(drl.Name, N'SIN LÍNEA') AS ResearchLine,
    da.Nombre,
    COUNT(*) AS TotalParticipations
FROM dw.FactArticleAuthor f
INNER JOIN dw.DimAuthor da
    ON da.AuthorKey = f.AuthorKey
LEFT JOIN dw.DimResearchLine drl
    ON drl.ResearchLineKey = f.ResearchLineKey
GROUP BY
    drl.Name,
    da.Nombre;
GO
/****** Object:  View [dw].[vw_AverageAuthorsPerArticle]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_AverageAuthorsPerArticle]
AS
SELECT
    AVG(CAST(x.TotalAuthors AS DECIMAL(18,2))) AS AvgAuthorsPerArticle
FROM (
    SELECT
        f.ArticleKey,
        COUNT(*) AS TotalAuthors
    FROM dw.FactArticleAuthor f
    GROUP BY f.ArticleKey
) x;
GO
/****** Object:  Table [dw].[FactVenueMetricYear]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactVenueMetricYear](
	[FactVenueMetricYearId] [bigint] IDENTITY(1,1) NOT NULL,
	[VenueKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[MetricCount] [int] NOT NULL,
	[SJR] [decimal](10, 4) NULL,
	[CiteScore] [decimal](10, 4) NULL,
	[HIndex] [int] NULL,
	[Quartile] [nvarchar](10) NULL,
PRIMARY KEY CLUSTERED 
(
	[FactVenueMetricYearId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[FactArticleIndexing]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactArticleIndexing](
	[FactArticleIndexingId] [bigint] IDENTITY(1,1) NOT NULL,
	[ArticleKey] [int] NOT NULL,
	[IndexingSourceKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[IndexingCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactArticleIndexingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_Articles_ByIndexingSource]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [dw].[vw_Articles_ByIndexingSource]
AS
SELECT
    dis.Name AS IndexingSourceName,
    SUM(f.IndexingCount) AS TotalArticles
FROM dw.FactArticleIndexing f
INNER JOIN dw.DimIndexingSource dis
    ON dis.IndexingSourceKey = f.IndexingSourceKey
GROUP BY dis.Name;
GO
/****** Object:  View [dw].[vw_VenueMetrics_ByYear]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   3. METRÍCAS DE VENUES
   ========================================================= */

CREATE   VIEW [dw].[vw_VenueMetrics_ByYear]
AS
SELECT
    d.YearNumber,
    dv.Name AS VenueName,
    dv.VenueType,
    f.SJR,
    f.CiteScore,
    f.HIndex,
    f.Quartile
FROM dw.FactVenueMetricYear f
INNER JOIN dw.DimVenue dv
    ON dv.VenueKey = f.VenueKey
INNER JOIN dw.DimDate d
    ON d.DateKey = f.DateKey;
GO
/****** Object:  View [dw].[vw_QuartileDistribution]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_QuartileDistribution]
AS
SELECT
    ISNULL(f.Quartile, N'SIN CUARTIL') AS Quartile,
    COUNT(*) AS TotalVenues
FROM dw.FactVenueMetricYear f
GROUP BY f.Quartile;
GO
/****** Object:  Table [dw].[DimUser]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimUser](
	[UserKey] [int] IDENTITY(1,1) NOT NULL,
	[UserId_OLTP] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[FullName] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[UserKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimRegistrationSource]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimRegistrationSource](
	[RegistrationSourceKey] [int] IDENTITY(1,1) NOT NULL,
	[SourceCode] [nvarchar](50) NOT NULL,
	[SourceName] [nvarchar](100) NOT NULL,
	[SourceCategory] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[RegistrationSourceKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimBatchStatus]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimBatchStatus](
	[BatchStatusKey] [int] IDENTITY(1,1) NOT NULL,
	[StatusCode] [nvarchar](30) NOT NULL,
	[StatusName] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BatchStatusKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_Batches_Summary]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   4. CARGA MASIVA Y CALIDAD DE DATOS
   ========================================================= */

CREATE   VIEW [dw].[vw_Batches_Summary]
AS
SELECT
    f.BatchId_OLTP,
    ds.SourceName,
    dbs.StatusName,
    du.FullName AS CreatedBy,
    d1.FullDate AS StartDate,
    d2.FullDate AS FinishDate,
    f.TotalRows,
    f.SuccessfulRows,
    f.ErrorRows,
    f.DurationSeconds
FROM dw.FactRegistrationBatch f
LEFT JOIN dw.DimRegistrationSource ds
    ON ds.RegistrationSourceKey = f.RegistrationSourceKey
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = f.BatchStatusKey
LEFT JOIN dw.DimUser du
    ON du.UserKey = f.UserKey
LEFT JOIN dw.DimDate d1
    ON d1.DateKey = f.StartDateKey
LEFT JOIN dw.DimDate d2
    ON d2.DateKey = f.FinishDateKey;
GO
/****** Object:  View [dw].[vw_Batches_ByStatus]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Batches_ByStatus]
AS
SELECT
    dbs.StatusName,
    COUNT(*) AS TotalBatches,
    SUM(f.TotalRows) AS TotalRows,
    SUM(f.SuccessfulRows) AS SuccessfulRows,
    SUM(f.ErrorRows) AS ErrorRows
FROM dw.FactRegistrationBatch f
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = f.BatchStatusKey
GROUP BY dbs.StatusName;
GO
/****** Object:  View [dw].[vw_Batches_BySource]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Batches_BySource]
AS
SELECT
    ds.SourceName,
    COUNT(*) AS TotalBatches,
    SUM(f.TotalRows) AS TotalRows,
    SUM(f.SuccessfulRows) AS SuccessfulRows,
    SUM(f.ErrorRows) AS ErrorRows
FROM dw.FactRegistrationBatch f
LEFT JOIN dw.DimRegistrationSource ds
    ON ds.RegistrationSourceKey = f.RegistrationSourceKey
GROUP BY ds.SourceName;
GO
/****** Object:  Table [dw].[FactRegistrationRow]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactRegistrationRow](
	[FactRegistrationRowId] [bigint] IDENTITY(1,1) NOT NULL,
	[BatchId_OLTP] [int] NOT NULL,
	[BatchRowId_OLTP] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[RegistrationSourceKey] [int] NULL,
	[BatchStatusKey] [int] NULL,
	[RegistrationMatrixKey] [int] NULL,
	[RowQty] [int] NOT NULL,
	[IsValidFlag] [bit] NOT NULL,
	[IsProcessedFlag] [bit] NOT NULL,
	[IsErrorFlag] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactRegistrationRowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_RegistrationRows_Status]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_RegistrationRows_Status]
AS
SELECT
    SUM(CASE WHEN f.IsValidFlag = 1 THEN 1 ELSE 0 END) AS ValidRows,
    SUM(CASE WHEN f.IsProcessedFlag = 1 THEN 1 ELSE 0 END) AS ProcessedRows,
    SUM(CASE WHEN f.IsErrorFlag = 1 THEN 1 ELSE 0 END) AS ErrorRows
FROM dw.FactRegistrationRow f;
GO
/****** Object:  Table [dw].[DimValidationError]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimValidationError](
	[ValidationErrorKey] [int] IDENTITY(1,1) NOT NULL,
	[ErrorCode] [nvarchar](100) NOT NULL,
	[Severity] [nvarchar](20) NOT NULL,
	[ErrorMessageTemplate] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ValidationErrorKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[FactValidationError]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactValidationError](
	[FactValidationErrorId] [bigint] IDENTITY(1,1) NOT NULL,
	[BatchId_OLTP] [int] NOT NULL,
	[BatchRowId_OLTP] [int] NULL,
	[DateKey] [int] NOT NULL,
	[ValidationErrorKey] [int] NOT NULL,
	[DynamicFieldKey] [int] NULL,
	[RegistrationSourceKey] [int] NULL,
	[UserKey] [int] NULL,
	[ErrorCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactValidationErrorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_ValidationErrors_ByCode]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_ValidationErrors_ByCode]
AS
SELECT
    dve.ErrorCode,
    dve.Severity,
    COUNT(*) AS TotalErrors
FROM dw.FactValidationError f
INNER JOIN dw.DimValidationError dve
    ON dve.ValidationErrorKey = f.ValidationErrorKey
GROUP BY dve.ErrorCode, dve.Severity;
GO
/****** Object:  Table [dw].[DimDynamicField]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimDynamicField](
	[DynamicFieldKey] [int] IDENTITY(1,1) NOT NULL,
	[FieldId_OLTP] [int] NOT NULL,
	[EntityName] [nvarchar](100) NOT NULL,
	[FieldKey] [nvarchar](100) NOT NULL,
	[FieldLabel] [nvarchar](150) NOT NULL,
	[DataType] [nvarchar](50) NOT NULL,
	[SourceType] [nvarchar](30) NOT NULL,
	[PhysicalTableName] [nvarchar](100) NULL,
	[PhysicalColumnName] [nvarchar](100) NULL,
	[ReferenceTableName] [nvarchar](100) NULL,
	[IsSystemField] [bit] NOT NULL,
	[IsDynamic] [bit] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[IsVisible] [bit] NOT NULL,
	[IsEditable] [bit] NOT NULL,
	[IsFilterable] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[MaxLength] [int] NULL,
	[Placeholder] [nvarchar](200) NULL,
	[HelpText] [nvarchar](500) NULL,
	[DefaultValue] [nvarchar](200) NULL,
	[ValidationRule] [nvarchar](500) NULL,
	[IsAnalytical] [bit] NOT NULL,
	[IsEnabledForReporting] [bit] NOT NULL,
	[IsGroupingEnabled] [bit] NOT NULL,
	[IsPromotedToCore] [bit] NOT NULL,
	[ReportDisplayName] [nvarchar](150) NULL,
	[AnalyticalCategory] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[DynamicFieldKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_ValidationErrors_ByField]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_ValidationErrors_ByField]
AS
SELECT
    ISNULL(ddf.FieldLabel, N'SIN CAMPO') AS FieldLabel,
    COUNT(*) AS TotalErrors
FROM dw.FactValidationError f
LEFT JOIN dw.DimDynamicField ddf
    ON ddf.DynamicFieldKey = f.DynamicFieldKey
GROUP BY ddf.FieldLabel;
GO
/****** Object:  View [dw].[vw_ErrorRate_ByBatch]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_ErrorRate_ByBatch]
AS
SELECT
    f.BatchId_OLTP,
    f.TotalRows,
    f.ErrorRows,
    CAST(
        CASE
            WHEN f.TotalRows = 0 THEN 0
            ELSE (f.ErrorRows * 100.0) / f.TotalRows
        END
        AS DECIMAL(10,2)
    ) AS ErrorRatePercent
FROM dw.FactRegistrationBatch f;
GO
/****** Object:  Table [dw].[DimWorkflow]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimWorkflow](
	[WorkflowKey] [int] IDENTITY(1,1) NOT NULL,
	[WorkflowDefinitionId_OLTP] [int] NOT NULL,
	[WorkflowCode] [nvarchar](100) NOT NULL,
	[WorkflowName] [nvarchar](200) NOT NULL,
	[EntityName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkflowKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dw].[DimWorkflowStage]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimWorkflowStage](
	[WorkflowStageKey] [int] IDENTITY(1,1) NOT NULL,
	[WorkflowStageDefinitionId_OLTP] [int] NOT NULL,
	[WorkflowDefinitionId_OLTP] [int] NOT NULL,
	[StageKey] [nvarchar](100) NOT NULL,
	[StageName] [nvarchar](200) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[StageGroupKey] [nvarchar](100) NULL,
	[StageGroupName] [nvarchar](200) NULL,
	[ResponsibleRoleId_OLTP] [nvarchar](450) NULL,
	[CanEditData] [bit] NOT NULL,
	[CanReturn] [bit] NOT NULL,
	[CanApprove] [bit] NOT NULL,
	[CanProcessBatch] [bit] NOT NULL,
	[IsFinalStage] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkflowStageKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[FactWorkflowStage]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactWorkflowStage](
	[FactWorkflowStageId] [bigint] IDENTITY(1,1) NOT NULL,
	[WorkflowInstanceId_OLTP] [int] NOT NULL,
	[WorkflowStageInstanceId_OLTP] [int] NOT NULL,
	[BatchId_OLTP] [int] NOT NULL,
	[WorkflowKey] [int] NOT NULL,
	[WorkflowStageKey] [int] NOT NULL,
	[StartDateKey] [int] NULL,
	[EndDateKey] [int] NULL,
	[AssignedUserKey] [int] NULL,
	[ApprovedByUserKey] [int] NULL,
	[BatchStatusKey] [int] NULL,
	[StageCount] [int] NOT NULL,
	[StageDurationSeconds] [int] NULL,
	[ApprovedFlag] [bit] NOT NULL,
	[ReturnedFlag] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactWorkflowStageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_WorkflowStages_Summary]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   5. WORKFLOW INSTITUCIONAL
   ========================================================= */

CREATE   VIEW [dw].[vw_WorkflowStages_Summary]
AS
SELECT
    dwf.WorkflowName,
    dws.StageName,
    COUNT(*) AS TotalStageExecutions,
    AVG(CAST(f.StageDurationSeconds AS DECIMAL(18,2))) AS AvgDurationSeconds,
    SUM(CASE WHEN f.ApprovedFlag = 1 THEN 1 ELSE 0 END) AS ApprovedCount,
    SUM(CASE WHEN f.ReturnedFlag = 1 THEN 1 ELSE 0 END) AS ReturnedCount
FROM dw.FactWorkflowStage f
INNER JOIN dw.DimWorkflow dwf
    ON dwf.WorkflowKey = f.WorkflowKey
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = f.WorkflowStageKey
GROUP BY
    dwf.WorkflowName,
    dws.StageName;
GO
/****** Object:  Table [dw].[FactWorkflowAction]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactWorkflowAction](
	[FactWorkflowActionId] [bigint] IDENTITY(1,1) NOT NULL,
	[WorkflowActionLogId_OLTP] [int] NOT NULL,
	[WorkflowInstanceId_OLTP] [int] NOT NULL,
	[WorkflowStageInstanceId_OLTP] [int] NULL,
	[BatchId_OLTP] [int] NOT NULL,
	[WorkflowKey] [int] NOT NULL,
	[WorkflowStageKey] [int] NULL,
	[DateKey] [int] NOT NULL,
	[UserKey] [int] NULL,
	[BatchStatusKey] [int] NULL,
	[ActionType] [nvarchar](50) NOT NULL,
	[FromStatus] [nvarchar](30) NULL,
	[ToStatus] [nvarchar](30) NULL,
	[ActionCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactWorkflowActionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_WorkflowActions_ByUser]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_WorkflowActions_ByUser]
AS
SELECT
    ISNULL(du.FullName, N'SIN USUARIO') AS UserName,
    f.ActionType,
    COUNT(*) AS TotalActions
FROM dw.FactWorkflowAction f
LEFT JOIN dw.DimUser du
    ON du.UserKey = f.UserKey
GROUP BY
    du.FullName,
    f.ActionType;
GO
/****** Object:  View [dw].[vw_WorkflowActions_ByStage]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_WorkflowActions_ByStage]
AS
SELECT
    ISNULL(dws.StageName, N'SIN ETAPA') AS StageName,
    f.ActionType,
    COUNT(*) AS TotalActions
FROM dw.FactWorkflowAction f
LEFT JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = f.WorkflowStageKey
GROUP BY
    dws.StageName,
    f.ActionType;
GO
/****** Object:  View [dw].[vw_WorkflowDuration_ByStage]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_WorkflowDuration_ByStage]
AS
SELECT
    dws.StageName,
    AVG(CAST(f.StageDurationSeconds AS DECIMAL(18,2))) AS AvgDurationSeconds,
    MIN(f.StageDurationSeconds) AS MinDurationSeconds,
    MAX(f.StageDurationSeconds) AS MaxDurationSeconds
FROM dw.FactWorkflowStage f
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = f.WorkflowStageKey
GROUP BY dws.StageName;
GO
/****** Object:  View [dw].[vw_Workflow_CurrentPipeline]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Workflow_CurrentPipeline]
AS
SELECT
    f.BatchId_OLTP,
    dwf.WorkflowName,
    dws.StageName,
    dws.StageGroupName,
    dbs.StatusName AS StageStatus,
    f.StartDateKey,
    f.EndDateKey,
    assigned.FullName AS AssignedTo,
    approved.FullName AS ApprovedBy,
    f.StageDurationSeconds,
    f.ApprovedFlag,
    f.ReturnedFlag
FROM dw.FactWorkflowStage f
INNER JOIN dw.DimWorkflow dwf
    ON dwf.WorkflowKey = f.WorkflowKey
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = f.WorkflowStageKey
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = f.BatchStatusKey
LEFT JOIN dw.DimUser assigned
    ON assigned.UserKey = f.AssignedUserKey
LEFT JOIN dw.DimUser approved
    ON approved.UserKey = f.ApprovedByUserKey;
GO
/****** Object:  View [dw].[vw_Workflow_Batches_ByCurrentStage]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_Workflow_Batches_ByCurrentStage]
AS
WITH RankedStages AS (
    SELECT
        f.*,
        ROW_NUMBER() OVER (
            PARTITION BY f.BatchId_OLTP
            ORDER BY
                CASE WHEN f.EndDateKey IS NULL THEN 0 ELSE 1 END,
                f.EndDateKey DESC,
                f.StartDateKey DESC,
                f.FactWorkflowStageId DESC
        ) AS rn
    FROM dw.FactWorkflowStage f
)
SELECT
    rs.BatchId_OLTP,
    dwf.WorkflowName,
    dws.StageName,
    dws.StageGroupName,
    dbs.StatusName AS StageStatus,
    rs.StartDateKey,
    rs.EndDateKey,
    rs.StageDurationSeconds,
    rs.ApprovedFlag,
    rs.ReturnedFlag
FROM RankedStages rs
INNER JOIN dw.DimWorkflow dwf
    ON dwf.WorkflowKey = rs.WorkflowKey
INNER JOIN dw.DimWorkflowStage dws
    ON dws.WorkflowStageKey = rs.WorkflowStageKey
LEFT JOIN dw.DimBatchStatus dbs
    ON dbs.BatchStatusKey = rs.BatchStatusKey
WHERE rs.rn = 1;
GO
/****** Object:  Table [dw].[DimRegistrationMatrix]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimRegistrationMatrix](
	[RegistrationMatrixKey] [int] IDENTITY(1,1) NOT NULL,
	[RegistrationMatrixId_OLTP] [int] NOT NULL,
	[MatrixName] [nvarchar](200) NOT NULL,
	[EntityName] [nvarchar](100) NOT NULL,
	[MatrixStatus] [nvarchar](30) NOT NULL,
	[Notes] [nvarchar](1000) NULL,
	[CreatedByUserId_OLTP] [nvarchar](450) NULL,
	[CreatedAtSource] [datetime2](7) NOT NULL,
	[UpdatedAtSource] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[RegistrationMatrixKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dw].[FactRegistrationMatrix]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactRegistrationMatrix](
	[FactRegistrationMatrixId] [bigint] IDENTITY(1,1) NOT NULL,
	[RegistrationMatrixKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[UserKey] [int] NULL,
	[BatchStatusKey] [int] NULL,
	[MatrixCount] [int] NOT NULL,
	[LinkedBatchCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactRegistrationMatrixId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_RegistrationMatrices_Summary]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   6. MATRICES DE REGISTRO
   ========================================================= */

CREATE   VIEW [dw].[vw_RegistrationMatrices_Summary]
AS
SELECT
    drm.MatrixName,
    drm.EntityName,
    drm.MatrixStatus,
    du.FullName AS CreatedBy,
    dd.FullDate AS CreatedDate,
    f.MatrixCount,
    f.LinkedBatchCount
FROM dw.FactRegistrationMatrix f
INNER JOIN dw.DimRegistrationMatrix drm
    ON drm.RegistrationMatrixKey = f.RegistrationMatrixKey
LEFT JOIN dw.DimUser du
    ON du.UserKey = f.UserKey
LEFT JOIN dw.DimDate dd
    ON dd.DateKey = f.DateKey;
GO
/****** Object:  View [dw].[vw_DynamicFields_EnabledForReporting]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   7. CAMPOS DINÁMICOS
   ========================================================= */

CREATE   VIEW [dw].[vw_DynamicFields_EnabledForReporting]
AS
SELECT
    DynamicFieldKey,
    FieldId_OLTP,
    EntityName,
    FieldKey,
    FieldLabel,
    DataType,
    IsFilterable,
    IsAnalytical,
    IsEnabledForReporting,
    IsGroupingEnabled,
    IsPromotedToCore,
    ReportDisplayName,
    AnalyticalCategory
FROM dw.DimDynamicField
WHERE IsEnabledForReporting = 1
   OR IsAnalytical = 1;
GO
/****** Object:  Table [dw].[FactArticleDynamicAttribute]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactArticleDynamicAttribute](
	[FactArticleDynamicAttributeId] [bigint] IDENTITY(1,1) NOT NULL,
	[ArticleKey] [int] NOT NULL,
	[DynamicFieldKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[ValueString] [nvarchar](max) NULL,
	[ValueInt] [int] NULL,
	[ValueDecimal] [decimal](18, 4) NULL,
	[ValueDate] [datetime2](7) NULL,
	[ValueBit] [bit] NULL,
	[ValueJson] [nvarchar](max) NULL,
	[AttributeCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactArticleDynamicAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_ArticleDynamicAttributes]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_ArticleDynamicAttributes]
AS
SELECT
    da.Title,
    ddf.FieldLabel,
    f.ValueString,
    f.ValueInt,
    f.ValueDecimal,
    f.ValueDate,
    f.ValueBit,
    f.ValueJson
FROM dw.FactArticleDynamicAttribute f
INNER JOIN dw.DimArticle da
    ON da.ArticleKey = f.ArticleKey
INNER JOIN dw.DimDynamicField ddf
    ON ddf.DynamicFieldKey = f.DynamicFieldKey;
GO
/****** Object:  Table [dw].[FactParticipantDynamicAttribute]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[FactParticipantDynamicAttribute](
	[FactParticipantDynamicAttributeId] [bigint] IDENTITY(1,1) NOT NULL,
	[AuthorKey] [int] NOT NULL,
	[DynamicFieldKey] [int] NOT NULL,
	[DateKey] [int] NOT NULL,
	[ValueString] [nvarchar](max) NULL,
	[ValueInt] [int] NULL,
	[ValueDecimal] [decimal](18, 4) NULL,
	[ValueDate] [datetime2](7) NULL,
	[ValueBit] [bit] NULL,
	[ValueJson] [nvarchar](max) NULL,
	[AttributeCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FactParticipantDynamicAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dw].[vw_ParticipantDynamicAttributes]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_ParticipantDynamicAttributes]
AS
SELECT
    da.Nombre,
    ddf.FieldLabel,
    f.ValueString,
    f.ValueInt,
    f.ValueDecimal,
    f.ValueDate,
    f.ValueBit,
    f.ValueJson
FROM dw.FactParticipantDynamicAttribute f
INNER JOIN dw.DimAuthor da
    ON da.AuthorKey = f.AuthorKey
INNER JOIN dw.DimDynamicField ddf
    ON ddf.DynamicFieldKey = f.DynamicFieldKey;
GO
/****** Object:  View [dw].[vw_KPI_Workflow]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   VIEW [dw].[vw_KPI_Workflow]
AS
SELECT
    COUNT(*) AS TotalStageExecutions,
    AVG(CAST(StageDurationSeconds AS DECIMAL(18,2))) AS AvgStageDurationSeconds,
    SUM(CASE WHEN ApprovedFlag = 1 THEN 1 ELSE 0 END) AS ApprovedStages,
    SUM(CASE WHEN ReturnedFlag = 1 THEN 1 ELSE 0 END) AS ReturnedStages
FROM dw.FactWorkflowStage;
GO
/****** Object:  Table [dw].[DimSystemRole]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dw].[DimSystemRole](
	[SystemRoleKey] [int] IDENTITY(1,1) NOT NULL,
	[RoleId_OLTP] [nvarchar](450) NOT NULL,
	[RoleName] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
PRIMARY KEY CLUSTERED 
(
	[SystemRoleKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [etl].[EtlRowAudit]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [etl].[EtlRowAudit](
	[EtlRowAuditId] [bigint] IDENTITY(1,1) NOT NULL,
	[EtlRunId] [bigint] NOT NULL,
	[SourceEntity] [nvarchar](100) NOT NULL,
	[SourceId] [nvarchar](100) NOT NULL,
	[TargetEntity] [nvarchar](100) NOT NULL,
	[TargetKey] [nvarchar](100) NULL,
	[Status] [nvarchar](30) NOT NULL,
	[Message] [nvarchar](1000) NULL,
PRIMARY KEY CLUSTERED 
(
	[EtlRowAuditId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [etl].[EtlRun]    Script Date: 13/04/2026 03:44:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [etl].[EtlRun](
	[EtlRunId] [bigint] IDENTITY(1,1) NOT NULL,
	[ProcessName] [nvarchar](100) NOT NULL,
	[StartedAt] [datetime2](7) NOT NULL,
	[FinishedAt] [datetime2](7) NULL,
	[Status] [nvarchar](30) NOT NULL,
	[Notes] [nvarchar](1000) NULL,
PRIMARY KEY CLUSTERED 
(
	[EtlRunId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dw].[DimArticle] ADD  CONSTRAINT [DF_DimArticle_ValidFrom]  DEFAULT (sysutcdatetime()) FOR [ValidFrom]
GO
ALTER TABLE [dw].[DimArticle] ADD  CONSTRAINT [DF_DimArticle_IsCurrent]  DEFAULT ((1)) FOR [IsCurrent]
GO
ALTER TABLE [dw].[DimAuthor] ADD  CONSTRAINT [DF_DimAuthor_ValidFrom]  DEFAULT (sysutcdatetime()) FOR [ValidFrom]
GO
ALTER TABLE [dw].[DimAuthor] ADD  CONSTRAINT [DF_DimAuthor_IsCurrent]  DEFAULT ((1)) FOR [IsCurrent]
GO
ALTER TABLE [dw].[DimDynamicField] ADD  CONSTRAINT [DF_DimDynamicField_IsAnalytical]  DEFAULT ((0)) FOR [IsAnalytical]
GO
ALTER TABLE [dw].[DimDynamicField] ADD  CONSTRAINT [DF_DimDynamicField_IsEnabledForReporting]  DEFAULT ((0)) FOR [IsEnabledForReporting]
GO
ALTER TABLE [dw].[DimDynamicField] ADD  CONSTRAINT [DF_DimDynamicField_IsGroupingEnabled]  DEFAULT ((0)) FOR [IsGroupingEnabled]
GO
ALTER TABLE [dw].[DimDynamicField] ADD  CONSTRAINT [DF_DimDynamicField_IsPromotedToCore]  DEFAULT ((0)) FOR [IsPromotedToCore]
GO
ALTER TABLE [dw].[FactArticleAuthor] ADD  CONSTRAINT [DF_FactArticleAuthor_AuthorCount]  DEFAULT ((1)) FOR [AuthorCount]
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute] ADD  CONSTRAINT [DF_FactArticleDynamicAttribute_AttributeCount]  DEFAULT ((1)) FOR [AttributeCount]
GO
ALTER TABLE [dw].[FactArticleIndexing] ADD  CONSTRAINT [DF_FactArticleIndexing_IndexingCount]  DEFAULT ((1)) FOR [IndexingCount]
GO
ALTER TABLE [dw].[FactArticlePublication] ADD  CONSTRAINT [DF_FactArticlePublication_ArticleCount]  DEFAULT ((1)) FOR [ArticleCount]
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute] ADD  CONSTRAINT [DF_FactParticipantDynamicAttribute_AttributeCount]  DEFAULT ((1)) FOR [AttributeCount]
GO
ALTER TABLE [dw].[FactRegistrationBatch] ADD  CONSTRAINT [DF_FactRegistrationBatch_BatchCount]  DEFAULT ((1)) FOR [BatchCount]
GO
ALTER TABLE [dw].[FactRegistrationMatrix] ADD  CONSTRAINT [DF_FactRegistrationMatrix_MatrixCount]  DEFAULT ((1)) FOR [MatrixCount]
GO
ALTER TABLE [dw].[FactRegistrationMatrix] ADD  CONSTRAINT [DF_FactRegistrationMatrix_LinkedBatchCount]  DEFAULT ((0)) FOR [LinkedBatchCount]
GO
ALTER TABLE [dw].[FactRegistrationRow] ADD  CONSTRAINT [DF_FactRegistrationRow_RowQty]  DEFAULT ((1)) FOR [RowQty]
GO
ALTER TABLE [dw].[FactValidationError] ADD  CONSTRAINT [DF_FactValidationError_ErrorCount]  DEFAULT ((1)) FOR [ErrorCount]
GO
ALTER TABLE [dw].[FactVenueMetricYear] ADD  CONSTRAINT [DF_FactVenueMetricYear_MetricCount]  DEFAULT ((1)) FOR [MetricCount]
GO
ALTER TABLE [dw].[FactWorkflowAction] ADD  CONSTRAINT [DF_FactWorkflowAction_ActionCount]  DEFAULT ((1)) FOR [ActionCount]
GO
ALTER TABLE [dw].[FactWorkflowStage] ADD  CONSTRAINT [DF_FactWorkflowStage_StageCount]  DEFAULT ((1)) FOR [StageCount]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimArticle] FOREIGN KEY([ArticleKey])
REFERENCES [dw].[DimArticle] ([ArticleKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimArticle]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimAuthor] FOREIGN KEY([AuthorKey])
REFERENCES [dw].[DimAuthor] ([AuthorKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimAuthor]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimDate]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimField] FOREIGN KEY([FieldHierarchyKey])
REFERENCES [dw].[DimField] ([FieldHierarchyKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimField]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimRegistrationSource] FOREIGN KEY([RegistrationSourceKey])
REFERENCES [dw].[DimRegistrationSource] ([RegistrationSourceKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimRegistrationSource]
GO
ALTER TABLE [dw].[FactArticleAuthor]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleAuthor_DimResearchLine] FOREIGN KEY([ResearchLineKey])
REFERENCES [dw].[DimResearchLine] ([ResearchLineKey])
GO
ALTER TABLE [dw].[FactArticleAuthor] CHECK CONSTRAINT [FK_FactArticleAuthor_DimResearchLine]
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleDynamicAttribute_DimArticle] FOREIGN KEY([ArticleKey])
REFERENCES [dw].[DimArticle] ([ArticleKey])
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute] CHECK CONSTRAINT [FK_FactArticleDynamicAttribute_DimArticle]
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleDynamicAttribute_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute] CHECK CONSTRAINT [FK_FactArticleDynamicAttribute_DimDate]
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleDynamicAttribute_DimDynamicField] FOREIGN KEY([DynamicFieldKey])
REFERENCES [dw].[DimDynamicField] ([DynamicFieldKey])
GO
ALTER TABLE [dw].[FactArticleDynamicAttribute] CHECK CONSTRAINT [FK_FactArticleDynamicAttribute_DimDynamicField]
GO
ALTER TABLE [dw].[FactArticleIndexing]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleIndexing_DimArticle] FOREIGN KEY([ArticleKey])
REFERENCES [dw].[DimArticle] ([ArticleKey])
GO
ALTER TABLE [dw].[FactArticleIndexing] CHECK CONSTRAINT [FK_FactArticleIndexing_DimArticle]
GO
ALTER TABLE [dw].[FactArticleIndexing]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleIndexing_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactArticleIndexing] CHECK CONSTRAINT [FK_FactArticleIndexing_DimDate]
GO
ALTER TABLE [dw].[FactArticleIndexing]  WITH CHECK ADD  CONSTRAINT [FK_FactArticleIndexing_DimIndexingSource] FOREIGN KEY([IndexingSourceKey])
REFERENCES [dw].[DimIndexingSource] ([IndexingSourceKey])
GO
ALTER TABLE [dw].[FactArticleIndexing] CHECK CONSTRAINT [FK_FactArticleIndexing_DimIndexingSource]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimAcademicTerm] FOREIGN KEY([AcademicTermKey])
REFERENCES [dw].[DimAcademicTerm] ([AcademicTermKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimAcademicTerm]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimArticle] FOREIGN KEY([ArticleKey])
REFERENCES [dw].[DimArticle] ([ArticleKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimArticle]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimDateCreated] FOREIGN KEY([CreatedDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimDateCreated]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimDatePublished] FOREIGN KEY([PublishedDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimDatePublished]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimField] FOREIGN KEY([FieldHierarchyKey])
REFERENCES [dw].[DimField] ([FieldHierarchyKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimField]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimPublicationStatus] FOREIGN KEY([PublicationStatusKey])
REFERENCES [dw].[DimPublicationStatus] ([PublicationStatusKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimPublicationStatus]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimRegistrationSource] FOREIGN KEY([RegistrationSourceKey])
REFERENCES [dw].[DimRegistrationSource] ([RegistrationSourceKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimRegistrationSource]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimResearchLine] FOREIGN KEY([ResearchLineKey])
REFERENCES [dw].[DimResearchLine] ([ResearchLineKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimResearchLine]
GO
ALTER TABLE [dw].[FactArticlePublication]  WITH CHECK ADD  CONSTRAINT [FK_FactArticlePublication_DimVenue] FOREIGN KEY([VenueKey])
REFERENCES [dw].[DimVenue] ([VenueKey])
GO
ALTER TABLE [dw].[FactArticlePublication] CHECK CONSTRAINT [FK_FactArticlePublication_DimVenue]
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactParticipantDynamicAttribute_DimAuthor] FOREIGN KEY([AuthorKey])
REFERENCES [dw].[DimAuthor] ([AuthorKey])
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute] CHECK CONSTRAINT [FK_FactParticipantDynamicAttribute_DimAuthor]
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactParticipantDynamicAttribute_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute] CHECK CONSTRAINT [FK_FactParticipantDynamicAttribute_DimDate]
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FactParticipantDynamicAttribute_DimDynamicField] FOREIGN KEY([DynamicFieldKey])
REFERENCES [dw].[DimDynamicField] ([DynamicFieldKey])
GO
ALTER TABLE [dw].[FactParticipantDynamicAttribute] CHECK CONSTRAINT [FK_FactParticipantDynamicAttribute_DimDynamicField]
GO
ALTER TABLE [dw].[FactRegistrationBatch]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationBatch_DimBatchStatus] FOREIGN KEY([BatchStatusKey])
REFERENCES [dw].[DimBatchStatus] ([BatchStatusKey])
GO
ALTER TABLE [dw].[FactRegistrationBatch] CHECK CONSTRAINT [FK_FactRegistrationBatch_DimBatchStatus]
GO
ALTER TABLE [dw].[FactRegistrationBatch]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationBatch_DimDateFinish] FOREIGN KEY([FinishDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactRegistrationBatch] CHECK CONSTRAINT [FK_FactRegistrationBatch_DimDateFinish]
GO
ALTER TABLE [dw].[FactRegistrationBatch]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationBatch_DimDateStart] FOREIGN KEY([StartDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactRegistrationBatch] CHECK CONSTRAINT [FK_FactRegistrationBatch_DimDateStart]
GO
ALTER TABLE [dw].[FactRegistrationBatch]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationBatch_DimRegistrationSource] FOREIGN KEY([RegistrationSourceKey])
REFERENCES [dw].[DimRegistrationSource] ([RegistrationSourceKey])
GO
ALTER TABLE [dw].[FactRegistrationBatch] CHECK CONSTRAINT [FK_FactRegistrationBatch_DimRegistrationSource]
GO
ALTER TABLE [dw].[FactRegistrationBatch]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationBatch_DimUser] FOREIGN KEY([UserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactRegistrationBatch] CHECK CONSTRAINT [FK_FactRegistrationBatch_DimUser]
GO
ALTER TABLE [dw].[FactRegistrationMatrix]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationMatrix_DimBatchStatus] FOREIGN KEY([BatchStatusKey])
REFERENCES [dw].[DimBatchStatus] ([BatchStatusKey])
GO
ALTER TABLE [dw].[FactRegistrationMatrix] CHECK CONSTRAINT [FK_FactRegistrationMatrix_DimBatchStatus]
GO
ALTER TABLE [dw].[FactRegistrationMatrix]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationMatrix_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactRegistrationMatrix] CHECK CONSTRAINT [FK_FactRegistrationMatrix_DimDate]
GO
ALTER TABLE [dw].[FactRegistrationMatrix]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationMatrix_DimRegistrationMatrix] FOREIGN KEY([RegistrationMatrixKey])
REFERENCES [dw].[DimRegistrationMatrix] ([RegistrationMatrixKey])
GO
ALTER TABLE [dw].[FactRegistrationMatrix] CHECK CONSTRAINT [FK_FactRegistrationMatrix_DimRegistrationMatrix]
GO
ALTER TABLE [dw].[FactRegistrationMatrix]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationMatrix_DimUser] FOREIGN KEY([UserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactRegistrationMatrix] CHECK CONSTRAINT [FK_FactRegistrationMatrix_DimUser]
GO
ALTER TABLE [dw].[FactRegistrationRow]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationRow_DimBatchStatus] FOREIGN KEY([BatchStatusKey])
REFERENCES [dw].[DimBatchStatus] ([BatchStatusKey])
GO
ALTER TABLE [dw].[FactRegistrationRow] CHECK CONSTRAINT [FK_FactRegistrationRow_DimBatchStatus]
GO
ALTER TABLE [dw].[FactRegistrationRow]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationRow_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactRegistrationRow] CHECK CONSTRAINT [FK_FactRegistrationRow_DimDate]
GO
ALTER TABLE [dw].[FactRegistrationRow]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationRow_DimRegistrationMatrix] FOREIGN KEY([RegistrationMatrixKey])
REFERENCES [dw].[DimRegistrationMatrix] ([RegistrationMatrixKey])
GO
ALTER TABLE [dw].[FactRegistrationRow] CHECK CONSTRAINT [FK_FactRegistrationRow_DimRegistrationMatrix]
GO
ALTER TABLE [dw].[FactRegistrationRow]  WITH CHECK ADD  CONSTRAINT [FK_FactRegistrationRow_DimRegistrationSource] FOREIGN KEY([RegistrationSourceKey])
REFERENCES [dw].[DimRegistrationSource] ([RegistrationSourceKey])
GO
ALTER TABLE [dw].[FactRegistrationRow] CHECK CONSTRAINT [FK_FactRegistrationRow_DimRegistrationSource]
GO
ALTER TABLE [dw].[FactValidationError]  WITH CHECK ADD  CONSTRAINT [FK_FactValidationError_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactValidationError] CHECK CONSTRAINT [FK_FactValidationError_DimDate]
GO
ALTER TABLE [dw].[FactValidationError]  WITH CHECK ADD  CONSTRAINT [FK_FactValidationError_DimDynamicField] FOREIGN KEY([DynamicFieldKey])
REFERENCES [dw].[DimDynamicField] ([DynamicFieldKey])
GO
ALTER TABLE [dw].[FactValidationError] CHECK CONSTRAINT [FK_FactValidationError_DimDynamicField]
GO
ALTER TABLE [dw].[FactValidationError]  WITH CHECK ADD  CONSTRAINT [FK_FactValidationError_DimRegistrationSource] FOREIGN KEY([RegistrationSourceKey])
REFERENCES [dw].[DimRegistrationSource] ([RegistrationSourceKey])
GO
ALTER TABLE [dw].[FactValidationError] CHECK CONSTRAINT [FK_FactValidationError_DimRegistrationSource]
GO
ALTER TABLE [dw].[FactValidationError]  WITH CHECK ADD  CONSTRAINT [FK_FactValidationError_DimUser] FOREIGN KEY([UserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactValidationError] CHECK CONSTRAINT [FK_FactValidationError_DimUser]
GO
ALTER TABLE [dw].[FactValidationError]  WITH CHECK ADD  CONSTRAINT [FK_FactValidationError_DimValidationError] FOREIGN KEY([ValidationErrorKey])
REFERENCES [dw].[DimValidationError] ([ValidationErrorKey])
GO
ALTER TABLE [dw].[FactValidationError] CHECK CONSTRAINT [FK_FactValidationError_DimValidationError]
GO
ALTER TABLE [dw].[FactVenueMetricYear]  WITH CHECK ADD  CONSTRAINT [FK_FactVenueMetricYear_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactVenueMetricYear] CHECK CONSTRAINT [FK_FactVenueMetricYear_DimDate]
GO
ALTER TABLE [dw].[FactVenueMetricYear]  WITH CHECK ADD  CONSTRAINT [FK_FactVenueMetricYear_DimVenue] FOREIGN KEY([VenueKey])
REFERENCES [dw].[DimVenue] ([VenueKey])
GO
ALTER TABLE [dw].[FactVenueMetricYear] CHECK CONSTRAINT [FK_FactVenueMetricYear_DimVenue]
GO
ALTER TABLE [dw].[FactWorkflowAction]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowAction_DimBatchStatus] FOREIGN KEY([BatchStatusKey])
REFERENCES [dw].[DimBatchStatus] ([BatchStatusKey])
GO
ALTER TABLE [dw].[FactWorkflowAction] CHECK CONSTRAINT [FK_FactWorkflowAction_DimBatchStatus]
GO
ALTER TABLE [dw].[FactWorkflowAction]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowAction_DimDate] FOREIGN KEY([DateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactWorkflowAction] CHECK CONSTRAINT [FK_FactWorkflowAction_DimDate]
GO
ALTER TABLE [dw].[FactWorkflowAction]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowAction_DimUser] FOREIGN KEY([UserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactWorkflowAction] CHECK CONSTRAINT [FK_FactWorkflowAction_DimUser]
GO
ALTER TABLE [dw].[FactWorkflowAction]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowAction_DimWorkflow] FOREIGN KEY([WorkflowKey])
REFERENCES [dw].[DimWorkflow] ([WorkflowKey])
GO
ALTER TABLE [dw].[FactWorkflowAction] CHECK CONSTRAINT [FK_FactWorkflowAction_DimWorkflow]
GO
ALTER TABLE [dw].[FactWorkflowAction]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowAction_DimWorkflowStage] FOREIGN KEY([WorkflowStageKey])
REFERENCES [dw].[DimWorkflowStage] ([WorkflowStageKey])
GO
ALTER TABLE [dw].[FactWorkflowAction] CHECK CONSTRAINT [FK_FactWorkflowAction_DimWorkflowStage]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimApprovedUser] FOREIGN KEY([ApprovedByUserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimApprovedUser]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimAssignedUser] FOREIGN KEY([AssignedUserKey])
REFERENCES [dw].[DimUser] ([UserKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimAssignedUser]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimBatchStatus] FOREIGN KEY([BatchStatusKey])
REFERENCES [dw].[DimBatchStatus] ([BatchStatusKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimBatchStatus]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimDateEnd] FOREIGN KEY([EndDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimDateEnd]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimDateStart] FOREIGN KEY([StartDateKey])
REFERENCES [dw].[DimDate] ([DateKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimDateStart]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimWorkflow] FOREIGN KEY([WorkflowKey])
REFERENCES [dw].[DimWorkflow] ([WorkflowKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimWorkflow]
GO
ALTER TABLE [dw].[FactWorkflowStage]  WITH CHECK ADD  CONSTRAINT [FK_FactWorkflowStage_DimWorkflowStage] FOREIGN KEY([WorkflowStageKey])
REFERENCES [dw].[DimWorkflowStage] ([WorkflowStageKey])
GO
ALTER TABLE [dw].[FactWorkflowStage] CHECK CONSTRAINT [FK_FactWorkflowStage_DimWorkflowStage]
GO
ALTER TABLE [etl].[EtlRowAudit]  WITH CHECK ADD  CONSTRAINT [FK_EtlRowAudit_EtlRun] FOREIGN KEY([EtlRunId])
REFERENCES [etl].[EtlRun] ([EtlRunId])
GO
ALTER TABLE [etl].[EtlRowAudit] CHECK CONSTRAINT [FK_EtlRowAudit_EtlRun]
GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimAcademicTerm]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimAcademicTerm]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimAcademicTerm AS T
    USING (
        SELECT AcademicTermId, Name, CAST(1 AS BIT) AS IsActive, CAST(NULL AS DATETIME2) AS CreatedAt
        FROM [TesisDB_Extensible].dbo.AcademicTerms
    ) AS S
    ON T.AcademicTermId_OLTP = S.AcademicTermId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name, IsActive = S.IsActive, CreatedAtSource = S.CreatedAt
    WHEN NOT MATCHED THEN
        INSERT (AcademicTermId_OLTP, Name, IsActive, CreatedAtSource)
        VALUES (S.AcademicTermId, S.Name, S.IsActive, S.CreatedAt);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimArticle]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimArticle]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimArticle AS T
    USING (
        SELECT Id, Title, Doi, [Year], PublicationUrl, IsProjectResult, HasInterculturalComponent,
               ProceedingsName, Proceedings, EventName, GroupName, Filiacion, IsOpenAccess,
               ExternalSource, ExternalId, CreatedAt, CAST(NULL AS DATETIME2) AS UpdatedAt
        FROM [TesisDB_Extensible].dbo.Articles
    ) AS S
    ON T.ArticleId_OLTP = S.Id AND T.IsCurrent = 1
    WHEN MATCHED THEN
        UPDATE SET Title = S.Title, Doi = S.Doi, ArticleYear = S.[Year], PublicationUrl = S.PublicationUrl,
                   IsProjectResult = S.IsProjectResult, HasInterculturalComponent = S.HasInterculturalComponent,
                   ProceedingsName = S.ProceedingsName, Proceedings = S.Proceedings, EventName = S.EventName,
                   GroupName = S.GroupName, Filiacion = S.Filiacion, IsOpenAccess = S.IsOpenAccess,
                   ExternalSource = S.ExternalSource, ExternalId = S.ExternalId,
                   CreatedAtSource = S.CreatedAt, UpdatedAtSource = S.UpdatedAt
    WHEN NOT MATCHED THEN
        INSERT (
            ArticleId_OLTP, Title, Doi, ArticleYear, PublicationUrl, IsProjectResult, HasInterculturalComponent,
            ProceedingsName, Proceedings, EventName, GroupName, Filiacion, IsOpenAccess,
            ExternalSource, ExternalId, CreatedAtSource, UpdatedAtSource, ValidFrom, IsCurrent
        )
        VALUES (
            S.Id, S.Title, S.Doi, S.[Year], S.PublicationUrl, S.IsProjectResult, S.HasInterculturalComponent,
            S.ProceedingsName, S.Proceedings, S.EventName, S.GroupName, S.Filiacion, S.IsOpenAccess,
            S.ExternalSource, S.ExternalId, S.CreatedAt, S.UpdatedAt, SYSUTCDATETIME(), 1
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimAuthor]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimAuthor]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimAuthor AS T
    USING (
        SELECT Id, Identificacion, Nombre, Participacion, ParticipantType, InstitutionalPersonId,
               Email, Orcid, Affiliation, ExternalAuthorId, IsPrimaryAuthor, CreatedAt, UpdatedAt
        FROM [TesisDB_Extensible].dbo.ArticleParticipants
    ) AS S
    ON T.ArticleParticipantId_OLTP = S.Id AND T.IsCurrent = 1
    WHEN MATCHED THEN
        UPDATE SET Identificacion = S.Identificacion,
                   Nombre = S.Nombre,
                   Participacion = S.Participacion,
                   ParticipantType = S.ParticipantType,
                   InstitutionalPersonId = S.InstitutionalPersonId,
                   Email = S.Email,
                   Orcid = S.Orcid,
                   Affiliation = S.Affiliation,
                   ExternalAuthorId = S.ExternalAuthorId,
                   IsPrimaryAuthor = S.IsPrimaryAuthor,
                   CreatedAtSource = S.CreatedAt,
                   UpdatedAtSource = S.UpdatedAt
    WHEN NOT MATCHED THEN
        INSERT (
            ArticleParticipantId_OLTP, Identificacion, Nombre, Participacion, ParticipantType,
            InstitutionalPersonId, Email, Orcid, Affiliation, ExternalAuthorId, IsPrimaryAuthor,
            CreatedAtSource, UpdatedAtSource, ValidFrom, IsCurrent
        )
        VALUES (
            S.Id, S.Identificacion, S.Nombre, S.Participacion, S.ParticipantType,
            S.InstitutionalPersonId, S.Email, S.Orcid, S.Affiliation, S.ExternalAuthorId, S.IsPrimaryAuthor,
            S.CreatedAt, S.UpdatedAt, SYSUTCDATETIME(), 1
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimDynamicField]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimDynamicField]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimDynamicField AS T
    USING (
        SELECT FieldId, EntityName, FieldKey, FieldLabel, DataType, SourceType,
               PhysicalTableName, PhysicalColumnName, ReferenceTableName,
               IsSystemField, IsDynamic, IsRequired, IsVisible, IsEditable,
               IsFilterable, IsActive, DisplayOrder, MaxLength, Placeholder,
               HelpText, DefaultValue, ValidationRule
        FROM [TesisDB_Extensible].dbo.FieldCatalog
    ) AS S
    ON T.FieldId_OLTP = S.FieldId
    WHEN MATCHED THEN
        UPDATE SET EntityName = S.EntityName,
                   FieldKey = S.FieldKey,
                   FieldLabel = S.FieldLabel,
                   DataType = S.DataType,
                   SourceType = S.SourceType,
                   PhysicalTableName = S.PhysicalTableName,
                   PhysicalColumnName = S.PhysicalColumnName,
                   ReferenceTableName = S.ReferenceTableName,
                   IsSystemField = S.IsSystemField,
                   IsDynamic = S.IsDynamic,
                   IsRequired = S.IsRequired,
                   IsVisible = S.IsVisible,
                   IsEditable = S.IsEditable,
                   IsFilterable = S.IsFilterable,
                   IsActive = S.IsActive,
                   DisplayOrder = S.DisplayOrder,
                   MaxLength = S.MaxLength,
                   Placeholder = S.Placeholder,
                   HelpText = S.HelpText,
                   DefaultValue = S.DefaultValue,
                   ValidationRule = S.ValidationRule
    WHEN NOT MATCHED THEN
        INSERT (
            FieldId_OLTP, EntityName, FieldKey, FieldLabel, DataType, SourceType,
            PhysicalTableName, PhysicalColumnName, ReferenceTableName,
            IsSystemField, IsDynamic, IsRequired, IsVisible, IsEditable,
            IsFilterable, IsActive, DisplayOrder, MaxLength, Placeholder,
            HelpText, DefaultValue, ValidationRule
        )
        VALUES (
            S.FieldId, S.EntityName, S.FieldKey, S.FieldLabel, S.DataType, S.SourceType,
            S.PhysicalTableName, S.PhysicalColumnName, S.ReferenceTableName,
            S.IsSystemField, S.IsDynamic, S.IsRequired, S.IsVisible, S.IsEditable,
            S.IsFilterable, S.IsActive, S.DisplayOrder, S.MaxLength, S.Placeholder,
            S.HelpText, S.DefaultValue, S.ValidationRule
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimField]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimField]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimField AS T
    USING (
        SELECT
            bf.BroadFieldId,
            bf.Name AS BroadFieldName,
            sf.SpecificFieldId,
            sf.Code AS SpecificFieldCode,
            sf.Name AS SpecificFieldName,
            df.DetailedFieldId,
            df.Code AS DetailedFieldCode,
            df.Name AS DetailedFieldName,
            CAST(1 AS BIT) AS IsActive
        FROM [TesisDB_Extensible].dbo.DetailedFields df
        INNER JOIN [TesisDB_Extensible].dbo.SpecificFields sf
            ON sf.SpecificFieldId = df.SpecificFieldId
        INNER JOIN [TesisDB_Extensible].dbo.BroadFields bf
            ON bf.BroadFieldId = sf.BroadFieldId
    ) AS S
    ON ISNULL(T.BroadFieldId_OLTP,-1)=ISNULL(S.BroadFieldId,-1)
       AND ISNULL(T.SpecificFieldId_OLTP,-1)=ISNULL(S.SpecificFieldId,-1)
       AND ISNULL(T.DetailedFieldId_OLTP,-1)=ISNULL(S.DetailedFieldId,-1)
    WHEN MATCHED THEN
        UPDATE SET BroadFieldName = S.BroadFieldName,
                   SpecificFieldCode = S.SpecificFieldCode,
                   SpecificFieldName = S.SpecificFieldName,
                   DetailedFieldCode = S.DetailedFieldCode,
                   DetailedFieldName = S.DetailedFieldName,
                   IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (
            BroadFieldId_OLTP, BroadFieldName,
            SpecificFieldId_OLTP, SpecificFieldCode, SpecificFieldName,
            DetailedFieldId_OLTP, DetailedFieldCode, DetailedFieldName,
            IsActive
        )
        VALUES (
            S.BroadFieldId, S.BroadFieldName,
            S.SpecificFieldId, S.SpecificFieldCode, S.SpecificFieldName,
            S.DetailedFieldId, S.DetailedFieldCode, S.DetailedFieldName,
            S.IsActive
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimPublicationStatus]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimPublicationStatus]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimPublicationStatus AS T
    USING (
        SELECT PublicationStatusId, Name, CAST(1 AS BIT) AS IsActive
        FROM [TesisDB_Extensible].dbo.PublicationStatuses
    ) AS S
    ON T.PublicationStatusId_OLTP = S.PublicationStatusId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name, IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (PublicationStatusId_OLTP, Name, IsActive)
        VALUES (S.PublicationStatusId, S.Name, S.IsActive);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimIndexingSource]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimIndexingSource]
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'[TesisDB_Extensible].dbo.IndexingSources', N'U') IS NULL
        RETURN;

    MERGE dw.DimIndexingSource AS T
    USING (
        SELECT IndexingSourceId, Name, IsActive
        FROM [TesisDB_Extensible].dbo.IndexingSources
    ) AS S
    ON T.IndexingSourceId_OLTP = S.IndexingSourceId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name, IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (IndexingSourceId_OLTP, Name, IsActive)
        VALUES (S.IndexingSourceId, S.Name, S.IsActive);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimRegistrationMatrix]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimRegistrationMatrix]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimRegistrationMatrix AS T
    USING (
        SELECT RegistrationMatrixId, Name, EntityName, Status, Notes, CreatedByUserId, CreatedAt, UpdatedAt
        FROM [TesisDB_Extensible].dbo.RegistrationMatrix
    ) AS S
    ON T.RegistrationMatrixId_OLTP = S.RegistrationMatrixId
    WHEN MATCHED THEN
        UPDATE SET MatrixName = S.Name,
                   EntityName = S.EntityName,
                   MatrixStatus = S.Status,
                   Notes = S.Notes,
                   CreatedByUserId_OLTP = S.CreatedByUserId,
                   CreatedAtSource = S.CreatedAt,
                   UpdatedAtSource = S.UpdatedAt
    WHEN NOT MATCHED THEN
        INSERT (RegistrationMatrixId_OLTP, MatrixName, EntityName, MatrixStatus, Notes, CreatedByUserId_OLTP, CreatedAtSource, UpdatedAtSource)
        VALUES (S.RegistrationMatrixId, S.Name, S.EntityName, S.Status, S.Notes, S.CreatedByUserId, S.CreatedAt, S.UpdatedAt);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimResearchLine]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimResearchLine]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimResearchLine AS T
    USING (
        SELECT ResearchLineId, Name, CAST(1 AS BIT) AS IsActive, CAST(NULL AS DATETIME2) AS CreatedAt
        FROM [TesisDB_Extensible].dbo.ResearchLines
    ) AS S
    ON T.ResearchLineId_OLTP = S.ResearchLineId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name, IsActive = S.IsActive, CreatedAtSource = S.CreatedAt
    WHEN NOT MATCHED THEN
        INSERT (ResearchLineId_OLTP, Name, IsActive, CreatedAtSource)
        VALUES (S.ResearchLineId, S.Name, S.IsActive, S.CreatedAt);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimSystemRole]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimSystemRole]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimSystemRole AS T
    USING (
        SELECT Id, Name, NormalizedName
        FROM [TesisDB_Extensible].dbo.AspNetRoles
    ) AS S
    ON T.RoleId_OLTP = S.Id
    WHEN MATCHED THEN
        UPDATE SET RoleName = S.Name, NormalizedName = S.NormalizedName
    WHEN NOT MATCHED THEN
        INSERT (RoleId_OLTP, RoleName, NormalizedName)
        VALUES (S.Id, S.Name, S.NormalizedName);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimUser]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   DIMENSIONES
   ========================================================= */

CREATE   PROCEDURE [etl].[sp_Load_DimUser]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimUser AS T
    USING (
        SELECT Id, UserName, Email, FullName
        FROM [TesisDB_Extensible].dbo.AspNetUsers
    ) AS S
    ON T.UserId_OLTP = S.Id
    WHEN MATCHED THEN
        UPDATE SET UserName = S.UserName, Email = S.Email, FullName = S.FullName
    WHEN NOT MATCHED THEN
        INSERT (UserId_OLTP, UserName, Email, FullName)
        VALUES (S.Id, S.UserName, S.Email, S.FullName);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimRegistrationSource]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimRegistrationSource]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimRegistrationSource AS T
    USING (
        SELECT
            SourceCode,
            MAX(SourceName) AS SourceName,
            MAX(SourceCategory) AS SourceCategory
        FROM (
            SELECT N'MANUAL' AS SourceCode, N'Registro manual' AS SourceName, N'Manual' AS SourceCategory
            UNION ALL SELECT N'API', N'API externa', N'Automatizado'
            UNION ALL SELECT N'AUTHOR_SINGLE', N'Registro individual de autor', N'Autor'
            UNION ALL SELECT N'AUTHOR_MATRIX', N'Matriz de autor', N'Autor'
            UNION ALL SELECT N'ADMIN_BULK', N'Carga masiva administrativa', N'Administrador'
            UNION ALL SELECT N'MASS_IMPORT', N'Carga masiva', N'Masivo'
            UNION ALL SELECT N'BULK_IMPORT', N'Carga masiva', N'Masivo'
            UNION ALL SELECT N'EXTERNAL_API', N'API externa', N'Automatizado'
            UNION ALL SELECT N'MIGRATION', N'Migracion historica', N'Migracion'
            UNION ALL
            SELECT DISTINCT
                NULLIF(LTRIM(RTRIM(SourceType)), N'') AS SourceCode,
                NULLIF(LTRIM(RTRIM(SourceType)), N'') AS SourceName,
                N'OLTP' AS SourceCategory
            FROM [TesisDB_Extensible].dbo.ImportBatch
            WHERE NULLIF(LTRIM(RTRIM(SourceType)), N'') IS NOT NULL
        ) AS RawSources
        GROUP BY SourceCode
    ) AS S
    ON T.SourceCode = S.SourceCode
    WHEN MATCHED THEN
        UPDATE SET SourceName = S.SourceName, SourceCategory = S.SourceCategory
    WHEN NOT MATCHED THEN
        INSERT (SourceCode, SourceName, SourceCategory)
        VALUES (S.SourceCode, S.SourceName, S.SourceCategory);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimBatchStatus]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimBatchStatus]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimBatchStatus AS T
    USING (
        SELECT StatusCode, MAX(StatusName) AS StatusName
        FROM (
            SELECT N'Draft' AS StatusCode, N'Borrador' AS StatusName
            UNION ALL SELECT N'Pending', N'Pendiente'
            UNION ALL SELECT N'InReview', N'En revision'
            UNION ALL SELECT N'Approved', N'Aprobado'
            UNION ALL SELECT N'Returned', N'Devuelto'
            UNION ALL SELECT N'Processed', N'Procesado'
            UNION ALL SELECT N'Completed', N'Completado'
            UNION ALL SELECT N'Failed', N'Fallido'
            UNION ALL SELECT N'Valid', N'Valido'
            UNION ALL SELECT N'Error', N'Con error'
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.ImportBatch
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(RowStatus)), N''), NULLIF(LTRIM(RTRIM(RowStatus)), N'')
            FROM [TesisDB_Extensible].dbo.ImportBatchRow
            WHERE NULLIF(LTRIM(RTRIM(RowStatus)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.RegistrationMatrix
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.WorkflowInstance
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
            UNION ALL
            SELECT DISTINCT NULLIF(LTRIM(RTRIM(Status)), N''), NULLIF(LTRIM(RTRIM(Status)), N'')
            FROM [TesisDB_Extensible].dbo.WorkflowStageInstance
            WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
        ) AS RawStatuses
        WHERE StatusCode IS NOT NULL
        GROUP BY StatusCode
    ) AS S
    ON T.StatusCode = S.StatusCode
    WHEN MATCHED THEN
        UPDATE SET StatusName = S.StatusName
    WHEN NOT MATCHED THEN
        INSERT (StatusCode, StatusName)
        VALUES (S.StatusCode, S.StatusName);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimValidationError]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimValidationError]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimValidationError AS T
    USING (
        SELECT DISTINCT ErrorCode, Severity, MIN(ErrorMessage) AS ErrorMessageTemplate
        FROM [TesisDB_Extensible].dbo.ImportBatchError
        GROUP BY ErrorCode, Severity
    ) AS S
    ON T.ErrorCode = S.ErrorCode AND T.Severity = S.Severity
    WHEN MATCHED THEN
        UPDATE SET ErrorMessageTemplate = S.ErrorMessageTemplate
    WHEN NOT MATCHED THEN
        INSERT (ErrorCode, Severity, ErrorMessageTemplate)
        VALUES (S.ErrorCode, S.Severity, S.ErrorMessageTemplate);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimVenue]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimVenue]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimVenue AS T
    USING (
        SELECT VenueId, Name, IssnCode, IssueNumber, VolumeNumber, JournalUrl, Type, CAST(1 AS BIT) AS IsActive, CAST(NULL AS DATETIME2) AS CreatedAt
        FROM [TesisDB_Extensible].dbo.Venues
    ) AS S
    ON T.VenueId_OLTP = S.VenueId
    WHEN MATCHED THEN
        UPDATE SET Name = S.Name,
                   IssnCode = S.IssnCode,
                   IssueNumber = S.IssueNumber,
                   VolumeNumber = S.VolumeNumber,
                   JournalUrl = S.JournalUrl,
                   VenueType = S.Type,
                   IsActive = S.IsActive,
                   CreatedAtSource = S.CreatedAt
    WHEN NOT MATCHED THEN
        INSERT (VenueId_OLTP, Name, IssnCode, IssueNumber, VolumeNumber, JournalUrl, VenueType, IsActive, CreatedAtSource)
        VALUES (S.VenueId, S.Name, S.IssnCode, S.IssueNumber, S.VolumeNumber, S.JournalUrl, S.Type, S.IsActive, S.CreatedAt);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimWorkflow]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimWorkflow]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimWorkflow AS T
    USING (
        SELECT WorkflowDefinitionId, [Key], Name, EntityName, [Description], IsActive
        FROM [TesisDB_Extensible].dbo.WorkflowDefinition
    ) AS S
    ON T.WorkflowDefinitionId_OLTP = S.WorkflowDefinitionId
    WHEN MATCHED THEN
        UPDATE SET WorkflowCode = S.[Key], WorkflowName = S.Name, EntityName = S.EntityName, [Description] = S.[Description], IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (WorkflowDefinitionId_OLTP, WorkflowCode, WorkflowName, EntityName, [Description], IsActive)
        VALUES (S.WorkflowDefinitionId, S.[Key], S.Name, S.EntityName, S.[Description], S.IsActive);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_DimWorkflowStage]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_DimWorkflowStage]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.DimWorkflowStage AS T
    USING (
        SELECT WorkflowStageDefinitionId, WorkflowDefinitionId, StageKey, StageName, DisplayOrder,
               StageGroupKey, StageGroupName, ResponsibleRoleId,
               CanEditData, CanReturn, CanApprove, CanProcessBatch, IsFinalStage, IsActive
        FROM [TesisDB_Extensible].dbo.WorkflowStageDefinition
    ) AS S
    ON T.WorkflowStageDefinitionId_OLTP = S.WorkflowStageDefinitionId
    WHEN MATCHED THEN
        UPDATE SET WorkflowDefinitionId_OLTP = S.WorkflowDefinitionId,
                   StageKey = S.StageKey,
                   StageName = S.StageName,
                   DisplayOrder = S.DisplayOrder,
                   StageGroupKey = S.StageGroupKey,
                   StageGroupName = S.StageGroupName,
                   ResponsibleRoleId_OLTP = S.ResponsibleRoleId,
                   CanEditData = S.CanEditData,
                   CanReturn = S.CanReturn,
                   CanApprove = S.CanApprove,
                   CanProcessBatch = S.CanProcessBatch,
                   IsFinalStage = S.IsFinalStage,
                   IsActive = S.IsActive
    WHEN NOT MATCHED THEN
        INSERT (
            WorkflowStageDefinitionId_OLTP, WorkflowDefinitionId_OLTP, StageKey, StageName, DisplayOrder,
            StageGroupKey, StageGroupName, ResponsibleRoleId_OLTP,
            CanEditData, CanReturn, CanApprove, CanProcessBatch, IsFinalStage, IsActive
        )
        VALUES (
            S.WorkflowStageDefinitionId, S.WorkflowDefinitionId, S.StageKey, S.StageName, S.DisplayOrder,
            S.StageGroupKey, S.StageGroupName, S.ResponsibleRoleId,
            S.CanEditData, S.CanReturn, S.CanApprove, S.CanProcessBatch, S.IsFinalStage, S.IsActive
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactArticleAuthor]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactArticleAuthor]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactArticleAuthor;

    INSERT INTO dw.FactArticleAuthor (
        ArticleKey, AuthorKey, DateKey, ResearchLineKey, FieldHierarchyKey,
        RegistrationSourceKey, AuthorCount, IsPrimaryAuthorFlag, AuthorOrder
    )
    SELECT
        da.ArticleKey,
        dau.AuthorKey,
        etl.fn_DateKey(ap.CreatedAt),
        drl.ResearchLineKey,
        df.FieldHierarchyKey,
        drs.RegistrationSourceKey,
        1,
        ap.IsPrimaryAuthor,
        ap.[Index]
    FROM [TesisDB_Extensible].dbo.ArticleParticipants ap
    INNER JOIN [TesisDB_Extensible].dbo.Articles a ON a.Id = ap.ArticleId
    INNER JOIN dw.DimArticle da ON da.ArticleId_OLTP = a.Id AND da.IsCurrent = 1
    INNER JOIN dw.DimAuthor dau ON dau.ArticleParticipantId_OLTP = ap.Id AND dau.IsCurrent = 1
    LEFT JOIN dw.DimResearchLine drl ON drl.ResearchLineId_OLTP = a.ResearchLineId
    LEFT JOIN dw.DimField df
        ON ISNULL(df.BroadFieldId_OLTP,-1)=ISNULL(a.BroadFieldId,-1)
       AND ISNULL(df.SpecificFieldId_OLTP,-1)=ISNULL(a.SpecificFieldId,-1)
       AND ISNULL(df.DetailedFieldId_OLTP,-1)=ISNULL(a.DetailedFieldId,-1)
    OUTER APPLY (
        SELECT TOP 1 ib.SourceType
        FROM [TesisDB_Extensible].dbo.ImportBatchRow ibr
        INNER JOIN [TesisDB_Extensible].dbo.ImportBatch ib
            ON ib.ImportBatchId = ibr.ImportBatchId
        WHERE ibr.TargetArticleId = a.Id
        ORDER BY COALESCE(ibr.UpdatedAt, ibr.CreatedAt) DESC
    ) articleSource
    LEFT JOIN dw.DimRegistrationSource drs
        ON drs.SourceCode =
            CASE
                WHEN articleSource.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                WHEN articleSource.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                WHEN articleSource.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                WHEN articleSource.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                WHEN articleSource.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                WHEN a.ExternalSource IS NOT NULL THEN N'EXTERNAL_API'
                ELSE N'MANUAL'
            END;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactArticleIndexing]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactArticleIndexing]
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'[TesisDB_Extensible].dbo.ArticleIndexings', N'U') IS NULL
        RETURN;

    TRUNCATE TABLE dw.FactArticleIndexing;

    INSERT INTO dw.FactArticleIndexing (
        ArticleKey, IndexingSourceKey, DateKey, IndexingCount
    )
    SELECT
        da.ArticleKey,
        dis.IndexingSourceKey,
        etl.fn_DateKey(a.CreatedAt),
        1
    FROM [TesisDB_Extensible].dbo.ArticleIndexings ai
    INNER JOIN [TesisDB_Extensible].dbo.Articles a
        ON a.Id = ai.ArticleId
    INNER JOIN dw.DimArticle da
        ON da.ArticleId_OLTP = ai.ArticleId AND da.IsCurrent = 1
    INNER JOIN dw.DimIndexingSource dis
        ON dis.IndexingSourceId_OLTP = ai.IndexingSourceId;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactArticleDynamicAttribute]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactArticleDynamicAttribute]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactArticleDynamicAttribute;

    INSERT INTO dw.FactArticleDynamicAttribute (
        ArticleKey, DynamicFieldKey, DateKey,
        ValueString, ValueInt, ValueDecimal, ValueDate, ValueBit, ValueJson, AttributeCount
    )
    SELECT
        da.ArticleKey,
        ddf.DynamicFieldKey,
        etl.fn_DateKey(dfv.CreatedAt),
        dfv.ValueString,
        dfv.ValueInt,
        dfv.ValueDecimal,
        dfv.ValueDate,
        dfv.ValueBit,
        dfv.ValueJson,
        1
    FROM [TesisDB_Extensible].dbo.DynamicFieldValues dfv
    INNER JOIN dw.DimArticle da
        ON da.ArticleId_OLTP = dfv.ArticleId AND da.IsCurrent = 1
    INNER JOIN dw.DimDynamicField ddf
        ON ddf.FieldId_OLTP = dfv.FieldId;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactArticlePublication]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   HECHOS
   ========================================================= */

CREATE   PROCEDURE [etl].[sp_Load_FactArticlePublication]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactArticlePublication;

    INSERT INTO dw.FactArticlePublication (
        ArticleKey, CreatedDateKey, PublishedDateKey, VenueKey, AcademicTermKey,
        PublicationStatusKey, ResearchLineKey, FieldHierarchyKey, RegistrationSourceKey,
        ArticleCount, PageCount, IsOpenAccessFlag, IsProjectResultFlag, HasInterculturalFlag
    )
    SELECT
        da.ArticleKey,
        etl.fn_DateKey(a.CreatedAt),
        etl.fn_DateKey(a.PublishedAt),
        dv.VenueKey,
        dat.AcademicTermKey,
        dps.PublicationStatusKey,
        drl.ResearchLineKey,
        df.FieldHierarchyKey,
        drs.RegistrationSourceKey,
        1,
        a.PageCount,
        a.IsOpenAccess,
        a.IsProjectResult,
        a.HasInterculturalComponent
    FROM [TesisDB_Extensible].dbo.Articles a
    INNER JOIN dw.DimArticle da ON da.ArticleId_OLTP = a.Id AND da.IsCurrent = 1
    LEFT JOIN dw.DimVenue dv ON dv.VenueId_OLTP = a.VenueId
    LEFT JOIN dw.DimAcademicTerm dat ON dat.AcademicTermId_OLTP = a.AcademicTermId
    LEFT JOIN dw.DimPublicationStatus dps ON dps.PublicationStatusId_OLTP = a.PublicationStatusId
    LEFT JOIN dw.DimResearchLine drl ON drl.ResearchLineId_OLTP = a.ResearchLineId
    LEFT JOIN dw.DimField df
        ON ISNULL(df.BroadFieldId_OLTP,-1)=ISNULL(a.BroadFieldId,-1)
       AND ISNULL(df.SpecificFieldId_OLTP,-1)=ISNULL(a.SpecificFieldId,-1)
       AND ISNULL(df.DetailedFieldId_OLTP,-1)=ISNULL(a.DetailedFieldId,-1)
    OUTER APPLY (
        SELECT TOP 1 ib.SourceType
        FROM [TesisDB_Extensible].dbo.ImportBatchRow ibr
        INNER JOIN [TesisDB_Extensible].dbo.ImportBatch ib
            ON ib.ImportBatchId = ibr.ImportBatchId
        WHERE ibr.TargetArticleId = a.Id
        ORDER BY COALESCE(ibr.UpdatedAt, ibr.CreatedAt) DESC
    ) articleSource
    LEFT JOIN dw.DimRegistrationSource drs
        ON drs.SourceCode =
            CASE
                WHEN articleSource.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                WHEN articleSource.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                WHEN articleSource.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                WHEN articleSource.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                WHEN articleSource.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                WHEN a.ExternalSource IS NOT NULL THEN N'EXTERNAL_API'
                ELSE N'MANUAL'
            END;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactParticipantDynamicAttribute]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactParticipantDynamicAttribute]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactParticipantDynamicAttribute;

    INSERT INTO dw.FactParticipantDynamicAttribute (
        AuthorKey, DynamicFieldKey, DateKey,
        ValueString, ValueInt, ValueDecimal, ValueDate, ValueBit, ValueJson, AttributeCount
    )
    SELECT
        da.AuthorKey,
        ddf.DynamicFieldKey,
        etl.fn_DateKey(apdfv.CreatedAt),
        apdfv.ValueString,
        apdfv.ValueInt,
        apdfv.ValueDecimal,
        apdfv.ValueDate,
        apdfv.ValueBit,
        apdfv.ValueJson,
        1
    FROM [TesisDB_Extensible].dbo.ArticleParticipantDynamicFieldValues apdfv
    INNER JOIN dw.DimAuthor da
        ON da.ArticleParticipantId_OLTP = apdfv.ArticleParticipantId AND da.IsCurrent = 1
    INNER JOIN dw.DimDynamicField ddf
        ON ddf.FieldId_OLTP = apdfv.FieldId;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactRegistrationBatch]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactRegistrationBatch]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactRegistrationBatch AS T
    USING (
        SELECT
            ib.ImportBatchId,
            etl.fn_DateKey(ib.StartedAt) AS StartDateKey,
            etl.fn_DateKey(ib.FinishedAt) AS FinishDateKey,
            du.UserKey,
            drs.RegistrationSourceKey,
            dbs.BatchStatusKey,
            ib.TotalRows,
            ib.SuccessfulRows,
            ib.ErrorRows,
            DATEDIFF(SECOND, ib.StartedAt, ib.FinishedAt) AS DurationSeconds
        FROM [TesisDB_Extensible].dbo.ImportBatch ib
        LEFT JOIN dw.DimUser du ON du.UserId_OLTP = ib.CreatedByUserId
        LEFT JOIN dw.DimRegistrationSource drs
            ON drs.SourceCode =
                CASE
                    WHEN ib.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                    WHEN ib.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                    WHEN ib.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                    WHEN ib.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                    WHEN ib.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                    ELSE N'MANUAL'
                END
        LEFT JOIN dw.DimBatchStatus dbs ON dbs.StatusCode = ib.Status
    ) AS S
    ON T.BatchId_OLTP = S.ImportBatchId
    WHEN MATCHED THEN
        UPDATE SET StartDateKey = S.StartDateKey,
                   FinishDateKey = S.FinishDateKey,
                   UserKey = S.UserKey,
                   RegistrationSourceKey = S.RegistrationSourceKey,
                   BatchStatusKey = S.BatchStatusKey,
                   TotalRows = S.TotalRows,
                   SuccessfulRows = S.SuccessfulRows,
                   ErrorRows = S.ErrorRows,
                   DurationSeconds = S.DurationSeconds
    WHEN NOT MATCHED THEN
        INSERT (
            BatchId_OLTP, StartDateKey, FinishDateKey, UserKey,
            RegistrationSourceKey, BatchStatusKey, BatchCount,
            TotalRows, SuccessfulRows, ErrorRows, DurationSeconds
        )
        VALUES (
            S.ImportBatchId, S.StartDateKey, S.FinishDateKey, S.UserKey,
            S.RegistrationSourceKey, S.BatchStatusKey, 1,
            S.TotalRows, S.SuccessfulRows, S.ErrorRows, S.DurationSeconds
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactRegistrationMatrix]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactRegistrationMatrix]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactRegistrationMatrix;

    INSERT INTO dw.FactRegistrationMatrix (
        RegistrationMatrixKey, DateKey, UserKey, BatchStatusKey, MatrixCount, LinkedBatchCount
    )
    SELECT
        drm.RegistrationMatrixKey,
        etl.fn_DateKey(rm.CreatedAt),
        du.UserKey,
        dbs.BatchStatusKey,
        1,
        CASE WHEN rm.LastImportBatchId IS NULL THEN 0 ELSE 1 END
    FROM [TesisDB_Extensible].dbo.RegistrationMatrix rm
    INNER JOIN dw.DimRegistrationMatrix drm
        ON drm.RegistrationMatrixId_OLTP = rm.RegistrationMatrixId
    LEFT JOIN dw.DimUser du
        ON du.UserId_OLTP = rm.CreatedByUserId
    LEFT JOIN dw.DimBatchStatus dbs
        ON dbs.StatusCode = rm.Status;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactRegistrationRow]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactRegistrationRow]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactRegistrationRow AS T
    USING (
        SELECT
            r.ImportBatchRowId,
            r.ImportBatchId,
            etl.fn_DateKey(r.CreatedAt) AS DateKey,
            drs.RegistrationSourceKey,
            dbs.BatchStatusKey,
            drm.RegistrationMatrixKey,
            CAST(CASE WHEN r.RowStatus = N'Valid' THEN 1 ELSE 0 END AS BIT) AS IsValidFlag,
            CAST(CASE WHEN r.RowStatus = N'Processed' THEN 1 ELSE 0 END AS BIT) AS IsProcessedFlag,
            CAST(CASE WHEN r.RowStatus = N'Error' THEN 1 ELSE 0 END AS BIT) AS IsErrorFlag
        FROM [TesisDB_Extensible].dbo.ImportBatchRow r
        INNER JOIN [TesisDB_Extensible].dbo.ImportBatch b ON b.ImportBatchId = r.ImportBatchId
        LEFT JOIN dw.DimRegistrationSource drs
            ON drs.SourceCode =
                CASE
                    WHEN b.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                    WHEN b.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                    WHEN b.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                    WHEN b.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                    WHEN b.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                    ELSE N'MANUAL'
                END
        LEFT JOIN dw.DimBatchStatus dbs ON dbs.StatusCode = r.RowStatus
        LEFT JOIN [TesisDB_Extensible].dbo.RegistrationMatrix m ON m.LastImportBatchId = b.ImportBatchId
        LEFT JOIN dw.DimRegistrationMatrix drm ON drm.RegistrationMatrixId_OLTP = m.RegistrationMatrixId
    ) AS S
    ON T.BatchRowId_OLTP = S.ImportBatchRowId
    WHEN MATCHED THEN
        UPDATE SET BatchId_OLTP = S.ImportBatchId,
                   DateKey = S.DateKey,
                   RegistrationSourceKey = S.RegistrationSourceKey,
                   BatchStatusKey = S.BatchStatusKey,
                   RegistrationMatrixKey = S.RegistrationMatrixKey,
                   IsValidFlag = S.IsValidFlag,
                   IsProcessedFlag = S.IsProcessedFlag,
                   IsErrorFlag = S.IsErrorFlag
    WHEN NOT MATCHED THEN
        INSERT (
            BatchId_OLTP, BatchRowId_OLTP, DateKey, RegistrationSourceKey, BatchStatusKey,
            RegistrationMatrixKey, RowQty, IsValidFlag, IsProcessedFlag, IsErrorFlag
        )
        VALUES (
            S.ImportBatchId, S.ImportBatchRowId, S.DateKey, S.RegistrationSourceKey, S.BatchStatusKey,
            S.RegistrationMatrixKey, 1, S.IsValidFlag, S.IsProcessedFlag, S.IsErrorFlag
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactValidationError]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactValidationError]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactValidationError;

    INSERT INTO dw.FactValidationError (
        BatchId_OLTP, BatchRowId_OLTP, DateKey, ValidationErrorKey,
        DynamicFieldKey, RegistrationSourceKey, UserKey, ErrorCount
    )
    SELECT
        e.ImportBatchId,
        e.ImportBatchRowId,
        etl.fn_DateKey(e.CreatedAt),
        dve.ValidationErrorKey,
        ddf.DynamicFieldKey,
        drs.RegistrationSourceKey,
        du.UserKey,
        1
    FROM [TesisDB_Extensible].dbo.ImportBatchError e
    INNER JOIN dw.DimValidationError dve
        ON dve.ErrorCode = e.ErrorCode AND dve.Severity = e.Severity
    LEFT JOIN dw.DimDynamicField ddf
        ON ddf.FieldId_OLTP = e.FieldId
    LEFT JOIN [TesisDB_Extensible].dbo.ImportBatch b
        ON b.ImportBatchId = e.ImportBatchId
    LEFT JOIN dw.DimRegistrationSource drs
        ON drs.SourceCode =
            CASE
                    WHEN b.SourceType IN (N'API', N'EXTERNAL_API') THEN N'EXTERNAL_API'
                    WHEN b.SourceType IN (N'AUTHOR_SINGLE', N'AUTHOR_INDIVIDUAL') THEN N'AUTHOR_SINGLE'
                    WHEN b.SourceType IN (N'AUTHOR_MATRIX', N'AUTHOR_BULK') THEN N'AUTHOR_MATRIX'
                    WHEN b.SourceType IN (N'ADMIN_BULK') THEN N'ADMIN_BULK'
                    WHEN b.SourceType IN (N'Excel', N'CSV', N'Bulk', N'BULK', N'MASS_IMPORT', N'BULK_IMPORT') THEN N'MASS_IMPORT'
                    ELSE N'MANUAL'
                END
    LEFT JOIN dw.DimUser du
        ON du.UserId_OLTP = b.CreatedByUserId;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactVenueMetricYear]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactVenueMetricYear]
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dw.FactVenueMetricYear;

    INSERT INTO dw.FactVenueMetricYear (
        VenueKey, DateKey, MetricCount, SJR, CiteScore, HIndex, Quartile
    )
    SELECT
        dv.VenueKey,
        CAST(CONCAT(vm.[Year], '0101') AS INT),
        1,
        vm.SJR,
        CAST(NULL AS DECIMAL(10,4)),
        CAST(NULL AS INT),
        vm.Quartile
    FROM [TesisDB_Extensible].dbo.VenueMetrics vm
    INNER JOIN dw.DimVenue dv ON dv.VenueId_OLTP = vm.VenueId;
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactWorkflowAction]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactWorkflowAction]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactWorkflowAction AS T
    USING (
        SELECT
            wal.WorkflowActionLogId,
            wal.WorkflowInstanceId,
            wal.WorkflowStageInstanceId,
            wi.ImportBatchId,
            dwf.WorkflowKey,
            dws.WorkflowStageKey,
            etl.fn_DateKey(wal.PerformedAt) AS DateKey,
            du.UserKey,
            dbs.BatchStatusKey,
            wal.ActionType,
            wal.FromStatus,
            wal.ToStatus
        FROM [TesisDB_Extensible].dbo.WorkflowActionLog wal
        INNER JOIN [TesisDB_Extensible].dbo.WorkflowInstance wi
            ON wi.WorkflowInstanceId = wal.WorkflowInstanceId
        INNER JOIN dw.DimWorkflow dwf
            ON dwf.WorkflowDefinitionId_OLTP = wi.WorkflowDefinitionId
        LEFT JOIN [TesisDB_Extensible].dbo.WorkflowStageInstance wsi
            ON wsi.WorkflowStageInstanceId = wal.WorkflowStageInstanceId
        LEFT JOIN dw.DimWorkflowStage dws
            ON dws.WorkflowStageDefinitionId_OLTP = wsi.WorkflowStageDefinitionId
        LEFT JOIN dw.DimUser du
            ON du.UserId_OLTP = wal.PerformedByUserId
        LEFT JOIN dw.DimBatchStatus dbs
            ON dbs.StatusCode = wal.ToStatus
    ) AS S
    ON T.WorkflowActionLogId_OLTP = S.WorkflowActionLogId
    WHEN MATCHED THEN
        UPDATE SET WorkflowInstanceId_OLTP = S.WorkflowInstanceId,
                   WorkflowStageInstanceId_OLTP = S.WorkflowStageInstanceId,
                   BatchId_OLTP = S.ImportBatchId,
                   WorkflowKey = S.WorkflowKey,
                   WorkflowStageKey = S.WorkflowStageKey,
                   DateKey = S.DateKey,
                   UserKey = S.UserKey,
                   BatchStatusKey = S.BatchStatusKey,
                   ActionType = S.ActionType,
                   FromStatus = S.FromStatus,
                   ToStatus = S.ToStatus
    WHEN NOT MATCHED THEN
        INSERT (
            WorkflowActionLogId_OLTP, WorkflowInstanceId_OLTP, WorkflowStageInstanceId_OLTP,
            BatchId_OLTP, WorkflowKey, WorkflowStageKey, DateKey, UserKey, BatchStatusKey,
            ActionType, FromStatus, ToStatus, ActionCount
        )
        VALUES (
            S.WorkflowActionLogId, S.WorkflowInstanceId, S.WorkflowStageInstanceId,
            S.ImportBatchId, S.WorkflowKey, S.WorkflowStageKey, S.DateKey, S.UserKey, S.BatchStatusKey,
            S.ActionType, S.FromStatus, S.ToStatus, 1
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_Load_FactWorkflowStage]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [etl].[sp_Load_FactWorkflowStage]
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dw.FactWorkflowStage AS T
    USING (
        SELECT
            wsi.WorkflowStageInstanceId,
            wi.WorkflowInstanceId,
            wi.ImportBatchId,
            dwf.WorkflowKey,
            dws.WorkflowStageKey,
            etl.fn_DateKey(wsi.StartedAt) AS StartDateKey,
            etl.fn_DateKey(wsi.CompletedAt) AS EndDateKey,
            du1.UserKey AS AssignedUserKey,
            du2.UserKey AS ApprovedByUserKey,
            dbs.BatchStatusKey,
            DATEDIFF(SECOND, wsi.StartedAt, wsi.CompletedAt) AS StageDurationSeconds,
            CAST(CASE WHEN wsi.ApprovedByUserId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ApprovedFlag,
            CAST(CASE WHEN wsi.ReturnedAt IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ReturnedFlag
        FROM [TesisDB_Extensible].dbo.WorkflowStageInstance wsi
        INNER JOIN [TesisDB_Extensible].dbo.WorkflowInstance wi
            ON wi.WorkflowInstanceId = wsi.WorkflowInstanceId
        INNER JOIN dw.DimWorkflow dwf
            ON dwf.WorkflowDefinitionId_OLTP = wi.WorkflowDefinitionId
        INNER JOIN dw.DimWorkflowStage dws
            ON dws.WorkflowStageDefinitionId_OLTP = wsi.WorkflowStageDefinitionId
        LEFT JOIN dw.DimUser du1
            ON du1.UserId_OLTP = wsi.AssignedToUserId
        LEFT JOIN dw.DimUser du2
            ON du2.UserId_OLTP = wsi.ApprovedByUserId
        LEFT JOIN dw.DimBatchStatus dbs
            ON dbs.StatusCode = wsi.Status
    ) AS S
    ON T.WorkflowStageInstanceId_OLTP = S.WorkflowStageInstanceId
    WHEN MATCHED THEN
        UPDATE SET WorkflowInstanceId_OLTP = S.WorkflowInstanceId,
                   BatchId_OLTP = S.ImportBatchId,
                   WorkflowKey = S.WorkflowKey,
                   WorkflowStageKey = S.WorkflowStageKey,
                   StartDateKey = S.StartDateKey,
                   EndDateKey = S.EndDateKey,
                   AssignedUserKey = S.AssignedUserKey,
                   ApprovedByUserKey = S.ApprovedByUserKey,
                   BatchStatusKey = S.BatchStatusKey,
                   StageDurationSeconds = S.StageDurationSeconds,
                   ApprovedFlag = S.ApprovedFlag,
                   ReturnedFlag = S.ReturnedFlag
    WHEN NOT MATCHED THEN
        INSERT (
            WorkflowInstanceId_OLTP, WorkflowStageInstanceId_OLTP, BatchId_OLTP, WorkflowKey, WorkflowStageKey,
            StartDateKey, EndDateKey, AssignedUserKey, ApprovedByUserKey, BatchStatusKey,
            StageCount, StageDurationSeconds, ApprovedFlag, ReturnedFlag
        )
        VALUES (
            S.WorkflowInstanceId, S.WorkflowStageInstanceId, S.ImportBatchId, S.WorkflowKey, S.WorkflowStageKey,
            S.StartDateKey, S.EndDateKey, S.AssignedUserKey, S.ApprovedByUserKey, S.BatchStatusKey,
            1, S.StageDurationSeconds, S.ApprovedFlag, S.ReturnedFlag
        );
END;

GO
/****** Object:  StoredProcedure [etl].[sp_PopulateDimDate]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   PROCEDIMIENTO CALENDARIO
   ========================================================= */

CREATE   PROCEDURE [etl].[sp_PopulateDimDate]
    @StartDate DATE = '20000101',
    @EndDate   DATE = '20501231'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH d AS (
        SELECT @StartDate AS FullDate
        UNION ALL
        SELECT DATEADD(DAY, 1, FullDate)
        FROM d
        WHERE FullDate < @EndDate
    )
    INSERT INTO dw.DimDate (
        DateKey, FullDate, DayNumber, DayName, WeekNumber, MonthNumber,
        MonthName, QuarterNumber, SemesterNumber, YearNumber, IsWeekend
    )
    SELECT
        CAST(CONVERT(CHAR(8), d.FullDate, 112) AS INT),
        d.FullDate,
        DATEPART(DAY, d.FullDate),
        DATENAME(WEEKDAY, d.FullDate),
        DATEPART(ISO_WEEK, d.FullDate),
        DATEPART(MONTH, d.FullDate),
        DATENAME(MONTH, d.FullDate),
        DATEPART(QUARTER, d.FullDate),
        CASE WHEN DATEPART(MONTH, d.FullDate) <= 6 THEN 1 ELSE 2 END,
        DATEPART(YEAR, d.FullDate),
        CASE WHEN DATEPART(WEEKDAY, d.FullDate) IN (1,7) THEN 1 ELSE 0 END
    FROM d
    WHERE NOT EXISTS (
        SELECT 1
        FROM dw.DimDate x
        WHERE x.FullDate = d.FullDate
    )
    OPTION (MAXRECURSION 0);
END;

GO
/****** Object:  StoredProcedure [etl].[sp_RunFullLoad]    Script Date: 13/04/2026 03:44:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =========================================================
   PROCESO MAESTRO
   ========================================================= */

CREATE   PROCEDURE [etl].[sp_RunFullLoad]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RunId BIGINT;

    INSERT INTO etl.EtlRun (ProcessName, StartedAt, Status, Notes)
    VALUES (N'FULL_DW_LOAD', SYSUTCDATETIME(), N'Running', N'Carga completa del DW');
    SET @RunId = SCOPE_IDENTITY();

    BEGIN TRY
        EXEC etl.sp_PopulateDimDate;

        EXEC etl.sp_Load_DimUser;
        EXEC etl.sp_Load_DimSystemRole;
        EXEC etl.sp_Load_DimRegistrationSource;
        EXEC etl.sp_Load_DimBatchStatus;
        EXEC etl.sp_Load_DimVenue;
        EXEC etl.sp_Load_DimAcademicTerm;
        EXEC etl.sp_Load_DimPublicationStatus;
        EXEC etl.sp_Load_DimIndexingSource;
        EXEC etl.sp_Load_DimResearchLine;
        EXEC etl.sp_Load_DimField;
        EXEC etl.sp_Load_DimWorkflow;
        EXEC etl.sp_Load_DimWorkflowStage;
        EXEC etl.sp_Load_DimDynamicField;
        EXEC etl.sp_Load_DimRegistrationMatrix;
        EXEC etl.sp_Load_DimValidationError;
        EXEC etl.sp_Load_DimArticle;
        EXEC etl.sp_Load_DimAuthor;

        EXEC etl.sp_Load_FactArticlePublication;
        EXEC etl.sp_Load_FactArticleAuthor;
        EXEC etl.sp_Load_FactArticleIndexing;
        EXEC etl.sp_Load_FactVenueMetricYear;
        EXEC etl.sp_Load_FactRegistrationBatch;
        EXEC etl.sp_Load_FactRegistrationRow;
        EXEC etl.sp_Load_FactValidationError;
        EXEC etl.sp_Load_FactWorkflowStage;
        EXEC etl.sp_Load_FactWorkflowAction;
        EXEC etl.sp_Load_FactRegistrationMatrix;
        EXEC etl.sp_Load_FactArticleDynamicAttribute;
        EXEC etl.sp_Load_FactParticipantDynamicAttribute;

        UPDATE etl.EtlRun
        SET FinishedAt = SYSUTCDATETIME(),
            Status = N'Success'
        WHERE EtlRunId = @RunId;
    END TRY
    BEGIN CATCH
        UPDATE etl.EtlRun
        SET FinishedAt = SYSUTCDATETIME(),
            Status = N'Failed',
            Notes = ERROR_MESSAGE()
        WHERE EtlRunId = @RunId;

        THROW;
    END CATCH
END;

GO

