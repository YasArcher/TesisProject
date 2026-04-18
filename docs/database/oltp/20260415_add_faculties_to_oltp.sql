/*
    OLTP extensible - Facultades institucionales
    Ejecutar sobre la base transaccional actual: TesisDB_Extensible.
    Script idempotente: puede ejecutarse mas de una vez.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Faculties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Faculties
    (
        FacultyId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Faculties PRIMARY KEY,
        Code NVARCHAR(40) NULL,
        Name NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_Faculties_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Faculties_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.IndexingSources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IndexingSources
    (
        IndexingSourceId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_IndexingSources PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_IndexingSources_IsActive DEFAULT (1)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IndexingSources_Name'
      AND object_id = OBJECT_ID(N'dbo.IndexingSources')
)
BEGIN
    CREATE UNIQUE INDEX IX_IndexingSources_Name
        ON dbo.IndexingSources(Name);
END;
GO

IF OBJECT_ID(N'dbo.ArticleIndexings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArticleIndexings
    (
        ArticleId INT NOT NULL,
        IndexingSourceId INT NOT NULL,
        CONSTRAINT PK_ArticleIndexings PRIMARY KEY (ArticleId, IndexingSourceId),
        CONSTRAINT FK_ArticleIndexings_Articles_ArticleId
            FOREIGN KEY (ArticleId) REFERENCES dbo.Articles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ArticleIndexings_IndexingSources_IndexingSourceId
            FOREIGN KEY (IndexingSourceId) REFERENCES dbo.IndexingSources(IndexingSourceId) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleIndexings_IndexingSourceId'
      AND object_id = OBJECT_ID(N'dbo.ArticleIndexings')
)
BEGIN
    CREATE INDEX IX_ArticleIndexings_IndexingSourceId
        ON dbo.ArticleIndexings(IndexingSourceId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Faculties_Name'
      AND object_id = OBJECT_ID(N'dbo.Faculties')
)
BEGIN
    CREATE UNIQUE INDEX IX_Faculties_Name
        ON dbo.Faculties(Name);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Faculties_Code'
      AND object_id = OBJECT_ID(N'dbo.Faculties')
)
BEGIN
    CREATE UNIQUE INDEX IX_Faculties_Code
        ON dbo.Faculties(Code)
        WHERE Code IS NOT NULL AND Code <> N'';
END;
GO

IF COL_LENGTH(N'dbo.Articles', N'FacultyId') IS NULL
BEGIN
    ALTER TABLE dbo.Articles
        ADD FacultyId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Articles_FacultyId'
      AND object_id = OBJECT_ID(N'dbo.Articles')
)
BEGIN
    CREATE INDEX IX_Articles_FacultyId
        ON dbo.Articles(FacultyId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Articles_Faculties_FacultyId'
)
BEGIN
    ALTER TABLE dbo.Articles WITH CHECK
        ADD CONSTRAINT FK_Articles_Faculties_FacultyId
        FOREIGN KEY (FacultyId)
        REFERENCES dbo.Faculties(FacultyId)
        ON DELETE SET NULL;
END;
GO

IF OBJECT_ID(N'dbo.FieldCatalog', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM dbo.FieldCatalog
       WHERE EntityName = N'Article'
         AND FieldKey = N'FacultyId'
   )
BEGIN
    INSERT INTO dbo.FieldCatalog
        (EntityName, FieldKey, FieldLabel, DataType, SourceType, PhysicalTableName, PhysicalColumnName, ReferenceTableName,
         IsSystemField, IsDynamic, IsRequired, IsVisible, IsEditable, IsFilterable, IsActive, DisplayOrder,
         Placeholder, HelpText, CreatedAt)
    VALUES
        (N'Article', N'FacultyId', N'Facultad', N'int', N'Physical', N'Articles', N'FacultyId', N'Faculties',
         1, 0, 0, 1, 1, 1, 1, 180,
         N'Seleccione la facultad institucional', N'Facultad responsable o asociada al registro del articulo.', SYSUTCDATETIME());
END;
GO

MERGE dbo.IndexingSources AS T
USING (VALUES
    (N'Scopus'),
    (N'Web of Science'),
    (N'Latindex'),
    (N'SciELO'),
    (N'DOAJ'),
    (N'Redalyc'),
    (N'Dialnet'),
    (N'ERIC')
) AS S(Name)
ON T.Name = S.Name
WHEN NOT MATCHED THEN
    INSERT (Name, IsActive) VALUES (S.Name, 1);
GO

/* Semilla sugerida. Ajustar nombres/codigos si la institucion maneja otro catalogo oficial. */
IF NOT EXISTS (SELECT 1 FROM dbo.Faculties)
BEGIN
    INSERT INTO dbo.Faculties (Code, Name)
    VALUES
        (N'FISEI', N'Facultad de Ingenieria en Sistemas, Electronica e Industrial'),
        (N'FCHE', N'Facultad de Ciencias Humanas y de la Educacion'),
        (N'FCS', N'Facultad de Ciencias de la Salud'),
        (N'FCAUD', N'Facultad de Contabilidad y Auditoria'),
        (N'JCS', N'Facultad de Jurisprudencia y Ciencias Sociales'),
        (N'FCAGP', N'Facultad de Ciencias Agropecuarias'),
        (N'FICM', N'Facultad de Ingenieria Civil y Mecanica'),
        (N'FCIAL', N'Facultad de Ciencia e Ingenieria en Alimentos y Biotecnologia'),
        (N'DI-DIDE', N'Direccion de Investigacion y Desarrollo');
END;
GO
