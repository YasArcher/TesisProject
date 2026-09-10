# Unified Article registration

`IUnifiedArticleRegistrationCommandService` is registered only in `AddUnifiedDide`.
The existing HTTP registration controller and legacy service binding are unchanged.
The new service uses Unified repositories and `IUnifiedUnitOfWork`; it has no DbContext dependency.

## Contract for the Unified caller

- `Article.ProductTypeId` must explicitly be 1 (ScientificProduction) or 2 (RegionalProduction).
- `Article.ProjectId` is nullable. A concrete ID is the source of truth even if legacy
  `IsProjectResult` is false. Legacy true with no ID is rejected rather than inventing a project.
- `Article.IndexingDatabase` supplies base attribute 4.
- Institutional selection uses the directory person `id_usuario` in `InstitutionalPersonId`
  plus identification or email. The directory must return exactly that person and an `ASP_ID`;
  supplied document/email must agree. Only the returned ASP_ID goes to the existing Unified
  identity provisioning boundary. The integer is never cast to IdAsp, IdUser or IdLocal.
  Callers holding only an unverified legacy integer must resolve the selection first.
- External selection requires `Participants[].ExternalResearcherId` for an existing researcher.
  Institutional and external selections are mutually exclusive. There is no fuzzy email/name matching.
- `ExternalAuthorId` is optional legacy metadata on Author, never an identity lookup key.
- Optional `Files` contains file metadata, not uploads. `IndexingSourceIds` represents structural links.
- Response preserves `ArticleId` and `ParticipantIds`, adds `ProductId`; participant IDs now refer
  to `ProductAuthor.Id`, returned in AuthorOrder. They are not legacy ArticleParticipant IDs.

## Persistence

Title is stored in Product. Journal, IndexingDatabase, Sjr, Quartile, IssnIsbn, Doi, Year and
ConsultationUrl use typed base attribute IDs 3–10 and the actual definitions for the selected
product type. No physical definition IDs are assumed. Missing supplied/required definitions fail.
Required additional Product attributes unsupported by this contract fail explicitly.

Article retains the submitted structural fields, files and indexings. Explicit FacultyId remains
independent of Project.FacultyId. VenueId references an existing venue. Existing venue structural
metadata must agree if also submitted. A new structural Venue requires JournalName and Type;
issue/volume/journal URL are preserved. Its catalog Name is descriptive: the Article read model
always reads Journal from ProductValues. ISSN/SJR/Quartile/Year are not copied to Venue/VenueMetrics.
VenueMetric.Year, if supplied, must match Article.Year; it is not persisted separately.

Author is AppUser XOR ExternalResearcher. Existing Authors are reused and contradictory ORCID
is rejected. Snapshots, AuthorOrder, Participation and IsPrimaryAuthor retain independent values.
No relationship between primary status, participation and first position is invented.

Participant dynamic values are resolved by FieldId/FieldKey, validated and saved in
ProductAuthorDynamicFieldValues. Article extra fields use DynamicFieldValues. Known base fields
are validated from their canonical properties and rejected in the dynamic payload. Ambiguous IDs,
conflicting keys, duplicates, fields outside the effective form and multiple typed values fail.

`IUnifiedArticleFormSelector` is the sole selector for Unified form reads and registration validation:
active entity forms, exact trimmed key first, normalized alphanumeric key second, then fallback;
within each group, UpdatedAt-or-CreatedAt descending and FormId descending. The legacy configuration
and matrix algorithms have not been migrated or rebound in this phase.

Identity provisioning uses the existing Unified boundary before any Article aggregate mutation or
transaction. The boundary retains its pending-domain-changes protection, identity validations and
mapping conflict handling. Successfully provisioned Identity/AppUser records remain if Article fails;
registration does not compensate or relink them.
The UnitOfWork then owns a clean-scope transaction for the Product/Article graph, with one SaveChanges.
Exceptions roll back all aggregate writes (including Authors, ProductAuthors and ProductValues) and
clear the tracker. SQL retains DOI uniqueness across Article product types.

## Verification

Run `dotnet run --project tests/tesisproject.articlewritetests -c Release`.
The executable creates its own GUID database on `.\DINNOVA`, applies the full migration chain,
and deletes only that fixture in finally. It never opens the application database.

The SQL/EF tests cover both independent types, a concrete project, institutional and external
Authors, multiple authors/order/snapshots, participant dynamics, article structural collections,
canonical view visibility, SQL DOI rejection and aggregate rollback while Identity/AppUser remains.
They also verify mapping conflict without relinking or Article creation and reuse of existing users.
A SaveChanges interceptor also injects failure after aggregate SQL has executed but before commit.
Contract rejection and deterministic form selection are covered as well.

No migration or ArticleReadView change is needed for this write path. Deployment still requires
the previously approved schema migrations, base catalog definitions, active forms and Identity role
`user`; this phase does not apply anything to the application database or activate HTTP routes.
