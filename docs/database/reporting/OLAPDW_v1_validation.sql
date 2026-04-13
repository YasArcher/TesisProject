USE [TesisDW_Extensible]
GO

/* =========================================================
   OLAPDW v1 - Validaciones posteriores a sp_RunFullLoad
   Ejecutar despues de: EXEC etl.sp_RunFullLoad;
   ========================================================= */

/* 1. Estado del ultimo ETL */
SELECT TOP (10)
    EtlRunId,
    ProcessName,
    StartedAt,
    FinishedAt,
    Status,
    Notes
FROM etl.EtlRun
ORDER BY EtlRunId DESC;

/* 2. Conteos principales DW */
SELECT 'dw.DimArticle' AS ObjectName, COUNT(*) AS TotalRows FROM dw.DimArticle
UNION ALL SELECT 'dw.DimAuthor', COUNT(*) FROM dw.DimAuthor
UNION ALL SELECT 'dw.DimVenue', COUNT(*) FROM dw.DimVenue
UNION ALL SELECT 'dw.DimIndexingSource', COUNT(*) FROM dw.DimIndexingSource
UNION ALL SELECT 'dw.DimRegistrationSource', COUNT(*) FROM dw.DimRegistrationSource
UNION ALL SELECT 'dw.DimBatchStatus', COUNT(*) FROM dw.DimBatchStatus
UNION ALL SELECT 'dw.FactArticlePublication', COUNT(*) FROM dw.FactArticlePublication
UNION ALL SELECT 'dw.FactArticleAuthor', COUNT(*) FROM dw.FactArticleAuthor
UNION ALL SELECT 'dw.FactArticleIndexing', COUNT(*) FROM dw.FactArticleIndexing
UNION ALL SELECT 'dw.FactRegistrationBatch', COUNT(*) FROM dw.FactRegistrationBatch
UNION ALL SELECT 'dw.FactRegistrationRow', COUNT(*) FROM dw.FactRegistrationRow
UNION ALL SELECT 'dw.FactValidationError', COUNT(*) FROM dw.FactValidationError
UNION ALL SELECT 'dw.FactWorkflowStage', COUNT(*) FROM dw.FactWorkflowStage
UNION ALL SELECT 'dw.FactWorkflowAction', COUNT(*) FROM dw.FactWorkflowAction;

/* 3. Comparacion rapida OLTP vs DW */
SELECT 'Articles' AS EntityName,
       (SELECT COUNT(*) FROM [TesisDB_Extensible].dbo.Articles) AS OltpRows,
       (SELECT COUNT(*) FROM dw.DimArticle WHERE IsCurrent = 1) AS DwRows;

SELECT 'ArticleParticipants' AS EntityName,
       (SELECT COUNT(*) FROM [TesisDB_Extensible].dbo.ArticleParticipants) AS OltpRows,
       (SELECT COUNT(*) FROM dw.DimAuthor WHERE IsCurrent = 1) AS DwRows;

SELECT 'ImportBatch' AS EntityName,
       (SELECT COUNT(*) FROM [TesisDB_Extensible].dbo.ImportBatch) AS OltpRows,
       (SELECT COUNT(*) FROM dw.FactRegistrationBatch) AS DwRows;

SELECT 'WorkflowStageInstance' AS EntityName,
       (SELECT COUNT(*) FROM [TesisDB_Extensible].dbo.WorkflowStageInstance) AS OltpRows,
       (SELECT COUNT(*) FROM dw.FactWorkflowStage) AS DwRows;

SELECT 'WorkflowActionLog' AS EntityName,
       (SELECT COUNT(*) FROM [TesisDB_Extensible].dbo.WorkflowActionLog) AS OltpRows,
       (SELECT COUNT(*) FROM dw.FactWorkflowAction) AS DwRows;

/* 4. Llaves nulas que afectarian reporteria */
SELECT 'FactArticlePublication.ArticleKey' AS CheckName, COUNT(*) AS NullRows
FROM dw.FactArticlePublication WHERE ArticleKey IS NULL
UNION ALL SELECT 'FactArticlePublication.CreatedDateKey', COUNT(*) FROM dw.FactArticlePublication WHERE CreatedDateKey IS NULL
UNION ALL SELECT 'FactArticleAuthor.ArticleKey', COUNT(*) FROM dw.FactArticleAuthor WHERE ArticleKey IS NULL
UNION ALL SELECT 'FactArticleAuthor.AuthorKey', COUNT(*) FROM dw.FactArticleAuthor WHERE AuthorKey IS NULL
UNION ALL SELECT 'FactRegistrationBatch.BatchStatusKey', COUNT(*) FROM dw.FactRegistrationBatch WHERE BatchStatusKey IS NULL
UNION ALL SELECT 'FactWorkflowStage.BatchId_OLTP', COUNT(*) FROM dw.FactWorkflowStage WHERE BatchId_OLTP IS NULL
UNION ALL SELECT 'FactWorkflowAction.BatchId_OLTP', COUNT(*) FROM dw.FactWorkflowAction WHERE BatchId_OLTP IS NULL;

/* 5. Vistas base para dashboard */
SELECT TOP (20) * FROM dw.vw_KPI_ProduccionCientifica;
SELECT TOP (20) * FROM dw.vw_KPI_CalidadCarga;
SELECT TOP (20) * FROM dw.vw_KPI_Workflow;
SELECT TOP (20) * FROM dw.vw_Articles_ByYear ORDER BY YearNumber DESC;
SELECT TOP (20) * FROM dw.vw_Articles_ByIndexingSource ORDER BY TotalArticles DESC;
SELECT TOP (20) * FROM dw.vw_Workflow_Batches_ByCurrentStage ORDER BY BatchId_OLTP DESC;
