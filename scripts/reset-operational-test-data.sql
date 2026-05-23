/*
    Limpieza controlada para pruebas desde cero.

    OBJETIVO
    - Vaciar registros operativos/de prueba del OLTP.
    - Vaciar datos derivados del DW/reporteria.
    - Conservar catalogos, configuracion dinamica, formularios, usuarios, roles,
      permisos y definiciones de workflow.

    CONSERVA EN OLTP
    - AspNetUsers / AspNetRoles / permisos Identity.
    - AcademicTerms, PublicationStatuses, ResearchLines, IndexingSources,
      Faculties, BroadFields, SpecificFields, DetailedFields.
    - Venues y VenueMetrics como catalogo editorial/base para formularios.
    - Projects.
    - FieldCatalog, DynamicFieldOptions, FormDefinitions, FormFields.
    - WorkflowDefinition, WorkflowStageDefinition.
    - ApiFieldMappings, si existe.

    ELIMINA EN OLTP
    - Articulos, autores/participantes, indexaciones, adjuntos, valores dinamicos.
    - Lotes de importacion/staging, errores y valores por fila.
    - Instancias/logs de workflow.
    - Matrices de registro creadas por usuarios.
    - Auditoria, metricas de reportería e historial IA.
    - Logs de integracion externa.

    ELIMINA EN DW
    - Facts y dimensiones derivadas del OLTP para que la reportería quede en cero.
    - Conserva dw.DimDate y objetos/procedimientos.

    IMPORTANTE
    - Ejecutar solo en entorno de pruebas/desarrollo.
    - Realizar backup antes si hay alguna duda.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    -- OLTP: TesisDB_Extensible
    -------------------------------------------------------------------------
    USE [TesisDB_Extensible];

    -- Workflow operativo ligado a lotes.
    IF OBJECT_ID(N'dbo.WorkflowActionLog', N'U') IS NOT NULL DELETE FROM dbo.WorkflowActionLog;
    IF OBJECT_ID(N'dbo.WorkflowStageInstance', N'U') IS NOT NULL DELETE FROM dbo.WorkflowStageInstance;
    IF OBJECT_ID(N'dbo.WorkflowInstance', N'U') IS NOT NULL DELETE FROM dbo.WorkflowInstance;

    -- Matrices/drafts de registro masivo.
    IF OBJECT_ID(N'dbo.RegistrationMatrixCell', N'U') IS NOT NULL DELETE FROM dbo.RegistrationMatrixCell;
    IF OBJECT_ID(N'dbo.RegistrationMatrixRow', N'U') IS NOT NULL DELETE FROM dbo.RegistrationMatrixRow;
    IF OBJECT_ID(N'dbo.RegistrationMatrixColumn', N'U') IS NOT NULL DELETE FROM dbo.RegistrationMatrixColumn;
    IF OBJECT_ID(N'dbo.RegistrationMatrix', N'U') IS NOT NULL DELETE FROM dbo.RegistrationMatrix;

    -- Staging / importacion / ingesta externa.
    IF OBJECT_ID(N'dbo.ImportBatchRowValue', N'U') IS NOT NULL DELETE FROM dbo.ImportBatchRowValue;
    IF OBJECT_ID(N'dbo.ImportBatchError', N'U') IS NOT NULL DELETE FROM dbo.ImportBatchError;
    IF OBJECT_ID(N'dbo.ImportBatchRow', N'U') IS NOT NULL DELETE FROM dbo.ImportBatchRow;
    IF OBJECT_ID(N'dbo.ImportBatch', N'U') IS NOT NULL DELETE FROM dbo.ImportBatch;
    IF OBJECT_ID(N'dbo.ExternalIntegrationLog', N'U') IS NOT NULL DELETE FROM dbo.ExternalIntegrationLog;

    -- Articulos y datos dependientes.
    IF OBJECT_ID(N'dbo.ArticleParticipantDynamicFieldValues', N'U') IS NOT NULL DELETE FROM dbo.ArticleParticipantDynamicFieldValues;
    IF OBJECT_ID(N'dbo.DynamicFieldValues', N'U') IS NOT NULL DELETE FROM dbo.DynamicFieldValues;
    IF OBJECT_ID(N'dbo.ArticleIndexings', N'U') IS NOT NULL DELETE FROM dbo.ArticleIndexings;
    IF OBJECT_ID(N'dbo.ArticleFiles', N'U') IS NOT NULL DELETE FROM dbo.ArticleFiles;
    IF OBJECT_ID(N'dbo.ArticleParticipants', N'U') IS NOT NULL DELETE FROM dbo.ArticleParticipants;
    IF OBJECT_ID(N'dbo.Articles', N'U') IS NOT NULL DELETE FROM dbo.Articles;

    -- Trazabilidad de pruebas y modulo IA.
    IF OBJECT_ID(N'dbo.IntelligenceTrainingAlgorithmMetrics', N'U') IS NOT NULL DELETE FROM dbo.IntelligenceTrainingAlgorithmMetrics;
    IF OBJECT_ID(N'dbo.IntelligenceTrainingRuns', N'U') IS NOT NULL DELETE FROM dbo.IntelligenceTrainingRuns;
    IF OBJECT_ID(N'dbo.ReportingPerformanceMetrics', N'U') IS NOT NULL DELETE FROM dbo.ReportingPerformanceMetrics;
    IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DELETE FROM dbo.AuditLogs;

    -- Reinicio de identidades operativas.
    IF OBJECT_ID(N'dbo.WorkflowActionLog', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.WorkflowActionLog', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.WorkflowStageInstance', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.WorkflowStageInstance', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.WorkflowInstance', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.WorkflowInstance', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.RegistrationMatrixCell', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.RegistrationMatrixCell', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.RegistrationMatrixRow', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.RegistrationMatrixRow', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.RegistrationMatrixColumn', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.RegistrationMatrixColumn', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.RegistrationMatrix', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.RegistrationMatrix', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ImportBatchRowValue', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ImportBatchRowValue', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ImportBatchError', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ImportBatchError', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ImportBatchRow', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ImportBatchRow', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ImportBatch', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ImportBatch', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ArticleParticipantDynamicFieldValues', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ArticleParticipantDynamicFieldValues', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.DynamicFieldValues', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.DynamicFieldValues', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ArticleFiles', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ArticleFiles', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ArticleParticipants', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ArticleParticipants', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.Articles', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.Articles', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.IntelligenceTrainingAlgorithmMetrics', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.IntelligenceTrainingAlgorithmMetrics', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.IntelligenceTrainingRuns', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.IntelligenceTrainingRuns', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.ReportingPerformanceMetrics', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.ReportingPerformanceMetrics', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DBCC CHECKIDENT ('dbo.AuditLogs', RESEED, 0) WITH NO_INFOMSGS;

    -------------------------------------------------------------------------
    -- DW / Reporteria: TesisDW_Extensible
    -------------------------------------------------------------------------
    USE [TesisDW_Extensible];

    -- Facts primero por dependencias.
    IF OBJECT_ID(N'dw.FactParticipantDynamicAttribute', N'U') IS NOT NULL DELETE FROM dw.FactParticipantDynamicAttribute;
    IF OBJECT_ID(N'dw.FactArticleDynamicAttribute', N'U') IS NOT NULL DELETE FROM dw.FactArticleDynamicAttribute;
    IF OBJECT_ID(N'dw.FactArticleIndexing', N'U') IS NOT NULL DELETE FROM dw.FactArticleIndexing;
    IF OBJECT_ID(N'dw.FactArticleAuthor', N'U') IS NOT NULL DELETE FROM dw.FactArticleAuthor;
    IF OBJECT_ID(N'dw.FactVenueMetricYear', N'U') IS NOT NULL DELETE FROM dw.FactVenueMetricYear;
    IF OBJECT_ID(N'dw.FactArticlePublication', N'U') IS NOT NULL DELETE FROM dw.FactArticlePublication;
    IF OBJECT_ID(N'dw.FactWorkflowAction', N'U') IS NOT NULL DELETE FROM dw.FactWorkflowAction;
    IF OBJECT_ID(N'dw.FactWorkflowStage', N'U') IS NOT NULL DELETE FROM dw.FactWorkflowStage;
    IF OBJECT_ID(N'dw.FactValidationError', N'U') IS NOT NULL DELETE FROM dw.FactValidationError;
    IF OBJECT_ID(N'dw.FactRegistrationRow', N'U') IS NOT NULL DELETE FROM dw.FactRegistrationRow;
    IF OBJECT_ID(N'dw.FactRegistrationMatrix', N'U') IS NOT NULL DELETE FROM dw.FactRegistrationMatrix;
    IF OBJECT_ID(N'dw.FactRegistrationBatch', N'U') IS NOT NULL DELETE FROM dw.FactRegistrationBatch;

    -- Dimensiones derivadas/snapshots. Se preserva DimDate.
    IF OBJECT_ID(N'dw.DimArticle', N'U') IS NOT NULL DELETE FROM dw.DimArticle;
    IF OBJECT_ID(N'dw.DimAuthor', N'U') IS NOT NULL DELETE FROM dw.DimAuthor;
    IF OBJECT_ID(N'dw.DimRegistrationMatrix', N'U') IS NOT NULL DELETE FROM dw.DimRegistrationMatrix;
    IF OBJECT_ID(N'dw.DimValidationError', N'U') IS NOT NULL DELETE FROM dw.DimValidationError;

    -- Dimensiones que se reconstruyen desde catalogos/configuracion por ETL.
    IF OBJECT_ID(N'dw.DimAcademicTerm', N'U') IS NOT NULL DELETE FROM dw.DimAcademicTerm;
    IF OBJECT_ID(N'dw.DimPublicationStatus', N'U') IS NOT NULL DELETE FROM dw.DimPublicationStatus;
    IF OBJECT_ID(N'dw.DimResearchLine', N'U') IS NOT NULL DELETE FROM dw.DimResearchLine;
    IF OBJECT_ID(N'dw.DimIndexingSource', N'U') IS NOT NULL DELETE FROM dw.DimIndexingSource;
    IF OBJECT_ID(N'dw.DimFaculty', N'U') IS NOT NULL DELETE FROM dw.DimFaculty;
    IF OBJECT_ID(N'dw.DimField', N'U') IS NOT NULL DELETE FROM dw.DimField;
    IF OBJECT_ID(N'dw.DimVenue', N'U') IS NOT NULL DELETE FROM dw.DimVenue;
    IF OBJECT_ID(N'dw.DimProject', N'U') IS NOT NULL DELETE FROM dw.DimProject;
    IF OBJECT_ID(N'dw.DimDynamicField', N'U') IS NOT NULL DELETE FROM dw.DimDynamicField;
    IF OBJECT_ID(N'dw.DimWorkflowStage', N'U') IS NOT NULL DELETE FROM dw.DimWorkflowStage;
    IF OBJECT_ID(N'dw.DimWorkflow', N'U') IS NOT NULL DELETE FROM dw.DimWorkflow;
    IF OBJECT_ID(N'dw.DimBatchStatus', N'U') IS NOT NULL DELETE FROM dw.DimBatchStatus;
    IF OBJECT_ID(N'dw.DimRegistrationSource', N'U') IS NOT NULL DELETE FROM dw.DimRegistrationSource;
    IF OBJECT_ID(N'dw.DimSystemRole', N'U') IS NOT NULL DELETE FROM dw.DimSystemRole;
    IF OBJECT_ID(N'dw.DimUser', N'U') IS NOT NULL DELETE FROM dw.DimUser;

    -- Auditoria ETL derivada de pruebas.
    IF OBJECT_ID(N'etl.EtlRowAudit', N'U') IS NOT NULL DELETE FROM etl.EtlRowAudit;
    IF OBJECT_ID(N'etl.EtlRun', N'U') IS NOT NULL DELETE FROM etl.EtlRun;

    -- Reinicio de identidades DW si existen.
    IF OBJECT_ID(N'dw.FactParticipantDynamicAttribute', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactParticipantDynamicAttribute', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactArticleDynamicAttribute', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactArticleDynamicAttribute', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactArticleIndexing', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactArticleIndexing', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactArticleAuthor', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactArticleAuthor', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactVenueMetricYear', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactVenueMetricYear', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactArticlePublication', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactArticlePublication', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactWorkflowAction', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactWorkflowAction', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactWorkflowStage', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactWorkflowStage', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactValidationError', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactValidationError', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactRegistrationRow', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactRegistrationRow', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactRegistrationMatrix', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactRegistrationMatrix', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.FactRegistrationBatch', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.FactRegistrationBatch', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.DimArticle', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.DimArticle', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.DimAuthor', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.DimAuthor', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.DimRegistrationMatrix', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.DimRegistrationMatrix', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dw.DimValidationError', N'U') IS NOT NULL DBCC CHECKIDENT ('dw.DimValidationError', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'etl.EtlRowAudit', N'U') IS NOT NULL DBCC CHECKIDENT ('etl.EtlRowAudit', RESEED, 0) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'etl.EtlRun', N'U') IS NOT NULL DBCC CHECKIDENT ('etl.EtlRun', RESEED, 0) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    SELECT 'OK' AS Status, 'Limpieza operativa completada. Catalogos/configuracion/usuarios conservados.' AS Message;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity int = ERROR_SEVERITY();
    DECLARE @ErrorState int = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
