USE [TesisDB_Extensible];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchRow_ImportBatchId_RowNumber'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchRow')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchRow_ImportBatchId_RowNumber
    ON dbo.ImportBatchRow (ImportBatchId, RowNumber);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchRow_ImportBatchId_RowStatus'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchRow')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchRow_ImportBatchId_RowStatus
    ON dbo.ImportBatchRow (ImportBatchId, RowStatus);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchRowValue_ImportBatchRowId'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchRowValue')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchRowValue_ImportBatchRowId
    ON dbo.ImportBatchRowValue (ImportBatchRowId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchRowValue_ImportBatchRowId_FieldId'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchRowValue')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchRowValue_ImportBatchRowId_FieldId
    ON dbo.ImportBatchRowValue (ImportBatchRowId, FieldId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchError_ImportBatchId'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchError')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchError_ImportBatchId
    ON dbo.ImportBatchError (ImportBatchId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchError_ImportBatchRowId'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchError')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchError_ImportBatchRowId
    ON dbo.ImportBatchError (ImportBatchRowId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatchError_ImportBatchId_Severity'
      AND object_id = OBJECT_ID(N'dbo.ImportBatchError')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportBatchError_ImportBatchId_Severity
    ON dbo.ImportBatchError (ImportBatchId, Severity);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Articles_ExternalSource_ExternalId'
      AND object_id = OBJECT_ID(N'dbo.Articles')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Articles_ExternalSource_ExternalId
    ON dbo.Articles (ExternalSource, ExternalId)
    WHERE ExternalSource IS NOT NULL AND ExternalId IS NOT NULL;
END
GO

UPDATE STATISTICS dbo.ImportBatchRow;
UPDATE STATISTICS dbo.ImportBatchRowValue;
UPDATE STATISTICS dbo.ImportBatchError;
UPDATE STATISTICS dbo.Articles;
GO
