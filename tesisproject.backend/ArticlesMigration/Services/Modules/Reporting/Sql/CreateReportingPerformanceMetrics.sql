SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ReportingPerformanceMetrics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportingPerformanceMetrics
    (
        ReportingPerformanceMetricId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportingPerformanceMetrics PRIMARY KEY,
        Operation nvarchar(80) NOT NULL,
        Module nvarchar(80) NOT NULL CONSTRAINT DF_ReportingPerformanceMetrics_Module DEFAULT(N'Reporting'),
        StartedAtUtc datetime2(7) NOT NULL,
        FinishedAtUtc datetime2(7) NOT NULL,
        DurationMs bigint NOT NULL,
        Succeeded bit NOT NULL,
        UserId nvarchar(450) NULL,
        UserName nvarchar(256) NULL,
        Roles nvarchar(500) NULL,
        FilterSummaryJson nvarchar(4000) NULL,
        ResultCount int NULL,
        PayloadBytes bigint NULL,
        ManualBaselineMs bigint NOT NULL CONSTRAINT DF_ReportingPerformanceMetrics_ManualBaselineMs DEFAULT(0),
        EstimatedTimeSavedMs bigint NOT NULL CONSTRAINT DF_ReportingPerformanceMetrics_EstimatedTimeSavedMs DEFAULT(0),
        ErrorMessage nvarchar(1000) NULL,
        CreatedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_ReportingPerformanceMetrics_CreatedAtUtc DEFAULT(SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReportingPerformanceMetrics_StartedAtUtc' AND object_id = OBJECT_ID(N'dbo.ReportingPerformanceMetrics'))
    CREATE INDEX IX_ReportingPerformanceMetrics_StartedAtUtc ON dbo.ReportingPerformanceMetrics(StartedAtUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReportingPerformanceMetrics_Operation' AND object_id = OBJECT_ID(N'dbo.ReportingPerformanceMetrics'))
    CREATE INDEX IX_ReportingPerformanceMetrics_Operation ON dbo.ReportingPerformanceMetrics(Operation);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReportingPerformanceMetrics_UserId' AND object_id = OBJECT_ID(N'dbo.ReportingPerformanceMetrics'))
    CREATE INDEX IX_ReportingPerformanceMetrics_UserId ON dbo.ReportingPerformanceMetrics(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReportingPerformanceMetrics_Operation_StartedAtUtc' AND object_id = OBJECT_ID(N'dbo.ReportingPerformanceMetrics'))
    CREATE INDEX IX_ReportingPerformanceMetrics_Operation_StartedAtUtc ON dbo.ReportingPerformanceMetrics(Operation, StartedAtUtc);
GO
