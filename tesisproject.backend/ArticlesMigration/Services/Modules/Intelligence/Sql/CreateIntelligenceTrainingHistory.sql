IF OBJECT_ID(N'[dbo].[IntelligenceTrainingRuns]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntelligenceTrainingRuns]
    (
        [IntelligenceTrainingRunId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IntelligenceTrainingRuns] PRIMARY KEY,
        [RunId] uniqueidentifier NOT NULL,
        [Trigger] nvarchar(60) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NOT NULL,
        [Status] nvarchar(60) NOT NULL,
        [Promoted] bit NOT NULL,
        [ActiveModelVersion] nvarchar(160) NOT NULL,
        [PromotedAlgorithm] nvarchar(160) NOT NULL,
        [SelectionReason] nvarchar(800) NOT NULL,
        [Summary] nvarchar(800) NOT NULL,
        [DatasetName] nvarchar(180) NOT NULL,
        [Target] nvarchar(180) NOT NULL,
        [ValidationStrategy] nvarchar(800) NOT NULL,
        [FeatureWindow] nvarchar(120) NOT NULL,
        [TrainingRows] int NOT NULL,
        [ValidationRows] int NOT NULL,
        [BestAlgorithm] nvarchar(160) NOT NULL,
        [BestMetric] nvarchar(40) NOT NULL,
        [BestMetricValue] decimal(18,4) NOT NULL,
        [RetrainingPolicy] nvarchar(800) NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [CreatedBy] nvarchar(256) NULL
    );

    CREATE UNIQUE INDEX [IX_IntelligenceTrainingRuns_RunId]
        ON [dbo].[IntelligenceTrainingRuns] ([RunId]);

    CREATE INDEX [IX_IntelligenceTrainingRuns_StartedAt]
        ON [dbo].[IntelligenceTrainingRuns] ([StartedAt]);

    CREATE INDEX [IX_IntelligenceTrainingRuns_ActiveModelVersion]
        ON [dbo].[IntelligenceTrainingRuns] ([ActiveModelVersion]);
END;

IF OBJECT_ID(N'[dbo].[IntelligenceTrainingAlgorithmMetrics]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntelligenceTrainingAlgorithmMetrics]
    (
        [IntelligenceTrainingAlgorithmMetricId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IntelligenceTrainingAlgorithmMetrics] PRIMARY KEY,
        [IntelligenceTrainingRunId] int NOT NULL,
        [Algorithm] nvarchar(160) NOT NULL,
        [Family] nvarchar(80) NOT NULL,
        [Purpose] nvarchar(600) NOT NULL,
        [MetricName] nvarchar(40) NOT NULL,
        [Mae] decimal(18,4) NOT NULL,
        [Rmse] decimal(18,4) NOT NULL,
        [Mape] decimal(18,4) NOT NULL,
        [Score] decimal(18,4) NOT NULL,
        [IsBest] bit NOT NULL,
        [Status] nvarchar(60) NOT NULL,
        [ThesisUse] nvarchar(800) NOT NULL,
        CONSTRAINT [FK_IntelligenceTrainingAlgorithmMetrics_IntelligenceTrainingRuns_IntelligenceTrainingRunId]
            FOREIGN KEY ([IntelligenceTrainingRunId])
            REFERENCES [dbo].[IntelligenceTrainingRuns] ([IntelligenceTrainingRunId])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_IntelligenceTrainingAlgorithmMetrics_IntelligenceTrainingRunId_IsBest]
        ON [dbo].[IntelligenceTrainingAlgorithmMetrics] ([IntelligenceTrainingRunId], [IsBest]);
END;
