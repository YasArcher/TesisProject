# Articles configuration: prepared Unified implementation

`IUnifiedArticleConfigurationService` / `UnifiedArticleConfigurationService` implements the existing
21 configuration/catalog operations. `IUnifiedArticleConfigurationRepository` /
`UnifiedArticleConfigurationRepository` owns Unified persistence, catalog queries and bulk updates.
Mutations use `IUnifiedUnitOfWork`, including form deactivation and the subsequent save in one transaction.

Both existing controllers are now HTTP-only and depend on `IArticleConfigurationService`.
`ArticleConfigurationService` retains the extracted legacy implementation and ArticlesDbContext;
the current DI binding continues to use that service. This extraction does not change the database
behind the existing routes. The new Unified service/repository uses only Unified persistence.

`UnifiedArticlesConfigurationController` and `UnifiedArticlesCatalogsController` preserve the DTOs,
method signatures, authorization, routes and ServiceResult/status conversion, but are `[NonController]`.
They are not activated or registered as MVC controllers. The Unified service and repository are
available through `AddUnifiedDide` for isolated testing and the later explicit cutover.

## Behavior

- Forms, fields, options and field/form assignments retain the existing CRUD contracts, validations
  and normalization. Form activation uses repository-owned ExecuteUpdateAsync; a failed save rolls
  back deactivation too. Tracked forms are synchronized after bulk updates.
- `GetResolvedActiveForm` delegates selection exclusively to `IUnifiedArticleFormSelector`.
  `GetResolvedForm(formKey)` retains the explicit named-form preview, including inactive forms; it
  does not choose an effective form. Both use the same resolved-form builder.
- Resolved sections, options and ordering are preserved. Effective visibility/editability respects
  both field and assignment flags, matching registration's existing filtering.
- Dynamic Article fields cannot impersonate base Product attributes (including known aliases).
  The guard also prevents relabeling an additional dynamic field as a base attribute. Existing
  conflicting dynamic descriptors cannot be assigned or resolved as a second value source.
- Existing non-dynamic base descriptors are validated against actual ProductAttributeDefinitions
  for Article types 1 and 2. Resolved value descriptors point to ArticleReadView, backed by
  ProductValues; title points to Products and authors to ProductAuthors. Missing definitions fail
  explicitly. Configuration never writes ProductValues, Article values or participant values.
- Administrative catalogs preserve their DTO shape. Unified IndexingSource uses Id; Faculty's
  display Code uses Acronym, not ExternalFacultyId. Unknown catalog keys and the currently unused
  parentId parameter retain their existing behavior.
- Form deletion explicitly removes its assignments; unreferenced field deletion removes its
  options within the transaction. System fields, assigned fields and fields with saved values
  remain protected; SQL FK conflicts return ServiceResult instead of leaking an EF exception.

## Verification

`dotnet run --project tests/tesisproject.articleconfigurationtests -c Release`

The tests use a GUID-named temporary SQL Server database on `.\DINNOVA`, run the existing migration
chain and delete only that fixture. They cover CRUD, assignments, deterministic selection, resolved
forms, administrative catalogs, canonical guards, activation rollback, controller discovery,
contract parity and absence of direct DbContext injection in controllers/Unified service.

No schema migration or application database update is required. Registration/write path, matrices,
DW and frontend are unchanged. No controller cutover is performed.
