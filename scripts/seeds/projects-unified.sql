-- Projects Unified baseline adapted from 9.txt; SHA256 6d73aaeda34002c5fbbd3a58353c311fee91e999dfd138b57eb3753303e18c0e
-- Execute with Invoke-ProjectsSeed.ps1. Defaults to preview; no USE, jobs or external-user INSERTs.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF DB_NAME() <> @ExpectedDatabase OR DB_NAME() IN (N'tesis',N'master',N'model',N'msdb',N'tempdb') THROW 51000, 'Unexpected/non-Unified target database.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistoryUnifiedDide WHERE MigrationId='20260908201802_HardenArticleReadModel') THROW 51000, 'Required Unified migrations missing.', 1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Actor int, @ActorCount int;
SELECT @Actor=MIN(a.IdUser), @ActorCount=COUNT(*) FROM dbo.AppUsers a JOIN dbo.AspNetUsers u ON u.Id=a.IdLocal WHERE a.IdAsp=@OwnerAspId AND u.NormalizedEmail=UPPER(@OwnerEmail) AND EXISTS (SELECT 1 FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE ur.UserId=u.Id AND r.NormalizedName=N'SUPERADMIN');
IF @ActorCount<>1 THROW 51001, 'Expected a unique, mapped Unified superadmin; no identity will be created by this seed.', 1;
DECLARE @Changes TABLE (TableName nvarchar(128), RowsInSeed int, Inserts int, FixtureUpdates int);
CREATE TABLE #SeedDocumentTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedDocumentTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Memorando inicial',1,0),
(2,N'Memorando Informe Final',1,0),
(3,N'Resolucion Prorroga',1,0),
(4,N'Resolucion Informe Final',1,0),
(5,N'Contrato Auspicio',1,0),
(6,N'Resolucion de visita',1,0);
IF EXISTS (SELECT 1 FROM dbo.[DocumentTypes] d JOIN #SeedDocumentTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in DocumentTypes; seed aborted.', 1;
INSERT @Changes SELECT N'DocumentTypes',6,(SELECT COUNT(*) FROM #SeedDocumentTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[DocumentTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[DocumentTypes] d JOIN #SeedDocumentTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedGroupTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedGroupTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Integrantes',1,0),
(2,N'Investigadores',1,0);
IF EXISTS (SELECT 1 FROM dbo.[GroupTypes] d JOIN #SeedGroupTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT ((d.Id=1 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT CAST(1 AS int),CAST(N'Integrantes' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(1 AS bit))))) THROW 51002, 'Ambiguous existing data in GroupTypes; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.Groups g JOIN dbo.GroupTypes d ON d.Id=g.GroupTypeId JOIN #SeedGroupTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND (g.Name IS NULL OR (g.Name NOT LIKE N'CA-%' AND g.Name NOT LIKE N'CT-%'))) THROW 51003, 'Known GroupTypes fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'GroupTypes',2,(SELECT COUNT(*) FROM #SeedGroupTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[GroupTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[GroupTypes] d JOIN #SeedGroupTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedConvocations ([Id] int,[Name] nvarchar(200),[Code] nvarchar(max),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedConvocations ([Id],[Name],[Code],[IsActive],[IsLocked]) VALUES
(1,N'SIN CONVOCATORIA',NULL,1,0),
(2,N'CEDIA I+D+i 2022',NULL,1,0),
(3,N'CEDIA I+D+i 2023',NULL,1,0),
(4,N'CEDIA I+D+i-2023',NULL,1,0),
(5,N'CEDIA I+D+i-2024',NULL,1,0),
(6,N'CEPRA 2022',NULL,1,0),
(7,N'CONVENIO CON UNIVERSIDAD DE COMPOSTELA',NULL,1,0),
(8,N'CONVENIO INTERINSTITUCIONAL POLITECNICA DE CHIMBORAZA Y LA UTA',NULL,1,0),
(9,N'CONVOCATORI PRESELECCIÓN PERSONAL ACADÉMICO',NULL,1,0),
(10,N'CONVOCATORIA',NULL,1,0),
(11,N'CONVOCATORIA ABIERTA',NULL,1,0),
(12,N'CONVOCATORIA ADSIDEO COOPERACION 2021',NULL,1,0),
(13,N'CONVOCATORIA DOCENTES DIDE',NULL,1,0),
(14,N'CONVOCATORIA I + D + I 2024 DOCENTES DIDE',NULL,1,0),
(15,N'CONVOCATORIA I + D + i 2024 DOCENTES CINTRATADOS POR DIDE',NULL,1,0),
(16,N'CONVOCATORIA I+D 2022',NULL,1,0),
(17,N'CONVOCATORIA I+D 2022 DOCENTE DIDE',NULL,1,0),
(18,N'CONVOCATORIA I+D 2023',NULL,1,0),
(19,N'CONVOCATORIA I+D 2024 DOCENTE DIDE',NULL,1,0),
(20,N'CONVOCATORIA I+D+I 2023',NULL,1,0),
(21,N'CONVOCATORIA I+D+I 2024',NULL,1,0),
(22,N'CONVOCATORIA I+D+i 2020',NULL,1,0),
(23,N'CONVOCATORIA I+D+i 2021',NULL,1,0),
(24,N'CONVOCATORIA I+D+i 2023',NULL,1,0),
(25,N'CONVOCATORIA I+D+i 2024',NULL,1,0),
(26,N'CONVOCATORIA I+D+i 2025',NULL,1,0),
(27,N'CONVOCATORIA I+D+i 2025 - 2026',NULL,1,0),
(28,N'CONVOCATORIA I+D+i 2025-2026',NULL,1,0),
(29,N'CONVOCATORIA I+D+i2025-2026',NULL,1,0),
(30,N'CONVOCATORIA IMPLEMENTACIÓN DE LABORATORIOS',NULL,1,0),
(31,N'CONVOCATORIA PARA LA APROBACIÓN DE PROYECTOS DODENTES DIDE',NULL,1,0),
(32,N'CONVOCATORIA PARA LA APROBACIÓN Y FINANCIAMIENTO DE PROYECTOS DE INVESTIGACIÓN I+D+i 2025 - 2026',NULL,1,0),
(33,N'CONVOCATORIA PARA LA APROBACIÓN Y FINANCIAMIENTO I + D + i 2024 DOCENTES DIDE',NULL,1,0),
(34,N'CONVOCATORIA PRESELECCIÓN PERSONAL ACADÉMICO',NULL,1,0),
(35,N'CONVOCATORIA SEPTIEMBRE 2018 - FEBRERO 2019',NULL,1,0),
(36,N'Convocatoria Abierta para el Financiamiento de Proyectos de Investigación e Innovación I+D+i',NULL,1,0),
(37,N'Convocatoria KUKA',NULL,1,0),
(38,N'DOCENTE DIDE',NULL,1,0),
(39,N'EXTERNO',NULL,1,0),
(40,N'FIASA',NULL,1,0),
(41,N'Financiamiento de Proyectos de Investigación e Innovación I+D+i"',NULL,1,0),
(42,N'IX CEDIA-CEPRA-2015',NULL,1,0),
(43,N'Mar-Ago 2018',NULL,1,0),
(44,N'PRESENTA INFORME FINAL CON OFICIO UTA-FISEI-202-0517-M DEL 07-03-2022',NULL,1,0),
(45,N'PROYECTO VLIR CONVOCATORIA TEAM PROJECT 2022',NULL,1,0),
(46,N'SENESCYT',NULL,1,0),
(47,N'UTA-FCIAB-2021-1267-M',NULL,1,0);
IF EXISTS (SELECT 1 FROM dbo.[Convocations] d JOIN #SeedConvocations s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[Code] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[Code] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT ((d.Id=1 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[Code] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT CAST(1 AS int),CAST(N'Cutover smoke' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(N'CT' AS nvarchar(max)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(0 AS bit))))) THROW 51002, 'Ambiguous existing data in Convocations; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.Projects p JOIN dbo.[Convocations] d ON d.Id=p.[ConvocationId] JOIN #SeedConvocations s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[Code] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[Code] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND (p.ProjectCode IS NULL OR (p.ProjectCode NOT LIKE N'CA-%' AND p.ProjectCode NOT LIKE N'CT-%'))) THROW 51003, 'Known Convocations fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'Convocations',47,(SELECT COUNT(*) FROM #SeedConvocations s WHERE NOT EXISTS(SELECT 1 FROM dbo.[Convocations] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[Convocations] d JOIN #SeedConvocations s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[Code] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[Code] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProjectStates ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProjectStates ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'FINALIZADO',1,1),
(2,N'EN CIERRE',1,1),
(3,N'EN EJECUCION',1,1),
(4,N'DETENIDO',1,1),
(5,N'CANCELADO',1,1),
(6,N'PLANIFICADO',1,1);
IF EXISTS (SELECT 1 FROM dbo.[ProjectStates] d JOIN #SeedProjectStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT ((d.Id=3 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT CAST(3 AS int),CAST(N'En ejecución' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(1 AS bit))))) THROW 51002, 'Ambiguous existing data in ProjectStates; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.Projects p JOIN dbo.[ProjectStates] d ON d.Id=p.[ProjectStateId] JOIN #SeedProjectStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND (p.ProjectCode IS NULL OR (p.ProjectCode NOT LIKE N'CA-%' AND p.ProjectCode NOT LIKE N'CT-%'))) THROW 51003, 'Known ProjectStates fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'ProjectStates',6,(SELECT COUNT(*) FROM #SeedProjectStates s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProjectStates] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProjectStates] d JOIN #SeedProjectStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProjectTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProjectTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Aplicada',1,1),
(2,N'Experimental',1,1);
IF EXISTS (SELECT 1 FROM dbo.[ProjectTypes] d JOIN #SeedProjectTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT ((d.Id=1 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT CAST(1 AS int),CAST(N'Investigación' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(1 AS bit))))) THROW 51002, 'Ambiguous existing data in ProjectTypes; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.Projects p JOIN dbo.[ProjectTypes] d ON d.Id=p.[ProjectTypeId] JOIN #SeedProjectTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND (p.ProjectCode IS NULL OR (p.ProjectCode NOT LIKE N'CA-%' AND p.ProjectCode NOT LIKE N'CT-%'))) THROW 51003, 'Known ProjectTypes fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'ProjectTypes',2,(SELECT COUNT(*) FROM #SeedProjectTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProjectTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProjectTypes] d JOIN #SeedProjectTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedTransactionTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedTransactionTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Certificacion',1,1),
(2,N'Devengado',1,1),
(3,N'Anulacion',1,0);
IF EXISTS (SELECT 1 FROM dbo.[TransactionTypes] d JOIN #SeedTransactionTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in TransactionTypes; seed aborted.', 1;
INSERT @Changes SELECT N'TransactionTypes',3,(SELECT COUNT(*) FROM #SeedTransactionTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[TransactionTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[TransactionTypes] d JOIN #SeedTransactionTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedVisitStates ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedVisitStates ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Creada',1,1),
(2,N'Planificada',1,1),
(3,N'Realizada',1,1),
(4,N'Pendiente',1,1);
IF EXISTS (SELECT 1 FROM dbo.[VisitStates] d JOIN #SeedVisitStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in VisitStates; seed aborted.', 1;
INSERT @Changes SELECT N'VisitStates',4,(SELECT COUNT(*) FROM #SeedVisitStates s WHERE NOT EXISTS(SELECT 1 FROM dbo.[VisitStates] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[VisitStates] d JOIN #SeedVisitStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedObjectiveTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedObjectiveTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'General',1,1),
(2,N'Especifico',1,1);
IF EXISTS (SELECT 1 FROM dbo.[ObjectiveTypes] d JOIN #SeedObjectiveTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ObjectiveTypes; seed aborted.', 1;
INSERT @Changes SELECT N'ObjectiveTypes',2,(SELECT COUNT(*) FROM #SeedObjectiveTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ObjectiveTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ObjectiveTypes] d JOIN #SeedObjectiveTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedResearchCategoryGroups ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedResearchCategoryGroups ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Áreas de Investigación',1,0),
(2,N'Campos de Conocimiento',1,0),
(3,N'Alcances',1,0),
(4,N'Impactos',1,0);
IF EXISTS (SELECT 1 FROM dbo.[ResearchCategoryGroups] d JOIN #SeedResearchCategoryGroups s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ResearchCategoryGroups; seed aborted.', 1;
INSERT @Changes SELECT N'ResearchCategoryGroups',4,(SELECT COUNT(*) FROM #SeedResearchCategoryGroups s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ResearchCategoryGroups] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ResearchCategoryGroups] d JOIN #SeedResearchCategoryGroups s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedResearchCategoryTypes ([Id] int,[Name] nvarchar(200),[ResearchCategoryGroupId] int,[IsActive] bit,[IsFilterEnabled] bit,[IsLocked] bit);
INSERT INTO #SeedResearchCategoryTypes ([Id],[Name],[ResearchCategoryGroupId],[IsActive],[IsFilterEnabled],[IsLocked]) VALUES
(1,N'Dominio',1,1,1,0),
(2,N'Línea de investigación',1,1,1,0),
(3,N'Sub-línea de investigación',1,1,1,0),
(4,N'Campo amplio',2,1,1,0),
(5,N'Campo específico',2,1,1,0),
(6,N'Campo detallado',2,1,1,0),
(7,N'Alcance Territorial',3,1,1,0),
(8,N'Impacto Esperado',4,1,1,0);
IF EXISTS (SELECT 1 FROM dbo.[ResearchCategoryTypes] d JOIN #SeedResearchCategoryTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[ResearchCategoryGroupId],d.[IsActive],d.[IsFilterEnabled],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[ResearchCategoryGroupId],s.[IsActive],s.[IsFilterEnabled],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ResearchCategoryTypes; seed aborted.', 1;
INSERT @Changes SELECT N'ResearchCategoryTypes',8,(SELECT COUNT(*) FROM #SeedResearchCategoryTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ResearchCategoryTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ResearchCategoryTypes] d JOIN #SeedResearchCategoryTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[ResearchCategoryGroupId],d.[IsActive],d.[IsFilterEnabled],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[ResearchCategoryGroupId],s.[IsActive],s.[IsFilterEnabled],s.[IsLocked]));
CREATE TABLE #SeedResearchCategories ([Id] int,[ResearchCategoryTypeId] int,[ParentCategoryId] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedResearchCategories ([Id],[ResearchCategoryTypeId],[ParentCategoryId],[Name],[IsActive],[IsLocked]) VALUES
(1,1,NULL,N'Fortalecimiento social, democrático y educativo',1,0),
(2,1,NULL,N'Optimización de los sistemas productivos, técnicos-tecnológicos y desarrollo urbanístico',1,0),
(3,1,NULL,N'Desarrollo económico, productivo empresarial',1,0),
(4,1,NULL,N'Sistemas alimentarios, nutrición y salud pública',1,0),
(101,2,1,N'Exclusión e Integración Social',1,0),
(102,2,1,N'Políticas Públicas, Derecho y Sociedad',1,0),
(103,2,1,N'Comunicación, Sociedad, Cultura y Tecnología',1,0),
(104,2,1,N'Comportamiento Social y Educativo',1,0),
(105,2,1,N'Inclusión, Igualdad de oportunidades e Interculturalidad',1,0),
(106,2,2,N'Construcción, Estructuras, Vías, Tránsito y Transporte',1,0),
(107,2,2,N'Desarrollo y Ordenamiento Territorial',1,0),
(108,2,2,N'Energía, Desarrollo Sostenible y Gestión de Recursos Naturales',1,0),
(109,2,2,N'Salud y Bienestar',1,0),
(110,2,2,N'Diseño, Materiales, Producción, Identidad, Sostenibilidad y Tecnologías aplicadas',1,0),
(111,2,2,N'Software, Tecnologías de la Información y Ciencia de Datos',1,0),
(112,2,2,N'Electrónica, Telecomunicaciones y Sistemas de Control',1,0),
(113,2,2,N'Arquitectura, Construcción, Territorio y Paisaje',1,0),
(114,2,3,N'Desarrollo Sostenible e Innovación Empresarial',1,0),
(115,2,3,N'Economía del Desarrollo',1,0),
(116,2,4,N'Salud Humana',1,0),
(117,2,4,N'Salud Animal',1,0),
(118,2,4,N'Seguridad y Soberanía Alimentaria',1,0),
(119,2,4,N'Producción Agroalimentaria y Medio Ambiente',1,0),
(120,2,4,N'Microbiología y Biotecnología',1,0),
(2001,4,NULL,N'Educación',1,0),
(2002,4,NULL,N'Artes y humanidades',1,0),
(2003,4,NULL,N'Ciencias sociales, periodismo, información y derecho',1,0),
(2004,4,NULL,N'Administración',1,0),
(2005,4,NULL,N'Ciencias naturales, matemáticas y estadísticas',1,0),
(2006,4,NULL,N'Tecnologías de la información y la comunicación (TIC)',1,0),
(2007,4,NULL,N'Ingeniería, industría y construcción',1,0),
(2008,4,NULL,N'Agricultura, silvicultura, pesca y veterinaria',1,0),
(2009,4,NULL,N'Salud y Bienestar',1,0),
(2010,4,NULL,N'Servicios',1,0),
(2101,5,2001,N'Educación',1,0),
(2102,5,2002,N'Artes',1,0),
(2103,5,2002,N'Humanidades',1,0),
(2104,5,2002,N'Idiomas',1,0),
(2105,5,2003,N'Ciencias sociales y del comportamiento',1,0),
(2106,5,2003,N'Periodismo e información',1,0),
(2107,5,2003,N'Derecho',1,0),
(2108,5,2004,N'Educación comercial y administración',1,0),
(2109,5,2005,N'Ciencias biológicas y afines',1,0),
(2110,5,2005,N'Medio ambiente',1,0),
(2111,5,2005,N'Ciencias físicas',1,0),
(2112,5,2005,N'Matemáticas y estadística',1,0),
(2113,5,2006,N'Tecnologías de la información y la comunicación (TIC)',1,0),
(2114,5,2007,N'Ingeniaría y profesiones afines',1,0),
(2115,5,2007,N'Industría y producción',1,0),
(2116,5,2007,N'Arquitectura y construcción',1,0),
(2117,5,2008,N'Agricultura',1,0),
(2118,5,2008,N'Silvicultura',1,0),
(2119,5,2008,N'Pesca',1,0),
(2120,5,2008,N'Veterinaria',1,0),
(2121,5,2009,N'Salud',1,0),
(2122,5,2009,N'Bienestar',1,0),
(2123,5,2010,N'Servicios personales',1,0),
(2124,5,2010,N'Servicios de protección',1,0),
(2125,5,2010,N'Servicios de seguridad',1,0),
(2126,5,2010,N'Servicio de transporte',1,0),
(2201,6,2101,N'Educación',1,0),
(2202,6,2101,N'Psicopedagogía',1,0),
(2203,6,2101,N'Formación para docentes de educación preprimaria',1,0),
(2204,6,2101,N'Formación para docentes sin asignaturas de especialización',1,0),
(2205,6,2101,N'Formación para docentes con asignaturas de especialización',1,0),
(2210,6,2102,N'Técnicas audiovisuales y producción para medios de comunicación',1,0),
(2211,6,2102,N'Diseño',1,0),
(2212,6,2102,N'Artes',1,0),
(2213,6,2102,N'Música y artes escénicas',1,0),
(2220,6,2103,N'Religión y Teología',1,0),
(2221,6,2103,N'Historia y Arqueología',1,0),
(2222,6,2103,N'Filosofía',1,0),
(2230,6,2104,N'Idiomas',1,0),
(2231,6,2104,N'Literatura y lingüística',1,0),
(2240,6,2105,N'Economía',1,0),
(2241,6,2105,N'Economía Matemática',1,0),
(2242,6,2105,N'Ciencias políticas',1,0),
(2243,6,2105,N'Psicología',1,0),
(2244,6,2105,N'Estudios Sociales y Culturales',1,0),
(2245,6,2105,N'Estudios de Género',1,0),
(2246,6,2105,N'Geografía y territorio',1,0),
(2250,6,2106,N'Periodismo y comunicación',1,0),
(2251,6,2106,N'Bibliotecología, documentación y archivología',1,0),
(2260,6,2107,N'Derecho',1,0),
(2270,6,2108,N'Contabilidad y auditoría',1,0),
(2271,6,2108,N'Gestión financiera',1,0),
(2272,6,2108,N'Administración',1,0),
(2273,6,2108,N'Mercadotecnia y publicidad',1,0),
(2274,6,2108,N'Información gerencial',1,0),
(2275,6,2108,N'Comercio',1,0),
(2276,6,2108,N'Competencias laborales',1,0),
(2280,6,2109,N'Biología',1,0),
(2281,6,2109,N'Biofísica',1,0),
(2282,6,2109,N'Biofarmacéutica',1,0),
(2283,6,2109,N'Biomedicina',1,0),
(2284,6,2109,N'Bioquímica',1,0),
(2285,6,2109,N'Genética',1,0),
(2286,6,2109,N'Biodiversidad',1,0),
(2287,6,2109,N'Neurociencias',1,0),
(2290,6,2110,N'Medio ambiente',1,0),
(2291,6,2110,N'Recursos Naturales Renovables',1,0),
(2300,6,2111,N'Química',1,0),
(2301,6,2111,N'Ciencias de la Tierra',1,0),
(2302,6,2111,N'Física',1,0),
(2303,6,2111,N'Ciencias Marítimas',1,0),
(2304,6,2111,N'Ciencias Marinas',1,0),
(2310,6,2112,N'Matemáticas',1,0),
(2311,6,2112,N'Estadísticas',1,0),
(2312,6,2112,N'Logística y transporte',1,0),
(2320,6,2113,N'Computación',1,0),
(2321,6,2113,N'Diseño y administración de redes y bases de datos',1,0),
(2322,6,2113,N'Desarrollo y análisis de software y aplicaciones',1,0),
(2323,6,2113,N'Sistemas de Información',1,0),
(2330,6,2114,N'Química aplicada',1,0),
(2331,6,2114,N'Tecnología de protección del medio ambiente',1,0),
(2332,6,2114,N'Electricidad y energía',1,0),
(2333,6,2114,N'Electrónica, automatización y sonido',1,0),
(2334,6,2114,N'Mecánica y profesiones afines a la metalistería',1,0),
(2335,6,2114,N'Diseño y construcción de vehículos, barcos y aeronaves motorizadas',1,0),
(2336,6,2114,N'Tecnologías Nucleares y Energéticas',1,0),
(2337,6,2114,N'Mecatrónica',1,0),
(2338,6,2114,N'Hidráulica',1,0),
(2339,6,2114,N'Telecomunicaciones',1,0),
(2340,6,2114,N'Nanotecnología',1,0),
(2350,6,2115,N'Procesamiento de alimentos',1,0),
(2351,6,2115,N'Materiales',1,0),
(2352,6,2115,N'Productos textiles',1,0),
(2353,6,2115,N'Minería y extracción',1,0),
(2354,6,2115,N'Producción industrial',1,0),
(2355,6,2115,N'Seguridad industrial',1,0),
(2356,6,2115,N'Diseño industrial y de procesos',1,0),
(2357,6,2115,N'Mantenimiento industrial',1,0),
(2360,6,2116,N'Arquitectura, urbanismo y restauración',1,0),
(2361,6,2116,N'Construcción e ingeniería civil',1,0),
(2370,6,2117,N'Producción agrícola y ganadera',1,0),
(2371,6,2118,N'Silvicultura',1,0),
(2372,6,2119,N'Pesca',1,0),
(2373,6,2120,N'Veterinaria',1,0),
(2380,6,2121,N'Odontología',1,0),
(2381,6,2121,N'Medicina',1,0),
(2382,6,2121,N'Enfermería y obstetricia',1,0),
(2383,6,2121,N'Tecnología de diagnóstico y tratamiento médico',1,0),
(2384,6,2121,N'Terapia, Rehabilitación y Tratamiento de la Salud',1,0),
(2385,6,2121,N'Farmacia',1,0),
(2386,6,2121,N'Terapias alternativas y complementarias',1,0),
(2387,6,2121,N'Salud Pública',1,0),
(2390,6,2122,N'Asistencia a adultos mayores y discapacitados',1,0),
(2391,6,2122,N'Asistencia a la infancia y servicios para jóvenes',1,0),
(2400,6,2123,N'Peluquería y tratamiento de belleza',1,0),
(2401,6,2123,N'Hotelería y gastronomía',1,0),
(2402,6,2123,N'Actividad física',1,0),
(2403,6,2123,N'Turismo',1,0),
(2410,6,2124,N'Prevención y gestión de riesgos',1,0),
(2411,6,2124,N'Salud y seguridad ocupacional',1,0),
(2420,6,2125,N'Educación policial, militar y defensa',1,0),
(2421,6,2125,N'Seguridad ciudadana',1,0),
(2430,6,2126,N'Gestión del transporte',1,0),
(2431,7,NULL,N'Nacional',1,0),
(2432,7,NULL,N'Provincial',1,0),
(2433,7,NULL,N'Cantonal2',1,0),
(2434,7,NULL,N'Parroquial',1,0),
(2435,7,NULL,N'Institucional',1,0),
(2436,7,NULL,N'Internacional',1,0);
IF EXISTS (SELECT 1 FROM dbo.[ResearchCategories] d JOIN #SeedResearchCategories s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[ResearchCategoryTypeId],d.[ParentCategoryId],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[ResearchCategoryTypeId],s.[ParentCategoryId],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ResearchCategories; seed aborted.', 1;
INSERT @Changes SELECT N'ResearchCategories',163,(SELECT COUNT(*) FROM #SeedResearchCategories s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ResearchCategories] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ResearchCategories] d JOIN #SeedResearchCategories s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[ResearchCategoryTypeId],d.[ParentCategoryId],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[ResearchCategoryTypeId],s.[ParentCategoryId],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedFundingTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedFundingTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Externo',1,0),
(2,N'Interno',1,0),
(3,N'Propio',1,0),
(4,N'Sin financiamiento',1,0);
IF EXISTS (SELECT 1 FROM dbo.[FundingTypes] d JOIN #SeedFundingTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in FundingTypes; seed aborted.', 1;
INSERT @Changes SELECT N'FundingTypes',4,(SELECT COUNT(*) FROM #SeedFundingTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[FundingTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[FundingTypes] d JOIN #SeedFundingTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedMemberRoleTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[Flag] int,[IsLocked] bit);
INSERT INTO #SeedMemberRoleTypes ([Id],[Name],[IsActive],[Flag],[IsLocked]) VALUES
(1,N'Coordinador Principal',1,1,1),
(2,N'Coordinador Subrogante',1,1,1),
(3,N'Investigador',1,1,0),
(4,N'Investigador',1,2,0);
IF EXISTS (SELECT 1 FROM dbo.[MemberRoleTypes] d JOIN #SeedMemberRoleTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[Flag],s.[IsLocked]) AND NOT ((d.Id=1 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT CAST(1 AS int),CAST(N'Coordinador' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(1 AS int),CAST(1 AS bit))) OR (d.Id=3 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT CAST(3 AS int),CAST(N'Investigador' AS nvarchar(200)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(1 AS int),CAST(1 AS bit))))) THROW 51002, 'Ambiguous existing data in MemberRoleTypes; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.GroupMembers m JOIN dbo.Groups g ON g.GroupId=m.GroupId JOIN dbo.MemberRoleTypes d ON d.Id=m.MemberRoleId JOIN #SeedMemberRoleTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[Flag],s.[IsLocked]) AND (g.Name IS NULL OR (g.Name NOT LIKE N'CA-%' AND g.Name NOT LIKE N'CT-%'))) THROW 51003, 'Known MemberRoleTypes fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'MemberRoleTypes',4,(SELECT COUNT(*) FROM #SeedMemberRoleTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[MemberRoleTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[MemberRoleTypes] d JOIN #SeedMemberRoleTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[Flag],s.[IsLocked]));
CREATE TABLE #SeedCountries ([Id] int,[Name] nvarchar(200),[IsoCode] nvarchar(max),[IsoAlpha3] nvarchar(max),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedCountries ([Id],[Name],[IsoCode],[IsoAlpha3],[IsActive],[IsLocked]) VALUES
(1,N'Ecuador',N'EC',N'ECU',1,0),
(2,N'Argentina',N'AR',N'ARG',1,0),
(3,N'Bolivia',N'BO',N'BOL',1,0),
(4,N'Brasil',N'BR',N'BRA',1,0),
(5,N'Chile',N'CL',N'CHL',1,0),
(6,N'Colombia',N'CO',N'COL',1,0),
(7,N'Paraguay',N'PY',N'PRY',1,0),
(8,N'Peru',N'PE',N'PER',1,0),
(9,N'Uruguay',N'UY',N'URY',1,0),
(10,N'Venezuela',N'VE',N'VEN',1,0),
(11,N'México',N'MX',N'MEX',1,0),
(12,N'Guatemala',N'GT',N'GTM',1,0),
(13,N'El Salvador',N'SV',N'SLV',1,0),
(14,N'Honduras',N'HN',N'HND',1,0),
(15,N'Nicaragua',N'NI',N'NIC',1,0),
(16,N'Costa Rica',N'CR',N'CRI',1,0),
(17,N'Panamá',N'PA',N'PAN',1,0),
(18,N'Cuba',N'CU',N'CUB',1,0),
(19,N'Dominicana',N'DO',N'DOM',1,0),
(20,N'Haití',N'HT',N'HTI',1,0),
(21,N'Estados Unidos',N'US',N'USA',1,0),
(22,N'Canadá',N'CA',N'CAN',1,0),
(23,N'España',N'ES',N'ESP',1,0),
(24,N'Alemania',N'DE',N'DEU',1,0),
(25,N'Francia',N'FR',N'FRA',1,0),
(26,N'Italia',N'IT',N'ITA',1,0),
(27,N'Reino Unido',N'GB',N'GBR',1,0),
(28,N'Portugal',N'PT',N'PRT',1,0),
(29,N'Países Bajos',N'NL',N'NLD',1,0),
(30,N'Suiza',N'CH',N'CHE',1,0),
(31,N'China',N'CN',N'CHN',1,0),
(32,N'Japón',N'JP',N'JPN',1,0),
(33,N'Corea del Sur',N'KR',N'KOR',1,0),
(34,N'India',N'IN',N'IND',1,0),
(35,N'Singapur',N'SG',N'SGP',1,0),
(36,N'Australia',N'AU',N'AUS',1,0),
(37,N'Nueva Zelanda',N'NZ',N'NZL',1,0),
(38,N'Sudáfrica',N'ZA',N'ZAF',1,0),
(39,N'Egipto',N'EG',N'EGY',1,0),
(40,N'Marruecos',N'MA',N'MAR',1,0);
IF EXISTS (SELECT 1 FROM dbo.[Countries] d JOIN #SeedCountries s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsoCode] COLLATE Latin1_General_100_BIN2,d.[IsoAlpha3] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsoCode] COLLATE Latin1_General_100_BIN2,s.[IsoAlpha3] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in Countries; seed aborted.', 1;
INSERT @Changes SELECT N'Countries',40,(SELECT COUNT(*) FROM #SeedCountries s WHERE NOT EXISTS(SELECT 1 FROM dbo.[Countries] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[Countries] d JOIN #SeedCountries s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsoCode] COLLATE Latin1_General_100_BIN2,d.[IsoAlpha3] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsoCode] COLLATE Latin1_General_100_BIN2,s.[IsoAlpha3] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedInstitutions ([Id] int,[Name] nvarchar(200),[CountryId] int,[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedInstitutions ([Id],[Name],[CountryId],[IsActive],[IsLocked]) VALUES
(1,N'UNIVERSIDAD TECNICA DE MACHALA',1,1,0),
(2,N'UNIVERSIDAD ESTATAL DE BOLIVAR',1,1,0),
(3,N'UNIVERSIDAD DE CUENCA',1,1,0),
(4,N'UNIVERSIDAD CENTRAL DEL ECUADOR',1,1,0),
(5,N'UNIVERSIDAD DE GUAYAQUIL',1,1,0),
(6,N'ESCUELA POLITECNICA NACIONAL',1,1,0),
(7,N'ESCUELA SUPERIOR POLITECNICA DEL LITORAL (ESPOL)',1,1,0),
(8,N'UNIVERSIDAD DE LAS FUERZAS ARMADAS (ESPE)',1,1,0),
(9,N'UNIVERSIDAD LAICA ELOY ALFARO DE MANABI',1,1,0),
(10,N'UNIVERSIDAD TECNICA DEL NORTE',1,1,0),
(11,N'UNIVERSIDAD ESTATAL DE MILAGRO',1,1,0),
(12,N'UNIVERSIDAD NACIONAL DE LOJA',1,1,0),
(13,N'UNIVERSIDAD CATOLICA DE SANTIAGO DE GUAYAQUIL',1,1,0),
(14,N'UNIVERSIDAD POLITECNICA SALESIANA',1,1,0),
(15,N'UNIVERSIDAD ANDINA SIMON BOLIVAR',1,1,0),
(16,N'UNIVERSIDAD TECNOLOGICA EQUINOCCIAL (UTE)',1,1,0),
(17,N'UNIVERSIDAD SAN FRANCISCO DE QUITO (USFQ)',1,1,0),
(18,N'UNIVERSIDAD INTERNACIONAL DEL ECUADOR (UIDE)',1,1,0),
(19,N'UNIVERSIDAD CASA GRANDE',1,1,0),
(20,N'UNIVERSIDAD DE ESPECIALIDADES ESPIRITU SANTO (UEES)',1,1,0),
(21,N'UNIVERSIDAD ESTATAL PENINSULA DE SANTA ELENA',1,1,0),
(22,N'UNIVERSIDAD LUIS VARGAS TORRES DE ESMERALDAS',1,1,0),
(23,N'UNIVERSIDAD TECNICA ESTATAL DE QUEVEDO',1,1,0),
(24,N'UNIVERSIDAD ESTATAL AMAZONICA',1,1,0),
(25,N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES (UNIANDES)',1,1,0),
(26,N'UNIVERSIDAD METROPOLITANA',1,1,0),
(27,N'UNIVERSIDAD NACIONAL AUTONOMA DE MEXICO (UNAM)',11,1,0),
(28,N'INSTITUTO TECNOLOGICO Y DE ESTUDIOS SUPERIORES DE MONTERREY (TEC DE MONTERREY)',11,1,0),
(29,N'UNIVERSIDAD DE BUENOS AIRES (UBA)',2,1,0),
(30,N'UNIVERSIDAD NACIONAL DE LA PLATA (UNLP)',2,1,0),
(31,N'UNIVERSIDAD DE CHILE',5,1,0),
(32,N'PONTIFICIA UNIVERSIDAD CATOLICA DE CHILE',5,1,0),
(33,N'UNIVERSIDAD NACIONAL DE COLOMBIA',6,1,0),
(34,N'UNIVERSIDAD DE LOS ANDES (COLOMBIA)',6,1,0),
(35,N'UNIVERSIDAD DE SAO PAULO (USP)',4,1,0),
(36,N'UNIVERSIDADE ESTADUAL DE CAMPINAS (UNICAMP)',4,1,0);
IF EXISTS (SELECT 1 FROM dbo.[Institutions] d JOIN #SeedInstitutions s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[CountryId],d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[CountryId],s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in Institutions; seed aborted.', 1;
INSERT @Changes SELECT N'Institutions',36,(SELECT COUNT(*) FROM #SeedInstitutions s WHERE NOT EXISTS(SELECT 1 FROM dbo.[Institutions] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[Institutions] d JOIN #SeedInstitutions s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[CountryId],d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[CountryId],s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProjectExtensionTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProjectExtensionTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Por objetivos',1,0),
(2,N'Por ampliación de plazo',1,0);
IF EXISTS (SELECT 1 FROM dbo.[ProjectExtensionTypes] d JOIN #SeedProjectExtensionTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ProjectExtensionTypes; seed aborted.', 1;
INSERT @Changes SELECT N'ProjectExtensionTypes',2,(SELECT COUNT(*) FROM #SeedProjectExtensionTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProjectExtensionTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProjectExtensionTypes] d JOIN #SeedProjectExtensionTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProjectOriginTypes ([Id] int,[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProjectOriginTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'Interno',1,1),
(2,N'Extern0',1,1),
(3,N'Mixto',1,1);
IF EXISTS (SELECT 1 FROM dbo.[ProjectOriginTypes] d JOIN #SeedProjectOriginTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ProjectOriginTypes; seed aborted.', 1;
INSERT @Changes SELECT N'ProjectOriginTypes',3,(SELECT COUNT(*) FROM #SeedProjectOriginTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProjectOriginTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProjectOriginTypes] d JOIN #SeedProjectOriginTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedExportFields ([Id] int,[Key] nvarchar(100),[DisplayName] nvarchar(200),[DefaultHeader] nvarchar(200),[Description] nvarchar(500),[SourceEntity] nvarchar(100),[SourcePath] nvarchar(300),[IsActive] bit);
INSERT INTO #SeedExportFields ([Id],[Key],[DisplayName],[DefaultHeader],[Description],[SourceEntity],[SourcePath],[IsActive]) VALUES
(1,N'PROJECT_CODE',N'Código de proyecto',N'PROJECT_CODE',N'Código interno del proyecto (p.ej. PII-2025-001).',N'ProjectFlatReportDTO',N'ProjectCode',1),
(2,N'PROJECT_NAME',N'Nombre del proyecto',N'PROJECT_NAME',N'Título o nombre oficial del proyecto.',N'ProjectFlatReportDTO',N'ProjectName',1),
(3,N'PROJECT_NUMBER',N'Número de proyecto',N'PROJECT_NUMBER',N'Número secuencial/correlativo del proyecto.',N'ProjectFlatReportDTO',N'ProjectNumber',1),
(4,N'PROJECT_TYPE',N'Tipo de proyecto',N'PROJECT_TYPE',N'Tipo de proyecto (formación, investigación, vinculación, etc.).',N'ProjectFlatReportDTO',N'ProjectTypeName',1),
(5,N'PROJECT_STATE',N'Estado del proyecto',N'PROJECT_STATE',N'Estado actual del proyecto (En ejecución, Finalizado, etc.).',N'ProjectFlatReportDTO',N'ProjectStateName',1),
(6,N'CONVOCATION_NAME',N'Convocatoria',N'CONVOCATION',N'Convocatoria o línea de financiamiento del proyecto.',N'ProjectFlatReportDTO',N'ConvocationName',1),
(7,N'APPROVAL_DATE',N'Fecha de aprobación',N'APPROVAL_DATE',N'Fecha de aprobación del proyecto.',N'ProjectFlatReportDTO',N'ApprovalDate',1),
(8,N'START_DATE',N'Fecha de inicio',N'START_DATE',N'Fecha de inicio del proyecto.',N'ProjectFlatReportDTO',N'StartDate',1),
(9,N'DURATION_MONTHS',N'Duración (meses)',N'DURATION_MONTHS',N'Duración total del proyecto en meses.',N'ProjectFlatReportDTO',N'DurationInMonths',1),
(10,N'TENTATIVE_END_DATE',N'Fecha tentativa de fin',N'TENTATIVE_END_DATE',N'Fecha tentativa de finalización del proyecto.',N'ProjectFlatReportDTO',N'TentativeEndDate',1),
(11,N'REAL_END_DATE',N'Fecha real de fin',N'REAL_END_DATE',N'Fecha real de finalización del proyecto (si existe).',N'ProjectFlatReportDTO',N'RealEndDate',1),
(12,N'EXECUTION_PERCENTAGE',N'% de ejecución',N'EXECUTION_PERCENT',N'Porcentaje global de ejecución del proyecto.',N'ProjectFlatReportDTO',N'ExecutionPercentage',1),
(13,N'FACULTY_NAME',N'Facultad',N'FACULTY',N'Facultad responsable del proyecto (según directorio externo).',N'ProjectFlatReportDTO',N'FacultyName',1),
(14,N'COORDINATOR_NAME',N'Coordinador del proyecto',N'COORDINATOR_NAME',N'Nombre del coordinador/director del proyecto.',N'ProjectFlatReportDTO',N'CoordinatorName',1),
(15,N'COORDINATOR_EMAIL',N'Correo del coordinador',N'COORDINATOR_EMAIL',N'Correo electrónico del coordinador del proyecto.',N'ProjectFlatReportDTO',N'CoordinatorEmail',1),
(16,N'COORDINATOR_PHONE',N'Teléfono del coordinador',N'COORDINATOR_PHONE',N'Teléfono de contacto del coordinador del proyecto.',N'ProjectFlatReportDTO',N'CoordinatorPhone',1),
(28,N'SUBROGANT_NAME',N'Subrogante del proyecto',N'SUBROGANT_NAME',N'Nombre del coordinador subrogante del proyecto.',N'ProjectFlatReportDTO',N'SubrogantName',1),
(29,N'SUBROGANT_EMAIL',N'Correo del subrogante',N'SUBROGANT_EMAIL',N'Correo electrónico del coordinador subrogante del proyecto.',N'ProjectFlatReportDTO',N'SubrogantEmail',1),
(30,N'SUBROGANT_PHONE',N'Teléfono del subrogante',N'SUBROGANT_PHONE',N'Teléfono de contacto del coordinador subrogante del proyecto.',N'ProjectFlatReportDTO',N'SubrogantPhone',1),
(17,N'SENESCYT_MEMBERS',N'Investigadores SENESCYT',N'SENESCYT_MEMBERS',N'Lista de investigadores acreditados SENESCYT vinculados al proyecto (concatenados).',N'ProjectFlatReportDTO',N'SenescytMembers',1),
(18,N'EXTERNAL_RESEARCHER_NAMES',N'Investigadores externos',N'EXTERNAL_NAMES',N'Nombres de investigadores externos asociados al proyecto.',N'ProjectFlatReportDTO',N'ExternalResearchers',1),
(19,N'EXTERNAL_INSTITUTIONS',N'Instituciones de investigadores externos',N'EXTERNAL_INSTITUTIONS',N'Instituciones asociadas a los investigadores externos del proyecto.',N'ProjectFlatReportDTO',N'ExternalResearchers',1),
(20,N'BUDGET_INITIAL_SUMMARY',N'Presupuesto inicial (resumen)',N'BUDGET_INITIAL',N'Resumen de montos iniciales de presupuesto del proyecto (concatenados).',N'ProjectFlatReportDTO',N'Budgets',1),
(21,N'BUDGET_FUNDINGTYPES_SUMMARY',N'Tipos de financiamiento',N'BUDGET_FUNDING_TYPES',N'Resumen de tipos de financiamiento asociados al presupuesto del proyecto.',N'ProjectFlatReportDTO',N'Budgets',1),
(22,N'BUDGET_CERTIFIED_SUMMARY',N'Presupuesto certificado (resumen)',N'BUDGET_CERTIFIED',N'Resumen de montos certificados del presupuesto del proyecto.',N'ProjectFlatReportDTO',N'Budgets',1),
(23,N'BUDGET_EXECUTED_SUMMARY',N'Presupuesto ejecutado (resumen)',N'BUDGET_EXECUTED',N'Resumen de montos ejecutados del presupuesto del proyecto.',N'ProjectFlatReportDTO',N'Budgets',1),
(24,N'PRODUCT_TITLES_SUMMARY',N'Productos (títulos)',N'PRODUCT_TITLES',N'Títulos de los productos asociados al proyecto (concatenados).',N'ProjectFlatReportDTO',N'Products',1),
(25,N'PRODUCT_TYPES_SUMMARY',N'Productos (tipos)',N'PRODUCT_TYPES',N'Tipos de productos asociados al proyecto (concatenados).',N'ProjectFlatReportDTO',N'Products',1),
(26,N'MATRIX_DYNAMIC_OBJECTIVES',N'Objetivos del proyecto (bundle)',N'MATRIX_DYNAMIC_OBJECTIVES',N'Conjunto de objetivos del proyecto. Se expande a columnas específicas (objetivo general y objetivos específicos) según la plantilla.',N'ProjectFlatReportDTO',N'Objectives',1),
(27,N'MATRIX_DYNAMIC_CATEGORIES',N'Categorías de investigación (bundle)',N'MATRIX_DYNAMIC_CATEGORIES',N'Conjunto de categorías de investigación asociadas al proyecto (dominio, línea, campos). Se expande a columnas específicas según la plantilla.',N'ProjectFlatReportDTO',N'ResearchCategories',1);
IF EXISTS (SELECT 1 FROM dbo.[ExportFields] d JOIN #SeedExportFields s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Key] COLLATE Latin1_General_100_BIN2,d.[DisplayName] COLLATE Latin1_General_100_BIN2,d.[DefaultHeader] COLLATE Latin1_General_100_BIN2,d.[Description] COLLATE Latin1_General_100_BIN2,d.[SourceEntity] COLLATE Latin1_General_100_BIN2,d.[SourcePath] COLLATE Latin1_General_100_BIN2,d.[IsActive] EXCEPT SELECT s.[Id],s.[Key] COLLATE Latin1_General_100_BIN2,s.[DisplayName] COLLATE Latin1_General_100_BIN2,s.[DefaultHeader] COLLATE Latin1_General_100_BIN2,s.[Description] COLLATE Latin1_General_100_BIN2,s.[SourceEntity] COLLATE Latin1_General_100_BIN2,s.[SourcePath] COLLATE Latin1_General_100_BIN2,s.[IsActive]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ExportFields; seed aborted.', 1;
INSERT @Changes SELECT N'ExportFields',30,(SELECT COUNT(*) FROM #SeedExportFields s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ExportFields] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ExportFields] d JOIN #SeedExportFields s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Key] COLLATE Latin1_General_100_BIN2,d.[DisplayName] COLLATE Latin1_General_100_BIN2,d.[DefaultHeader] COLLATE Latin1_General_100_BIN2,d.[Description] COLLATE Latin1_General_100_BIN2,d.[SourceEntity] COLLATE Latin1_General_100_BIN2,d.[SourcePath] COLLATE Latin1_General_100_BIN2,d.[IsActive] EXCEPT SELECT s.[Id],s.[Key] COLLATE Latin1_General_100_BIN2,s.[DisplayName] COLLATE Latin1_General_100_BIN2,s.[DefaultHeader] COLLATE Latin1_General_100_BIN2,s.[Description] COLLATE Latin1_General_100_BIN2,s.[SourceEntity] COLLATE Latin1_General_100_BIN2,s.[SourcePath] COLLATE Latin1_General_100_BIN2,s.[IsActive]));
CREATE TABLE #SeedExportTemplates ([Id] int,[Key] nvarchar(50),[Name] nvarchar(200),[TargetSystem] nvarchar(100),[Version] nvarchar(50),[IsDefault] bit,[IsActive] bit);
INSERT INTO #SeedExportTemplates ([Id],[Key],[Name],[TargetSystem],[Version],[IsDefault],[IsActive]) VALUES
(1,N'TD',N'TODO',N'CASES',N'v1',0,1);
IF EXISTS (SELECT 1 FROM dbo.[ExportTemplates] d JOIN #SeedExportTemplates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Key] COLLATE Latin1_General_100_BIN2,d.[Name] COLLATE Latin1_General_100_BIN2,d.[TargetSystem] COLLATE Latin1_General_100_BIN2,d.[Version] COLLATE Latin1_General_100_BIN2,d.[IsDefault],d.[IsActive] EXCEPT SELECT s.[Id],s.[Key] COLLATE Latin1_General_100_BIN2,s.[Name] COLLATE Latin1_General_100_BIN2,s.[TargetSystem] COLLATE Latin1_General_100_BIN2,s.[Version] COLLATE Latin1_General_100_BIN2,s.[IsDefault],s.[IsActive]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ExportTemplates; seed aborted.', 1;
INSERT @Changes SELECT N'ExportTemplates',1,(SELECT COUNT(*) FROM #SeedExportTemplates s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ExportTemplates] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ExportTemplates] d JOIN #SeedExportTemplates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Key] COLLATE Latin1_General_100_BIN2,d.[Name] COLLATE Latin1_General_100_BIN2,d.[TargetSystem] COLLATE Latin1_General_100_BIN2,d.[Version] COLLATE Latin1_General_100_BIN2,d.[IsDefault],d.[IsActive] EXCEPT SELECT s.[Id],s.[Key] COLLATE Latin1_General_100_BIN2,s.[Name] COLLATE Latin1_General_100_BIN2,s.[TargetSystem] COLLATE Latin1_General_100_BIN2,s.[Version] COLLATE Latin1_General_100_BIN2,s.[IsDefault],s.[IsActive]));
CREATE TABLE #SeedExportTemplateColumns ([Id] int,[TemplateId] int,[ExportFieldId] int,[TargetHeader] nvarchar(200),[OrderIndex] int,[IsRequired] bit,[Format] nvarchar(100),[Separator] nvarchar(10));
INSERT INTO #SeedExportTemplateColumns ([Id],[TemplateId],[ExportFieldId],[TargetHeader],[OrderIndex],[IsRequired],[Format],[Separator]) VALUES
(1,1,6,N'CONVOCATION',1,1,NULL,NULL),
(2,1,1,N'PROJECT_CODE',2,1,NULL,NULL),
(3,1,3,N'PROJECT_NUMBER',3,1,NULL,NULL),
(4,1,2,N'PROJECT_NAME',4,1,NULL,NULL),
(5,1,13,N'FACULTY',5,1,NULL,NULL),
(6,1,12,N'EXECUTION_PERCENT',6,1,NULL,NULL),
(7,1,7,N'APPROVAL_DATE',7,1,NULL,NULL),
(8,1,8,N'START_DATE',8,1,NULL,NULL),
(9,1,10,N'TENTATIVE_END_DATE',9,1,NULL,NULL),
(10,1,11,N'REAL_END_DATE',10,1,NULL,NULL),
(11,1,4,N'PROJECT_TYPE',11,1,NULL,NULL),
(12,1,5,N'PROJECT_STATE',12,1,NULL,NULL),
(13,1,14,N'COORDINATOR_NAME',13,1,NULL,NULL),
(14,1,15,N'COORDINATOR_EMAIL',14,1,NULL,NULL),
(15,1,16,N'COORDINATOR_PHONE',15,1,NULL,NULL),
(16,1,28,N'SUBROGANT_NAME',16,1,NULL,NULL),
(17,1,29,N'SUBROGANT_EMAIL',17,1,NULL,NULL),
(18,1,30,N'SUBROGANT_PHONE',18,1,NULL,NULL),
(19,1,19,N'EXTERNAL_INSTITUTIONS',19,1,NULL,NULL),
(20,1,18,N'EXTERNAL_NAMES',20,1,NULL,NULL),
(21,1,17,N'SENESCYT_MEMBERS',21,1,NULL,NULL),
(22,1,9,N'DURATION_MONTHS',22,1,NULL,NULL),
(23,1,21,N'BUDGET_FUNDING_TYPES',23,1,NULL,NULL),
(24,1,20,N'BUDGET_INITIAL',24,1,NULL,NULL),
(25,1,22,N'BUDGET_CERTIFIED',25,1,NULL,NULL),
(26,1,22,N'BUDGET_CERTIFIED',26,1,NULL,NULL),
(27,1,26,N'OBJETIVOS_BUNDLE',27,1,NULL,NULL),
(28,1,27,N'RESEARCH_CATEGORIES_BUNDLE',28,1,NULL,NULL),
(29,1,25,N'PRODUCT_TYPES',29,1,NULL,NULL),
(30,1,24,N'PRODUCT_TITLES',30,1,NULL,NULL);
IF EXISTS (SELECT 1 FROM dbo.[ExportTemplateColumns] d JOIN #SeedExportTemplateColumns s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[TemplateId],d.[ExportFieldId],d.[TargetHeader] COLLATE Latin1_General_100_BIN2,d.[OrderIndex],d.[IsRequired],d.[Format] COLLATE Latin1_General_100_BIN2,d.[Separator] COLLATE Latin1_General_100_BIN2 EXCEPT SELECT s.[Id],s.[TemplateId],s.[ExportFieldId],s.[TargetHeader] COLLATE Latin1_General_100_BIN2,s.[OrderIndex],s.[IsRequired],s.[Format] COLLATE Latin1_General_100_BIN2,s.[Separator] COLLATE Latin1_General_100_BIN2) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ExportTemplateColumns; seed aborted.', 1;
INSERT @Changes SELECT N'ExportTemplateColumns',30,(SELECT COUNT(*) FROM #SeedExportTemplateColumns s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ExportTemplateColumns] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ExportTemplateColumns] d JOIN #SeedExportTemplateColumns s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[TemplateId],d.[ExportFieldId],d.[TargetHeader] COLLATE Latin1_General_100_BIN2,d.[OrderIndex],d.[IsRequired],d.[Format] COLLATE Latin1_General_100_BIN2,d.[Separator] COLLATE Latin1_General_100_BIN2 EXCEPT SELECT s.[Id],s.[TemplateId],s.[ExportFieldId],s.[TargetHeader] COLLATE Latin1_General_100_BIN2,s.[OrderIndex],s.[IsRequired],s.[Format] COLLATE Latin1_General_100_BIN2,s.[Separator] COLLATE Latin1_General_100_BIN2));
CREATE TABLE #SeedProductTypes ([Id] int,[Name] nvarchar(100),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProductTypes ([Id],[Name],[IsActive],[IsLocked]) VALUES
(1,N'PRODUCCIÓN CIENTÍFICA',1,0),
(2,N'PRODUCCIÓN REGIONAL',1,0),
(3,N'PONENCIAS',1,0),
(4,N'LIBROS',1,0),
(5,N'CAPÍTULO DE LIBROS',1,0),
(6,N'TRABAJO DE TITULACIÓN',1,0),
(7,N'AYUDANTE / BECARIO / PRÁCTICAS PREPROFESIONALES',1,0);
IF EXISTS (SELECT 1 FROM dbo.[ProductTypes] d JOIN #SeedProductTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT ((d.Id=1 AND NOT EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT CAST(1 AS int),CAST(N'Producto smoke' AS nvarchar(100)) COLLATE Latin1_General_100_BIN2,CAST(1 AS bit),CAST(0 AS bit))))) THROW 51002, 'Ambiguous existing data in ProductTypes; seed aborted.', 1;
IF EXISTS (SELECT 1 FROM dbo.Products p JOIN dbo.ProductTypes d ON d.Id=p.ProductTypeId JOIN #SeedProductTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND (p.Title NOT LIKE N'CT-%' OR p.ProjectId IS NOT NULL)) THROW 51003, 'Known ProductTypes fixture has non-smoke references; seed aborted.', 1;
INSERT @Changes SELECT N'ProductTypes',7,(SELECT COUNT(*) FROM #SeedProductTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProductTypes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProductTypes] d JOIN #SeedProductTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProductAttributes ([Id] int,[DataType] int,[Unit] nvarchar(32),[Name] nvarchar(200),[IsActive] bit,[IsLocked] bit);
INSERT INTO #SeedProductAttributes ([Id],[DataType],[Unit],[Name],[IsActive],[IsLocked]) VALUES
(1,0,NULL,N'TÍTULO',1,0),
(2,0,NULL,N'AUTORES',1,0),
(3,0,NULL,N'REVISTA',1,0),
(4,0,NULL,N'BASE DE DATOS',1,0),
(5,1,NULL,N'IMPACTO / SJR',1,0),
(6,0,NULL,N'CUARTIL',1,0),
(7,0,NULL,N'ISSN / ISBN',1,0),
(8,0,NULL,N'DOI',1,0),
(9,1,NULL,N'AÑO',1,0),
(10,3,NULL,N'URL A LA FECHA DE CONSULTA',1,0),
(11,0,NULL,N'NOMBRE DEL EVENTO',1,0),
(12,2,NULL,N'FECHA',1,0),
(13,0,NULL,N'PAÍS',1,0),
(14,0,NULL,N'EDITORIAL',1,0),
(15,0,NULL,N'CAPÍTULO DEL LIBRO',1,0),
(16,0,NULL,N'ESTUDIANTE',1,0),
(17,0,NULL,N'GRADO / POSGRADO',1,0),
(18,0,NULL,N'RESOLUCIÓN',1,0),
(19,0,NULL,N'ACTIVIDADES EJECUTADAS',1,0);
IF EXISTS (SELECT 1 FROM dbo.[ProductAttributes] d JOIN #SeedProductAttributes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[DataType],d.[Unit] COLLATE Latin1_General_100_BIN2,d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[DataType],s.[Unit] COLLATE Latin1_General_100_BIN2,s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ProductAttributes; seed aborted.', 1;
INSERT @Changes SELECT N'ProductAttributes',19,(SELECT COUNT(*) FROM #SeedProductAttributes s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProductAttributes] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProductAttributes] d JOIN #SeedProductAttributes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[DataType],d.[Unit] COLLATE Latin1_General_100_BIN2,d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[DataType],s.[Unit] COLLATE Latin1_General_100_BIN2,s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]));
CREATE TABLE #SeedProductAttributeDefinitions ([Id] int,[ProductTypeId] int,[ProductAttributeId] int,[IsRequired] bit,[DisplayOrder] int);
INSERT INTO #SeedProductAttributeDefinitions ([Id],[ProductTypeId],[ProductAttributeId],[IsRequired],[DisplayOrder]) VALUES
(1,1,1,0,1),
(2,1,2,0,2),
(3,1,3,0,3),
(4,1,4,0,4),
(5,1,5,0,5),
(6,1,6,0,6),
(7,1,7,0,7),
(8,1,8,0,8),
(9,1,9,0,9),
(10,1,10,0,10),
(11,2,1,0,1),
(12,2,2,0,2),
(13,2,3,0,3),
(14,2,4,0,4),
(15,2,7,0,5),
(16,2,10,0,6),
(17,3,1,0,1),
(18,3,11,0,2),
(19,3,12,0,3),
(20,3,13,0,4),
(21,4,1,0,1),
(22,4,2,0,2),
(23,4,9,0,3),
(24,4,13,0,4),
(25,4,14,0,5),
(26,4,7,0,6),
(27,5,1,0,1),
(28,5,15,0,2),
(29,5,2,0,3),
(30,5,9,0,4),
(31,5,13,0,5),
(32,5,14,0,6),
(33,5,7,0,7),
(34,6,16,0,1),
(35,6,17,0,2),
(36,6,18,0,3),
(37,7,16,0,1),
(38,7,19,0,2),
(39,7,18,0,3);
IF EXISTS (SELECT 1 FROM dbo.[ProductAttributeDefinitions] d JOIN #SeedProductAttributeDefinitions s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[ProductTypeId],d.[ProductAttributeId],d.[IsRequired],d.[DisplayOrder] EXCEPT SELECT s.[Id],s.[ProductTypeId],s.[ProductAttributeId],s.[IsRequired],s.[DisplayOrder]) AND NOT (1=0)) THROW 51002, 'Ambiguous existing data in ProductAttributeDefinitions; seed aborted.', 1;
INSERT @Changes SELECT N'ProductAttributeDefinitions',39,(SELECT COUNT(*) FROM #SeedProductAttributeDefinitions s WHERE NOT EXISTS(SELECT 1 FROM dbo.[ProductAttributeDefinitions] d WHERE d.Id=s.Id)),(SELECT COUNT(*) FROM dbo.[ProductAttributeDefinitions] d JOIN #SeedProductAttributeDefinitions s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[ProductTypeId],d.[ProductAttributeId],d.[IsRequired],d.[DisplayOrder] EXCEPT SELECT s.[Id],s.[ProductTypeId],s.[ProductAttributeId],s.[IsRequired],s.[DisplayOrder]));
IF @Apply=1
BEGIN
SET IDENTITY_INSERT dbo.[DocumentTypes] ON;
INSERT dbo.[DocumentTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedDocumentTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[DocumentTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[DocumentTypes] OFF;
UPDATE d SET [Name]=s.[Name],[IsActive]=s.[IsActive],[IsLocked]=s.[IsLocked] FROM dbo.[GroupTypes] d JOIN #SeedGroupTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[GroupTypes] ON;
INSERT dbo.[GroupTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedGroupTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[GroupTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[GroupTypes] OFF;
UPDATE d SET [Name]=s.[Name],[Code]=s.[Code],[IsActive]=s.[IsActive],[IsLocked]=s.[IsLocked] FROM dbo.[Convocations] d JOIN #SeedConvocations s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[Code] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[Code] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[Convocations] ON;
INSERT dbo.[Convocations] ([Id],[Name],[Code],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[Code],s.[IsActive],s.[IsLocked] FROM #SeedConvocations s WHERE NOT EXISTS (SELECT 1 FROM dbo.[Convocations] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[Convocations] OFF;
UPDATE d SET [Name]=s.[Name],[IsActive]=s.[IsActive],[IsLocked]=s.[IsLocked] FROM dbo.[ProjectStates] d JOIN #SeedProjectStates s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[ProjectStates] ON;
INSERT dbo.[ProjectStates] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProjectStates s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProjectStates] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProjectStates] OFF;
UPDATE d SET [Name]=s.[Name],[IsActive]=s.[IsActive],[IsLocked]=s.[IsLocked] FROM dbo.[ProjectTypes] d JOIN #SeedProjectTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[ProjectTypes] ON;
INSERT dbo.[ProjectTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProjectTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProjectTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProjectTypes] OFF;
SET IDENTITY_INSERT dbo.[TransactionTypes] ON;
INSERT dbo.[TransactionTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedTransactionTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[TransactionTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[TransactionTypes] OFF;
SET IDENTITY_INSERT dbo.[VisitStates] ON;
INSERT dbo.[VisitStates] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedVisitStates s WHERE NOT EXISTS (SELECT 1 FROM dbo.[VisitStates] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[VisitStates] OFF;
SET IDENTITY_INSERT dbo.[ObjectiveTypes] ON;
INSERT dbo.[ObjectiveTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedObjectiveTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ObjectiveTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ObjectiveTypes] OFF;
SET IDENTITY_INSERT dbo.[ResearchCategoryGroups] ON;
INSERT dbo.[ResearchCategoryGroups] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedResearchCategoryGroups s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ResearchCategoryGroups] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ResearchCategoryGroups] OFF;
SET IDENTITY_INSERT dbo.[ResearchCategoryTypes] ON;
INSERT dbo.[ResearchCategoryTypes] ([Id],[Name],[ResearchCategoryGroupId],[IsActive],[IsFilterEnabled],[IsLocked]) SELECT s.[Id],s.[Name],s.[ResearchCategoryGroupId],s.[IsActive],s.[IsFilterEnabled],s.[IsLocked] FROM #SeedResearchCategoryTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ResearchCategoryTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ResearchCategoryTypes] OFF;
SET IDENTITY_INSERT dbo.[ResearchCategories] ON;
INSERT dbo.[ResearchCategories] ([Id],[ResearchCategoryTypeId],[ParentCategoryId],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[ResearchCategoryTypeId],s.[ParentCategoryId],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedResearchCategories s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ResearchCategories] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ResearchCategories] OFF;
SET IDENTITY_INSERT dbo.[FundingTypes] ON;
INSERT dbo.[FundingTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedFundingTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[FundingTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[FundingTypes] OFF;
UPDATE d SET [Name]=s.[Name],[IsActive]=s.[IsActive],[Flag]=s.[Flag],[IsLocked]=s.[IsLocked] FROM dbo.[MemberRoleTypes] d JOIN #SeedMemberRoleTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[Flag],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[Flag],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[MemberRoleTypes] ON;
INSERT dbo.[MemberRoleTypes] ([Id],[Name],[IsActive],[Flag],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[Flag],s.[IsLocked] FROM #SeedMemberRoleTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[MemberRoleTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[MemberRoleTypes] OFF;
SET IDENTITY_INSERT dbo.[Countries] ON;
INSERT dbo.[Countries] ([Id],[Name],[IsoCode],[IsoAlpha3],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsoCode],s.[IsoAlpha3],s.[IsActive],s.[IsLocked] FROM #SeedCountries s WHERE NOT EXISTS (SELECT 1 FROM dbo.[Countries] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[Countries] OFF;
SET IDENTITY_INSERT dbo.[Institutions] ON;
INSERT dbo.[Institutions] ([Id],[Name],[CountryId],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[CountryId],s.[IsActive],s.[IsLocked] FROM #SeedInstitutions s WHERE NOT EXISTS (SELECT 1 FROM dbo.[Institutions] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[Institutions] OFF;
SET IDENTITY_INSERT dbo.[ProjectExtensionTypes] ON;
INSERT dbo.[ProjectExtensionTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProjectExtensionTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProjectExtensionTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProjectExtensionTypes] OFF;
SET IDENTITY_INSERT dbo.[ProjectOriginTypes] ON;
INSERT dbo.[ProjectOriginTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProjectOriginTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProjectOriginTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProjectOriginTypes] OFF;
SET IDENTITY_INSERT dbo.[ExportFields] ON;
INSERT dbo.[ExportFields] ([Id],[Key],[DisplayName],[DefaultHeader],[Description],[SourceEntity],[SourcePath],[IsActive]) SELECT s.[Id],s.[Key],s.[DisplayName],s.[DefaultHeader],s.[Description],s.[SourceEntity],s.[SourcePath],s.[IsActive] FROM #SeedExportFields s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ExportFields] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ExportFields] OFF;
SET IDENTITY_INSERT dbo.[ExportTemplates] ON;
INSERT dbo.[ExportTemplates] ([Id],[Key],[Name],[TargetSystem],[Version],[IsDefault],[IsActive]) SELECT s.[Id],s.[Key],s.[Name],s.[TargetSystem],s.[Version],s.[IsDefault],s.[IsActive] FROM #SeedExportTemplates s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ExportTemplates] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ExportTemplates] OFF;
SET IDENTITY_INSERT dbo.[ExportTemplateColumns] ON;
INSERT dbo.[ExportTemplateColumns] ([Id],[TemplateId],[ExportFieldId],[TargetHeader],[OrderIndex],[IsRequired],[Format],[Separator]) SELECT s.[Id],s.[TemplateId],s.[ExportFieldId],s.[TargetHeader],s.[OrderIndex],s.[IsRequired],s.[Format],s.[Separator] FROM #SeedExportTemplateColumns s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ExportTemplateColumns] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ExportTemplateColumns] OFF;
UPDATE d SET [Name]=s.[Name],[IsActive]=s.[IsActive],[IsLocked]=s.[IsLocked] FROM dbo.[ProductTypes] d JOIN #SeedProductTypes s ON s.Id=d.Id WHERE EXISTS (SELECT d.[Id],d.[Name] COLLATE Latin1_General_100_BIN2,d.[IsActive],d.[IsLocked] EXCEPT SELECT s.[Id],s.[Name] COLLATE Latin1_General_100_BIN2,s.[IsActive],s.[IsLocked]);
SET IDENTITY_INSERT dbo.[ProductTypes] ON;
INSERT dbo.[ProductTypes] ([Id],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProductTypes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProductTypes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProductTypes] OFF;
SET IDENTITY_INSERT dbo.[ProductAttributes] ON;
INSERT dbo.[ProductAttributes] ([Id],[DataType],[Unit],[Name],[IsActive],[IsLocked]) SELECT s.[Id],s.[DataType],s.[Unit],s.[Name],s.[IsActive],s.[IsLocked] FROM #SeedProductAttributes s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProductAttributes] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProductAttributes] OFF;
SET IDENTITY_INSERT dbo.[ProductAttributeDefinitions] ON;
INSERT dbo.[ProductAttributeDefinitions] ([Id],[ProductTypeId],[ProductAttributeId],[IsRequired],[DisplayOrder]) SELECT s.[Id],s.[ProductTypeId],s.[ProductAttributeId],s.[IsRequired],s.[DisplayOrder] FROM #SeedProductAttributeDefinitions s WHERE NOT EXISTS (SELECT 1 FROM dbo.[ProductAttributeDefinitions] d WHERE d.Id=s.Id);
SET IDENTITY_INSERT dbo.[ProductAttributeDefinitions] OFF;
END;
COMMIT TRANSACTION;
SELECT @Actor AS ActorAppUserId, @Apply AS Applied;
SELECT * FROM @Changes ORDER BY TableName;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW;
END CATCH;
