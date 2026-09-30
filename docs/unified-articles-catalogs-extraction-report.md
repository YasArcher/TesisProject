# Unified Articles Initial Catalog Extraction

## Source backup

- Archivo: `tesis_unified_20260929_183922.bak`.
- Instancia: `Archer\DINNOVA`, SQL Server `16.0.1200.5`.
- Base temporal de solo lectura conceptual: `tesis_unified_articles_seed_source_20260929`.
- Archivos lógicos: `tesis_unified` y `tesis_unified_log`.
- El backup solo se usó para extracción y validación. No es una dependencia de ejecución.
- No se restauró ni se modificó `tesis_unified`.

## Existing seed infrastructure

La implementación reutiliza `IDataMigration`, el registro por DI, `DataMigrationService`,
`OperationExecutionHistory`, la transacción existente y el controlador administrativo genérico.
No se creó otro endpoint, controlador, servicio de seed ni mecanismo `HasData`.

La referencia existente `PROJECTS_INITIAL_CATALOG_V1` tiene `Version = "1"` y `Order = 100`.
La nueva migración usa `Version = "1"` y `Order = 200`.

## Tables inspected

Se inventariaron todas las tablas `dbo` del backup. Las tablas materialmente relacionadas con
Articles/Product se clasificaron así:

| Tabla | Filas backup | PK / relaciones principales | Clasificación | Decisión |
|---|---:|---|---|---|
| `ProductTypes` | 7 | `Id`; referenciada por `Products` y `ProductAttributeDefinitions` | `ALREADY_SEEDED` / `SHARED_INITIAL_CATALOG` | Cubierta por Projects |
| `ProductAttributes` | 19 | `Id`; referenciada por `ProductAttributeDefinitions` | `ALREADY_SEEDED` / `SHARED_INITIAL_CATALOG` | Cubierta por Projects |
| `ProductAttributeDefinitions` | 43 | `Id`; FK a tipos y atributos | Mixta | 39 cubiertas; 4 incompatibles excluidas |
| `PublicationStatuses` | 4 | `PublicationStatusId`; nombre único; FK desde `Articles` | `ARTICLE_INITIAL_CATALOG` + QA/DW | 3 aceptadas |
| `ResearchLines` | 26 | `ResearchLineId`; nombre único; FK desde `Articles` | `ARTICLE_INITIAL_CATALOG` + QA/`UNCERTAIN` | 16 aceptadas |
| `BroadFields` | 19 | `BroadFieldId`; nombre único | `ARTICLE_INITIAL_CATALOG` + QA/`UNCERTAIN` | 9 aceptadas |
| `SpecificFields` | 39 | `SpecificFieldId`; FK a `BroadFields`; claves únicas por padre y código/nombre | `ARTICLE_INITIAL_CATALOG` + QA/`UNCERTAIN` | 25 aceptadas |
| `DetailedFields` | 104 | `DetailedFieldId`; FK a `SpecificFields`; claves únicas por padre y código/nombre | `ARTICLE_INITIAL_CATALOG` + QA/`UNCERTAIN` | 90 aceptadas |
| `IndexingSources` | 8 | `Id`; FK desde `ArticleIndexings` | `ARTICLE_INITIAL_CATALOG` | 8 aceptadas |
| `Faculties` | 9 | `FacultyId`; FK desde `Articles` | `SYNC_MANAGED` | Excluida |
| `AcademicTerms` | 8 | `AcademicTermId`; FK desde `Articles` | `SYNC_MANAGED` | Excluida |
| `Articles` | 1 | `Id`; FK a Product y catálogos | `QA_TEST` / `TRANSACTIONAL` | Excluida |
| `Products` | 1 | `Id`; FK a `ProductTypes` | `QA_TEST` / `TRANSACTIONAL` | Excluida |
| `Authors` | 1 | `AuthorId` | `QA_TEST` / `USER_GENERATED` | Excluida |
| `ProductAuthors` | 1 | `Id`; FK a Product y Author | `QA_TEST` / `TRANSACTIONAL` | Excluida |
| `ProductValues` | 8 | `Id`; FK a Product y definición | `QA_TEST` / `TRANSACTIONAL` | Excluida |
| `ArticleIndexings` | 1 | FK a Article e `IndexingSources` | `QA_TEST` / `TRANSACTIONAL` | Excluida |
| `Venues` | 1 | `VenueId`; FK desde `Articles` | `QA_TEST` / `USER_GENERATED` | Excluida |
| `ExternalResearchers` | 1 | `ExternalResearcherId` | `QA_TEST` / `USER_GENERATED` | Excluida |
| `RegistrationMatrix` | 1 | `RegistrationMatrixId`; creada por usuario | `BULK_IMPORT` / `OPERATIONAL` | Excluida |
| `RegistrationMatrixColumn` | 15 | FK a matriz y `FieldCatalog` | `BULK_IMPORT` / `OPERATIONAL` | Excluida |
| `RegistrationMatrixRow` / `RegistrationMatrixCell` | 0 / 0 | FKs de importación | `BULK_IMPORT` | Excluida |
| `FieldCatalog` | 54 | `FieldId`; catálogo base de campos físicos, dinámicos y compuestos | Mixta | 40 aceptadas; 14 temporales/externas excluidas |
| `FormDefinitions` / `FormFields` | 2 / 23 | formularios y asignación ordenada de campos | `ARTICLE_FORM_CONFIGURATION` | 2 / 23 aceptadas |
| `OperationExecutionHistory` | 1 | `ExecutionId` | `SYSTEM` / `ETL` | Excluida |
| tablas `AspNet*`, `AppUsers`, `RefreshTokens` | varias | Identity | `IDENTITY` | Excluidas |

Las tablas DW no están en el conjunto operacional `dbo` sembrado. `ProjectsDW.*` y
`ArticlesDW.*` se crean mediante sus migraciones y se alimentan por ETL.

## Articles initial catalogs detected

| Catálogo | Filas aceptadas | Rango canónico |
|---|---:|---|
| `PublicationStatuses` | 3 | IDs 1-3 |
| `ResearchLines` | 16 | IDs 1-16 |
| `BroadFields` | 9 | IDs 1-9 |
| `SpecificFields` | 25 | IDs 1-25 |
| `DetailedFields` | 90 | IDs 1-90 |
| `IndexingSources` | 8 | IDs 1-8 |
| `FieldCatalog` | 40 | IDs 1-32, 34-40 y 1036 |
| `FormDefinitions` | 2 | IDs 1-2 |
| `FormFields` | 23 | asignaciones canónicas del backup |
| **Total nuevo** | **216** | |

Se conservaron IDs, nombres Unicode, códigos, `null`, booleanos, relaciones y orden padre-hijo.

## Shared catalogs already covered

`PROJECTS_INITIAL_CATALOG_V1` ya contiene:

- 7 `ProductTypes`;
- 19 `ProductAttributes`;
- 39 `ProductAttributeDefinitions` canónicas.

`UNIFIED_ARTICLES_CATALOGS_V1` no duplica esas filas. Valida explícitamente:

- ProductType 1: `PRODUCCIÓN CIENTÍFICA`;
- ProductType 2: `PRODUCCIÓN REGIONAL`;
- atributos 1-10;
- definiciones 1-10 para tipo 1;
- definiciones 11-16 para tipo 2, que corresponden solo a atributos 1, 2, 3, 4, 7 y 10.

La migración falla antes de insertar si la dependencia compartida falta, difiere o contiene
definiciones adicionales incompatibles para los tipos 1 o 2.

Matriz de comparación con seeds existentes:

| TABLE | BACKUP_ROWS | EXISTING_SEED_ROWS | ARTICLE_ROWS_REQUIRED | ALREADY_COVERED | NEW_ROWS | EXCLUDED_ROWS | REASON |
|---|---:|---:|---:|---:|---:|---:|---|
| `ProductTypes` | 7 | 7 | 2 | 2 | 0 | 0 | Dependencia compartida |
| `ProductAttributes` | 19 | 19 | 10 | 10 | 0 | 0 | Dependencia compartida |
| `ProductAttributeDefinitions` | 43 | 39 | 16 | 16 | 0 | 4 | Incompatibles con ProductType 2 |
| `PublicationStatuses` | 4 | 0 | 3 | 0 | 3 | 1 | `DW Publicado` excluido |
| `ResearchLines` | 26 | 0 | 16 | 0 | 16 | 10 | 5 DW y 5 ambiguas |
| `BroadFields` | 19 | 0 | 9 | 0 | 9 | 10 | 5 DW y 5 ambiguas |
| `SpecificFields` | 39 | 0 | 25 | 0 | 25 | 14 | 5 DW y 9 ambiguas |
| `DetailedFields` | 104 | 0 | 90 | 0 | 90 | 14 | 5 DW y 9 ambiguas |
| `IndexingSources` | 8 | 0 | 8 | 0 | 8 | 0 | Catálogo inicial |
| `FieldCatalog` | 54 | 0 | 40 | 0 | 40 | 14 | Se excluye campo temporal 33 y metadatos externos 1037-1049 |
| `FormDefinitions` | 2 | 0 | 2 | 0 | 2 | 0 | Formularios iniciales de artículo y participantes |
| `FormFields` | 23 | 0 | 23 | 0 | 23 | 0 | Composición, orden y reglas de ambos formularios |

## New catalog rows

La fuente canónica quedó en C# tipado dentro de `ArticlesCatalogSeedData` y
`ArticlesFormSeedData`.
La inserción conserva identidad mediante el mismo patrón `SqlBulkCopy` transaccional usado por
la migración existente. Conflictos por ID o business key producen `CATALOG_ID_CONFLICT`; no se
sobrescriben ni duplican filas.

## Sync-managed exclusions

- `Faculties`: 9 filas. Responsabilidad de Faculty Sync.
- `AcademicTerms`: 8 filas. Responsabilidad de AcademicTerm Sync.
- `AcademicTerms` incluía `DW Demo 2024-B`, además de periodos operacionales.

## Operational-data exclusions

Se excluyeron Articles, Products, Authors, ProductAuthors, ProductValues, ArticleIndexings,
Venues, ExternalResearchers, matrices de registro, archivos, proyectos,
presupuestos, historial de operaciones y datos ETL.

La matriz del backup fue creada a las `2026-09-29 23:36:50` y su nombre incorpora la fecha de
ejecución. Es un artefacto de operación, no catálogo inicial.

## QA/test exclusions

- `PublicationStatuses`: ID 10, `DW Publicado`.
- `ResearchLines`: IDs 17-21, nombres con prefijo `DW`.
- Jerarquía OECD: IDs 1001-1005 en cada nivel, nombres/códigos con prefijo `DW`.
- Article, Product, Venue y ExternalResearcher de prueba del flujo ejecutado el 2026-09-29.
- `FieldCatalog` ID 33, `TempConfigField`.

Los scripts QA actuales también crean marcadores `QA-DW-*`; ninguno se incorporó.

## Ambiguous records

Se excluyeron por `UNCERTAIN`, sin incorporación automática:

- `ResearchLines` IDs 22-24, 27-28;
- `BroadFields` IDs 1006-1008, 1011-1012;
- `SpecificFields` IDs 1006-1008, 1012-1017;
- `DetailedFields` IDs 1006-1008, 1012-1017;
- metadatos externos invisibles de `FieldCatalog`, IDs 1037-1049 de Scopus, cuya descripción
  indica que no alteran el formulario de registro.

Estos registros forman grupos de IDs altos, no pertenecen al bloque canónico continuo y no tienen
una fuente normativa actual que permita declararlos inequívocamente como catálogo inicial.

## ID conflicts

No hubo conflicto de ID entre las 216 filas aceptadas y el modelo actual.

Las filas `ProductAttributeDefinitions` 40-43 del backup asignaban al ProductType 2 los atributos
5, 6, 8 y 9. Se excluyeron por incompatibilidad estructural con la definición canónica actual.
No se reasignaron IDs.

## Resulting Data Migration

- Clase: `UnifiedArticlesCatalogsV1`.
- OperationCode: `UNIFIED_ARTICLES_CATALOGS_V1`.
- OperationType: `DATA_MIGRATION` mediante la infraestructura existente.
- Version: `1`.
- Order: `200`.
- Endpoint existente: `GET /api/admin/data-migrations`.
- Aplicación existente: `POST /api/admin/data-migrations/UNIFIED_ARTICLES_CATALOGS_V1/apply`.
- Seguridad conservada: autenticación, rol `superadmin`, `AdministrativeOperations__Enabled` y
  `X-Admin-Operation-Secret`.

## Execution order

1. Migraciones EF de Unified, ProjectsDW y ArticlesDW.
2. `PROJECTS_INITIAL_CATALOG_V1` (`Order = 100`).
3. `UNIFIED_ARTICLES_CATALOGS_V1` (`Order = 200`).
4. ETL de DW cuando corresponda; la nueva migración no escribe en los esquemas DW.

Orden interno de `UNIFIED_ARTICLES_CATALOGS_V1`:

1. `PublicationStatuses`;
2. `ResearchLines`;
3. `BroadFields`;
4. `SpecificFields`;
5. `DetailedFields`;
6. `IndexingSources`.

## Validation result

Base limpia final: `tesis_unified_articles_forms_validation_v2`, creada desde estado inexistente.

- Unified EF migrations: 2 aplicadas, 0 pendientes.
- ProjectsDW EF migrations: 2 aplicadas, 0 pendientes.
- ArticlesDW EF migrations: 2 aplicadas, 0 pendientes.
- Registro genérico: ambas Data Migrations disponibles y ordenadas.
- `PROJECTS_INITIAL_CATALOG_V1`: `SUCCEEDED`, una ejecución exitosa.
- `UNIFIED_ARTICLES_CATALOGS_V1`: `SUCCEEDED`, una ejecución exitosa.
- Segunda aplicación: `DATA_MIGRATION_ALREADY_APPLIED`; no duplicó registros.
- Resultado compacto: 216 insertadas, conteo por catálogo/configuración, sin filas completas ni secretos.
- Comparación binaria por ID, nombre, código, campos funcionales y FKs: 0 diferencias en cada catálogo.
- Comparación completa de `FieldCatalog` (40 aceptadas), `FormDefinitions` (2) y `FormFields` (23): 0 diferencias.
- `ARTICLES_INITIAL_CATALOG_DATA_DIFF = 0`.
- Prueba runtime: 48 verificaciones aprobadas.
- Prueba deployment: 26 verificaciones aprobadas, incluidas las tres cadenas EF y ambas Data Migrations.
- Unified model tests: 127 verificaciones aprobadas.
- Unified services tests: 14.548 aserciones aprobadas.
- Cutover/DI smoke: 826 verificaciones aprobadas.
- `dotnet restore TesisProject.sln`: aprobado.
- Shared Release, Backend Release, frontend principal Release y Solution Release: aprobados, 0 errores.
- Frontend separado `ArticlesMigration`: restore aprobado; build no ejecutable por errores preexistentes de DTOs ausentes
  (`ReportingSummaryItemDto`, `FieldCatalogItemDto`, `WorkflowInboxItemDto` y relacionados). No se modificó frontend.
- `git diff --check`: aprobado.

La base temporal de validación puede eliminarse después de la revisión. La base fuente se conserva
para trazabilidad de esta extracción y no participa en ejecución.
