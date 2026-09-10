# Articles remaining persistence coverage

Scope: active `Services/Implementations/RegistrationMatrixService.cs` and
`ArticleUserContext.cs`, with the matrix controller checked only as the producer of its owner key.
No service/controller migration, cutover or schema migration is included.

Discovery used Codebase Memory followed by the current source for metadata-changed paths.
The bounded final check of active backend Articles context/entity references found the matrix
service as the remaining direct EF consumer beyond the already approved read, registration and
configuration flows. ArticleUserContext consumes a repository, not a DbContext. The module's
disabled service has no persistence. ArticlesMigration is excluded from compilation and this scope;
DW, frontend and legacy Projects were excluded.

## Remaining accesses and their Unified destinations

| Active legacy access | Unified persistence available to the future service |
| --- | --- |
| GetMatricesAsync: owner predicate, recency, Take, column/row counts, status and LastImportBatchId | `ArticleRegistrationMatrices.Query()` inherited from GenericRepository; Where/OrderBy/Take/Select stay in SQL |
| FindMatrixAsync/GetMatrixAsync: columns→Field, ordered rows→Cells, ID/owner predicate, optional tracking | `ArticleRegistrationMatrices.QueryWithDetails(asNoTracking)`; callers compose data predicates before execution |
| LoadEligibleFieldsAsync: selected IDs, active/visible flags, Article/ArticleParticipant metadata | `ArticleFields.GetAllAsync(predicate)` or `Query()`; requested order and eligibility decisions remain in the future service |
| CreateMatrixAsync: add matrix + columns | inherited `ArticleRegistrationMatrices.AddAsync` + UoW.SaveChangesAsync |
| AddRowAsync: row insert, matrix timestamp | tracked aggregate or `RegistrationMatrixRows.AddAsync` + UoW save |
| UpdateCellAsync: insert/update/remove RawValue, row/matrix timestamps | generic `RegistrationMatrixCells` plus tracked row/matrix; no cleaning or field-membership policy in the repository |
| Column/row standard CRUD | generic `RegistrationMatrixColumns` / `RegistrationMatrixRows`; remove dependent cells first when deleting a populated row |
| DeleteMatrixAsync | full graph query, then `RemoveGraph`, then UoW save; explicit dependent deletes are required by Unified's global NoAction FKs |
| SubmitToStagingAsync | existing aggregate query covers its only data read; active legacy currently returns STAGING_PENDING and performs no staging write |
| ArticleUserContext.GetAppUserIdAsync | existing `AppUsers.GetByLocalIdAsync(identityId)` returns AppUser.IdUser; missing mapping stays null |

Status is stored as data. Draft checks, permission decisions, include-all handling, owner normalization,
row-number allocation, width defaults, input trimming, DTO projection policy and staging activation
belong to the future service. The new repositories contain none of these decisions or ServiceResult.

## Model mapping

- Matrix/column/row/cell map to the existing Unified RegistrationMatrix graph. They are draft input,
  not an additional authoritative publication store.
- `CreatedByUserId` remains the existing nullable string owner reference. The active producer supplies
  NameIdentifier or Name. It is not silently converted to AppUser.IdUser or assigned a new identity FK.
  Queries compare that stored value; authorization stays outside persistence.
- `FieldCatalogEntry.EntityName = ArticleParticipant` remains configuration metadata. A submitted
  participant belongs to Author/ProductAuthor and ProductAuthorDynamicFieldValue, never a recreated
  ArticleParticipant entity. Existing ProductAuthors/Authors repositories already cover that model.
- DOI/Year/PublicationUrl belong to ProductValues and ArticleReadView. The approved read/write layer
  is reused; matrix RawValue remains unsubmitted input.
- Institutional identity is the existing Identity→AppUser mapping. No parallel identity repository
  or provisioning logic was introduced.

## Repository and UoW integration

Only `IUnifiedArticleRegistrationMatrixRepository` is a new domain specialization. It inherits all
standard CRUD from the shared GenericRepository and adds full-graph querying and staged graph removal.
It neither saves nor starts transactions. Its read query uses one SQL statement and is composable.

Rows, cells, columns and field metadata use `IGenericRepository<T>`. `UnifiedGenericRepository<T>`
only supplies a UnifiedDideDbContext constructor to the existing GenericRepository implementation;
it introduces no CRUD behavior or entity-specific interfaces. This follows the constructor adaptation
used by the existing Unified repositories without invoking GenericRepository's legacy AppDbContext
constructor. DI registers only the four required closed generic Unified entity types.

Explicit UoW properties and constructor parameters added:

- `ArticleRegistrationMatrices` (specialized)
- `RegistrationMatrixColumns`, `RegistrationMatrixRows`, `RegistrationMatrixCells`, `ArticleFields` (generic)
- `ArticleReads`, `ArticleRegistration`, `ArticleConfiguration` (existing approved repositories, now also exposed by UoW)

All share the scoped UnifiedDideDbContext. AddUnifiedDide registers the matrix specialization and the
four generic constructor adapters. No service locator, dictionary or per-entity CRUD repositories.
Approved service constructors and behaviors remain unchanged.

## Verification and remaining work

`dotnet run --project tests/tesisproject.articlepersistencetests -c Release`

17 SQL/EF checks run on a uniquely named temporary database on `.\DINNOVA`, deleted in finally:
existing migration chain/model agreement, DI/UoW scope, generic reuse, eligible-field filtering,
no implicit saves, owner/status composition, graph loading and tracking, summary counts/pagination,
CRUD, AppUser lookup, explicit graph deletion and transaction rollback. The constructor check covers
all Unified repository implementations; a throwing legacy-context registration also verifies these
dependencies never resolve AppDbContext in the mixed DI container.

No application database was migrated. `RegistrationMatrixService` and `ArticleUserContext` remain
pending migration, with all their active persistence needs mapped above. Staging/workflow is still
functionally disabled in active legacy; its future activation is outside this persistence phase.
There is no blocking persistence gap for the active scope.
