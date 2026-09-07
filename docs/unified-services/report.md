# Services Projects Unified — cierre de fase

Se crearon **38 interfaces y 38 implementaciones paralelas**, más el selector
interno `UnifiedCatalogAccess`. Se conservaron los services legacy, los 48
repositorios de la UoW y el contexto ya terminado. No se modificaron Controllers,
Program.cs, DI runtime, Identity store, Articles, migraciones ni BD.

La fase entrega las equivalencias operativas y **28 métodos/helpers TODO UNIFIED**
con referencias legacy exactas; no equivale a migración funcional completa ni a
activación de runtime. Algunas interfaces de reportes/importación quedan sin
métodos operativos porque su único entry point depende de una decisión pendiente.
No devuelven éxitos ficticios ni excepciones stub en lugar de implementar el flujo.

## A–B. Inventario y archivos

- [inventory.csv](inventory.csv): service legacy → Unified, clasificación y estado.
  CREATED indica que existe el par, no que todos sus métodos estén migrados.
- [created.csv](created.csv): las 38 interfaces y sus implementaciones.
- [inventory-detail.md](inventory-detail.md): dependencias, repositorios, entidades
  y DTOs de cada service Projects/core y sus fronteras.
- [methods.csv](methods.csv): clasificación y evidencia por método descubierto en
  el inventario inicial, con saves y llamadas; incluye helpers/constructores.
- [pending.csv](pending.csv): los 28 métodos pendientes, decisión y archivo con el
  código legacy conservado. AppUser se incorporó después al inventario parcial;
  su lectura operativa y Author nuevo se describen abajo.

DIRECT: CRUD/lecturas de Budget, Country, Institution, IndexingSource,
ExternalResearcher/participación, objetivos/actividades/asignaciones, investigación,
catálogos de productos, Visit/VisitIssue/progreso y equivalencias locales de Project,
Group y FacultyScope. Los services mixtos se clasifican LOGIC_CHANGE en el inventario,
con sus métodos operativos detallados separadamente.

ADAPTABLE: catálogos genéricos obtienen repositorios de la UoW; VisitIssue evita
repositorios inyectados por separado; MemberRoleTypeRepository → MemberRoleTypes;
ProjectExtension.AcademicPeriodId = null → AcademicTermId = null (no transforma un ID
externo); Product.ListByProjectAsync proyecta el ProjectId con valor porque el
repository filtra por ese mismo FK. No se cambió ningún DTO Shared existente.

EXTERNAL_BOUNDARY: Document y reconocimiento/consultas externas conservan su frontera.
Los clientes HTTP y CurrentUser se reutilizan: son transporte/claims sin persistencia
Unified propia. AppUser solo migra GetAppUserIdByLocalIdAsync mediante el repository:
IdLocal → búsqueda → IdUser. La provisión y roles permanecen TODO/Identity.
El archivo AppConfigurationRepository dentro de Services es un repository mal
ubicado y ya tiene equivalente terminado; no se duplicó. DW, Articles,
RegistrationMatrix y runtime de autenticación permanecen fuera de alcance.

## C. Author CRUD

Contrato nuevo, separado de los DTOs de Products:

```csharp
UnifiedAuthorWriteRequest(int? AppUserId, int? ExternalResearcherId, string? Orcid = null)
UnifiedAuthorResponse(int AuthorId, int? AppUserId, int? ExternalResearcherId, string? Orcid)

Task<ServiceResult<UnifiedAuthorResponse>> GetByIdAsync(int authorId, CancellationToken ct = default);
Task<ServiceResult<IReadOnlyList<UnifiedAuthorResponse>>> GetAllAsync(CancellationToken ct = default);
Task<ServiceResult<UnifiedAuthorResponse>> GetByAppUserIdAsync(int appUserId, CancellationToken ct = default);
Task<ServiceResult<UnifiedAuthorResponse>> GetByExternalResearcherIdAsync(int externalResearcherId, CancellationToken ct = default);
Task<ServiceResult<UnifiedAuthorResponse>> CreateAsync(UnifiedAuthorWriteRequest request, CancellationToken ct = default);
Task<ServiceResult<UnifiedAuthorResponse>> UpdateAsync(int authorId, UnifiedAuthorWriteRequest request, CancellationToken ct = default);
Task<ServiceResult<NoContent>> DeleteAsync(int authorId, CancellationToken ct = default);
```

Create/Update validan fuente XOR, ID positivo, existencia de AppUser o
ExternalResearcher y ausencia de otro Author para esa fuente. Update sustituye los
campos escribibles después de validar y conserva AuthorId y ExternalAuthorId. ORCID
se recorta, vacío pasa a null, y se valida longitud 50/unicidad según el modelo
existente; no se inventa una validación bibliográfica. Delete rechaza autores
enlazados a productos. Conflictos de FK/unicidad concurrentes se convierten en el
error compartido de persistencia. La BD conserva sus constraints como última defensa.

Cada mutación válida hace un save; validaciones fallidas y lecturas no guardan.
Las búsquedas por fuente no crean Author. ExternalAuthorId no aparece en el
contrato ni se usa como identidad. Las pruebas usan IdUser=41,
ExternalResearcherId=82 y AuthorId=901 para verificar la separación semántica.

Se añadieron seis códigos/mensajes Author realmente nuevos a ErrorCodes/ErrorMessages.
66 textos de error ya presentes como literales legacy se centralizaron y deduplicaron
en ErrorMessages.UnifiedLegacy, conservando su texto y los códigos anteriores.
Los ServiceResult mantienen ErrorType, ErrorCode y ValidationErrors. No se añadió
ApiResponse ni un contrato alternativo de errores.

## D, G–H. Métodos pendientes

Cada TODO en fuente enlaza [legacy-reference](legacy-reference/) y queda fuera de
la interfaz cuando el método era público. Los cuerpos completos se conservaron en
Markdown, sin reemplazarlos por null, listas vacías, éxito ficticio o casts de IDs.

- Product.SyncAuthorsAsync: AuthorUserIds contiene IdUser; faltan la decisión de
  crear Author automáticamente y el contrato para fuentes externas. Existen
  Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y
  ProductAuthors.ExistsForAuthorAsync; el flujo será fuente → Author → AuthorId.
- Product.MapToDetailDTO: UserId puede representar AppUserId institucional, pero
  no ExternalResearcher. Se requiere DTO autoral con fuente/discriminador. Create,
  Update y GetById que dependen de este mapping permanecen TODO.
- Product.ListAsync: su DTO exige ProjectId no nullable; pendiente acordar cómo
  representar producción independiente. ListByProjectAsync y DeleteAsync sí operan.
- Project.CreateAsync/CreateFullAsync/MapToEntity/importación y helper de miembros:
  resolución Faculty/AcademicTerm external/local y/o provisión Identity pendiente.
- FacultyScope.CreateAsync/SetFacultiesAsync: no asumir que FacultyIds externos
  son FK locales. AssignScopeToUserAsync requiere separar la provisión Identity.
- Group.AddMemberAsync: mezcla provisión Identity y Faculty del coordinador.
  GetExternalUsersByGroupAsync/GetExternalUserByAspNetIdAsync/GetProjectMembersReportAsync
  consultan AspNetUsers y quedan fuera hasta definir esa frontera.
- ProjectFlatReport.GetFlatReportAsync y ProjectsFilters.GetBootstrapAsync mezclan
  facultades persistidas y catálogo externo. Sus exportadores y Upload de
  ProjectMatrix que dependen de métodos bloqueados también quedan TODO.
- AppUser.EnsureAppUserAsync/EnsureAppUsersAsync/EnsureSingleInternalAsync no se
  migran: crean identidades/roles y confirman por usuario.

Las lecturas operativas de FacultyId en Unified entregan IDs **locales**. No se
registraron consumidores runtime que pudieran interpretarlos como IDs externos.

## E–F. Service → Service y transacciones

[service-to-service.md](service-to-service.md) contiene la tabla de 45 llamadas con
caller, callee, saves, contexto, efectos, clasificación y recomendación.
[atomicity.md](atomicity.md) documenta los escenarios concretos y los owners;
[savechanges.csv](savechanges.csv) registra los 96 métodos con commits legacy.

Riesgos confirmados: ProductTypeDesign → ProductAttribute; provisión AppUser desde
Project/Group/FacultyScope; múltiples saves en Product.Create, Convocation.Create,
ExportTemplate.CreateTemplate y FacultyScope.Create. Las copias operativas de
ProductTypeDesign, Convocation y ExportTemplate **conservan esos riesgos**, señalados
para diseñar preparación/commit sin alterar reglas en esta fase. No se quitaron
saves ni se sustituyó lógica de negocio por repositories a ciegas.

Document tiene efectos de filesystem no reversibles por EF; ReplaceFile puede
eliminar el archivo nuevo si falla la recarga después del commit. No se implementaron
Outbox, Saga ni compensaciones nuevas. No hay transacción explícita Projects/core;
la encontrada en ArticleRegistrationCommandService está fuera de alcance.

## I. Validación

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings preexistentes, 0 warnings nuevos**.
El build incremental backend produjo 0 errores y 19 warnings, su baseline.

`dotnet run --project tests/tesisproject.unifiedservicetests/tesisproject.unifiedservicetests.csproj --no-restore`:
PASS; CRUD Author, XOR, fuentes inexistentes, duplicados, errores de persistencia,
cancelación, IDs distintos, saves y dependencias compiladas. No conecta a BD.
No prueba traducción SQL ni transacciones reales con proveedor relacional; los
riesgos transaccionales se verificaron mediante fuente y call graph, no se afirman
resueltos mediante mocks.

Para no duplicar warnings legacy, se hizo explícita la comprobación de request en
MemberRoleType/ProductAttribute y de ObjectiveId nullable en creación de actividad.
Dos anotaciones de nulabilidad conservan valores opcionales del transporte externo;
no convierten ni reinterpretan IDs. No se silenciaron warnings mediante pragmas.

## J. Grafo

Ver [graph-validation.md](graph-validation.md) para generación final, cobertura y
límites de evidencia. Se utilizó nivel Verify con descubrimiento MCP, trazas,
snippets y fuente directa. Algunas aristas heurísticas resolvían SaveChangesAsync a
RefreshTokenService o consultas de repository a métodos homónimos de otro service;
se descartaron por el tipo del receptor en fuente. No se interpretaron como
dependencias reales. La prueba compilada comprueba además que los campos y
constructores Unified no dependen de persistencia legacy.

## Actualización: cierre funcional de Products

Los siete TODO de Product quedan resueltos; pending.csv conserva los otros 21.
Ver [products-functional-report.md](products-functional-report.md) para contratos,
autoría, ProjectId nullable, atomicidad, compatibilidad legacy y validación.
Las secciones anteriores se conservan como histórico de la fase inicial.

## Actualización: cierre Faculty / AcademicTerm

Se resolvieron 9 de los 21 TODO restantes; `pending.csv` conserva 12 por Identity.
Las FK de Faculty/AcademicTerm se obtienen mediante lookup explícito de su ID externo.
El UoW expone ahora 50 repositories; los owners habilitados guardan una vez y los
helpers no guardan. CreateFull e importación conservan sus entry points pendientes,
con preparación académica implementada y probada. Ver
[academic-references-report.md](academic-references-report.md) para el informe A–J,
tabla de métodos, contratos, límites y evidencia final. Rebuild: 0 errores,
39 warnings preexistentes; suite Unified: PASS, 4108 aserciones.
