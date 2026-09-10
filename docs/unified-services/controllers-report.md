# Controllers y composición DI Unified — cierre 2026-09-07

## A. Inventario de controllers

**45 controllers concretos, 239 endpoints paralelos; 1 base abstracta de catálogos.**
El inventario operacional legacy contiene 240 endpoints contando `GET api/auth/me`.
Ese único endpoint no se duplica: su implementación depende de `IArticleUserContext`
y de permisos de Articles (`OUT_OF_SCOPE_ARTICLES`). No se publica una sesión con
permisos inventados. Auth register/login/refresh/logout sí tienen equivalencia Unified.

Todos los controllers paralelos llevan `[NonController]`: la prueba de descubrimiento
MVC mantiene Projects legacy activo y excluye los 45 Unified. Registrar sus clases
para resolución DI no los publica. Los tokens `[controller]` de las clases concretas
se sustituyen por el nombre legacy en minúsculas, equivalente a la convención activa.
Program.cs y los controllers legacy permanecen sin cambios.

| Legacy | Paralelo | Endpoints | Clasificación |
|---|---|---:|---|
| AcademicPeriodsController | UnifiedAcademicPeriodsController | 5 | DIRECT |
| AuthController | UnifiedAuthController | 4 | IDENTITY_BOUNDARY |
| BudgetsController | UnifiedBudgetsController | 11 | DIRECT |
| ConvocationsController | UnifiedConvocationsController | 8 | DIRECT |
| CountriesController | UnifiedCountriesController | 5 | DIRECT |
| DocumentRecognitionController | UnifiedDocumentRecognitionController | 2 | DIRECT |
| DocumentTypesController | UnifiedDocumentTypesController | 5 | ADAPTABLE |
| DocumentsController | UnifiedDocumentsController | 7 | DIRECT |
| ExportTemplatesController | UnifiedExportTemplatesController | 7 | DIRECT |
| ExternalAcademicsController | UnifiedExternalAcademicsController | 1 | DIRECT |
| ExternalPeriodsController | UnifiedExternalPeriodsController | 1 | DIRECT |
| ExternalResearcherProjectsController | UnifiedExternalResearcherProjectsController | 4 | DIRECT |
| ExternalResearchersController | UnifiedExternalResearchersController | 5 | DIRECT |
| FacultyScopesController | UnifiedFacultyScopesController | 9 | ADAPTABLE, CONTRACT_CHANGE, IDENTITY_BOUNDARY |
| FundingTypesController | UnifiedFundingTypesController | 5 | ADAPTABLE |
| GroupsController | UnifiedGroupsController | 12 | DIRECT, IDENTITY_BOUNDARY |
| IndexingSourcesController | UnifiedIndexingSourcesController | 5 | DIRECT |
| InstitutionsController | UnifiedInstitutionsController | 5 | DIRECT |
| MemberRoleTypesController | UnifiedMemberRoleTypesController | 5 | DIRECT |
| ObjectiveActivitiesController | UnifiedObjectiveActivitiesController | 6 | DIRECT |
| ObjectiveActivityUsersController | UnifiedObjectiveActivityUsersController | 4 | DIRECT |
| ObjectiveTypesController | UnifiedObjectiveTypesController | 5 | ADAPTABLE |
| ProductAttributeDefinitionsController | UnifiedProductAttributeDefinitionsController | 5 | DIRECT |
| ProductAttributesController | UnifiedProductAttributesController | 5 | DIRECT |
| ProductTypeDesignController | UnifiedProductTypeDesignController | 3 | DIRECT |
| ProductTypesController | UnifiedProductTypesController | 5 | ADAPTABLE |
| ProductsController | UnifiedProductsController | 6 | ADAPTABLE, DIRECT |
| ProjectExtensionTypesController | UnifiedProjectExtensionTypesController | 5 | ADAPTABLE |
| ProjectExtensionsController | UnifiedProjectExtensionsController | 6 | DIRECT |
| ProjectMatrixController | UnifiedProjectMatrixController | 2 | DIRECT |
| ProjectObjectivesController | UnifiedProjectObjectivesController | 7 | DIRECT |
| ProjectOriginTypesController | UnifiedProjectOriginTypesController | 5 | ADAPTABLE |
| ProjectResearchCategoryController | UnifiedProjectResearchCategoryController | 5 | DIRECT |
| ProjectStatesController | UnifiedProjectStatesController | 5 | ADAPTABLE |
| ProjectTypesController | UnifiedProjectTypesController | 5 | ADAPTABLE |
| ProjectsController | UnifiedProjectsController | 9 | ADAPTABLE, DIRECT, IDENTITY_BOUNDARY |
| ProjectsFiltersController | UnifiedProjectsFiltersController | 1 | DIRECT |
| ResearchCategoriesController | UnifiedResearchCategoriesController | 6 | DIRECT |
| ResearchCategoryGroupsController | UnifiedResearchCategoryGroupsController | 5 | ADAPTABLE |
| ResearchCategoryTypeController | UnifiedResearchCategoryTypeController | 5 | ADAPTABLE |
| TransactionTypesController | UnifiedTransactionTypesController | 5 | ADAPTABLE |
| VisitIssuesController | UnifiedVisitIssuesController | 5 | DIRECT |
| VisitObjectiveActivityProgressesController | UnifiedVisitObjectiveActivityProgressesController | 2 | DIRECT |
| VisitStatesController | UnifiedVisitStatesController | 5 | ADAPTABLE |
| VisitsController | UnifiedVisitsController | 11 | DIRECT |

Fuera de alcance, sin análisis funcional ni cambios: ArticlesController,
ArticlesConfigurationController, ArticlesCatalogsController,
ArticlesRegistrationMatricesController y DwEtlController. No se cuentan sus rutas.

El [inventario por endpoint](controllers-inventory.csv) incluye servicio/método
legacy y Unified, request, response, status, autorización, verbo y ruta. AcademicPeriods
Create/Update no llaman un servicio: mantienen el rechazo explícito de escritura externa.

## B. Compatibilidad HTTP

La [comparación completa](controllers-compatibility.csv) contiene **239 filas**, una
por ruta/verbo, incluidos los dos GET de ProductTypeDesign. Se obtuvieron de métodos
compilados, incluyendo acciones heredadas; se contrastan tipos, parámetros, binding,
valores por defecto, atributos, autorización y rutas. No se infieren contratos a
partir del nombre Unified.

**239/239 conservan tipos request/response y metadatos HTTP/auth.** Esto no significa
que todos los valores tengan la misma semántica; esas diferencias se clasifican abajo.
El helper existente `ToActionResult` sigue devolviendo el ServiceResult completo.
Se conservan 201 de CreateTemplate, contenido binario de documentos/Excel y sus ramas
de error. ExportMatrixToExcel conserva su switch propio (Unexpected → 400); la
duplicación se identifica pero no se refactoriza globalmente.

ResearchCategoryType conserva las cinco firmas HTTP existentes sin parámetro CT,
y ahora propaga `HttpContext.RequestAborted` al servicio. FacultyScopes conserva el
comportamiento previo de `users/{userId}/allowed-faculties`: resuelve el actor actual,
no el userId de la ruta. No se corrige esa regla de negocio en esta fase.

## C. DTOs

**DTO nuevos en esta fase: 0. DTO Shared modificados: 0.** La lista exhaustiva de
contratos reutilizados figura en ambas columnas de la comparación CSV.

| DTO previo Unified | Decisión | Uso |
|---|---|---|
| UnifiedAddProjectRequestDTO | INTERNO | POST Projects recibe AddProjectRequestDTO; FacultyId externo → ExternalFacultyId, copiando todos los campos. |
| UnifiedCreateFacultyScopeRequestDTO | INTERNO | CreateFacultyScopeRequestDTO.FacultyIds externos → ExternalFacultyIds. |
| UnifiedSetFacultyScopeFacultiesRequestDTO | INTERNO | SetFacultyScopeFacultiesRequestDTO.FacultyIds externos → ExternalFacultyIds. |
| UnifiedAuthorWriteRequest | INTERNO | CRUD Author del servicio; ningún nuevo endpoint autoral. |
| UnifiedAuthorResponse | INTERNO | Resultado del CRUD Author; no se introduce en Product HTTP. |

Los tres adaptadores conservan los nombres HTTP históricos. El pequeño adaptador
`UnifiedFacultyRequestErrors` traduce exclusivamente las claves internas
externalFacultyId(s) a FacultyId(s), preserva todos los mensajes/códigos y no muta
el resultado del servicio. No hay nueva lógica de resolución de IDs en controllers.
Los responses locales de scopes conservan FacultyId local y ExternalFacultyId explícito.
AcademicPeriods/ExternalPeriods siguen siendo catálogos externos; sus IDs no se
reinterpretaban como AcademicTermId local y no se renombran.

Product ya tiene un **Shared ProductAuthorResponseDTO adecuado**: AuthorId, AuthorType,
AppUserId, ExternalResearcherId y UserId nullable como alias institucional. No se
inventa UserId para externos. ProductCreate/Update mantienen AuthorUserIds institucionales.
La lectura externa cabe en el contrato vigente; no hace falta otro Author DTO público.
La selección/escritura externa sigue sin estar expresada por AuthorUserIds: sería una
ampliación funcional posterior, no se añade ahora. Su reemplazo explícito puede retirar
vínculos externos existentes; deuda previa preservada. Los consumidores institucionales
identificados en `products-functional-report.md` siguen usando sus mismos DTOs.

Se evitan copias de Project, Group, Budget, Visit, Document, Product, objetivos,
catálogos y reportes solo por cambiar persistencia. No hay contratos nuevos que justificar.

## D. Impacto frontend y conteo

| Categoría | Endpoints |
|---|---:|
| COMPATIBLE: sin cambio identificado de contrato | **216** |
| INTENTIONAL_CHANGE: compatible/aditivo o diferencia semántica explícita, sin cambio obligatorio del consumidor actual identificado | **16** |
| FRONTEND_IMPACT: consumidor de facultades debe distinguir IDs locales/externos | **7** |
| Total Unified | **239** |

Los 16 cambios intencionales son: Projects GetAll/GetById/GetByType/Create (4),
bootstrap (1), Matrix flat/upload (2), Products salvo Delete (5), y los flujos de
provisión Projects CreateFull, Groups AddMember, FacultyScopes Assign, Auth Register (4).
Los detalles y la razón concreta aparecen en cada fila CSV. No se equipara coincidencia
de tipos con igualdad semántica: IDs locales de lista y bootstrap deben consumirse
juntos; errores nuevos de Identity/importación usan el contrato de fallo existente.

Los **7 FRONTEND_IMPACT** son FacultyScopes: GET lista, GET por id, POST create,
PUT update, PUT faculties, GET users/{userId}/allowed-faculties y GET me/allowed-faculties.
Las selecciones institucionales deben usar ExternalFacultyId, no reenviar FacultyId
local al request externo. La diferencia ya proviene del servicio Unified; no se
oculta devolviendo IDs externos bajo una FK local. Es un impacto contractual por
endpoint, no un conteo de pantallas. Los consumidores conocidos se documentaron en
la fase académica; no se repitió una auditoría frontend ni se editó frontend.

## E. DI Unified

`Services/Unified/UnifiedApplicationRegistration.cs` expone
`AddUnifiedDide(configuration, configureDatabase)`, sin invocación desde Program.
[Registros explícitos](controllers-di.csv):

- UnifiedDideDbContext scoped; la configuración del proveedor la proporciona el caller.
- 50 repositorios scoped (33 especializados y 17 catálogos cerrados).
- IUnifiedUnitOfWork → UnifiedUnitOfWork scoped.
- **42 interfaces de servicio**: 41 registros cerrados y CatalogCrud abierto; se
  resuelven también las 17 especializaciones de catálogo admitidas por la UoW.
- IdentityCore, roles, UserManager/RoleManager/SignInManager, stores EF sobre
  UnifiedDideDbContext y token providers mediante AddUnifiedIdentityBoundary.
- 45 controllers scoped, sin registrar rutas.
- CurrentUserService (claims) y JwtTokenService (firma/configuración), scoped como
  legacy; no son servicios de persistencia legacy. Cuatro clientes externos tipados
  conservan su registro HTTP transient, opciones, caché y nombre ExternalApi.
- Logging, routing, authentication, authorization, memoria y HttpContextAccessor.
  No se configura un esquema de autenticación de producción ni se hace cutover.

Servicios (prefijo IUnified omitido):

AppUserService, AuthorService, AuthService, BudgetService, CatalogCrudService, CatalogQueryService, ConvocationService, CountryService, DocumentRecognitionService, DocumentService, ExportTemplateExcelService, ExportTemplateService, ExternalResearcherProjectService, ExternalResearcherService, FacultyScopeService, GroupService, IdentityProvisioningService, IdentityQueryService, IndexingSourceService, InstitutionService, MatrixExcelExportService, MatrixTemplateExcelExportService, MemberRoleTypeService, ObjectiveActivityService, ObjectiveActivityUserService, ProductAttributeDefinitionService, ProductAttributeService, ProductService, ProductTypeDesignService, ProjectExtensionService, ProjectFlatReportService, ProjectMatrixService, ProjectObjectiveService, ProjectResearchCategoryService, ProjectService, ProjectsFiltersService, RefreshTokenService, ResearchCategoryService, ResearchCategoryTypeService, VisitIssueService, VisitObjectiveActivityProgressService, VisitService

Se incorporaron Auth/RefreshToken Unified únicamente para completar los cuatro
endpoints de sesión: reutilizan los DTOs, cookies, emisión de tokens y reglas actuales;
AppUser usa la frontera Unified y RefreshToken usa el contexto Unified. No se crea
un repository adicional ni se rediseña la rotación de tokens.

## F. Resolución DI

ServiceProvider aislado con **ValidateOnBuild=true / ValidateScopes=true**:
45 controllers resueltos; todas las interfaces de servicio cerradas, las 17
especializaciones CatalogCrud y los 50 slots repository resueltos. Se comprueba
identidad de instancias scoped, separación entre scopes y que el store Identity
comparte la instancia de contexto. **0 registros faltantes, 0 ciclos, 0 ambigüedades,
0 conflictos, 0 errores de lifetime** al cierre. La composición rechaza registro
duplicado y mezcla con un store Identity ya registrado.

Se usó InMemory exclusivamente en pruebas, sin SQL, migraciones ni usuarios reales.
Resolver DocumentService conserva su creación de directorio configurado: en el test
se limita a artifacts/unified-controllers/storage. No se llama a APIs externas.

## G. Dispose ownership

DI es dueño del contexto scoped. UnifiedUnitOfWork recibe una referencia prestada;
`DisposeAsync` devuelve ValueTask.CompletedTask. Cambio de ownership mínimo (2 líneas
por 1), conservando la interfaz. La prueba dispone la UoW y verifica que el contexto
sigue utilizable; el scope dispone el contexto al terminar. La doble disposición
anterior podía ser tolerada por EF, pero no justificaba dos propietarios y podía
cerrar anticipadamente el contexto compartido por Identity/repositorios.
Un caller que construya manualmente la UoW debe disponer su propio contexto.

## H. Password debt

Las tres contraseñas fijas de Project/Group/FacultyScope permanecen pendientes,
sin modificaciones ni nuevas copias. No se trasladan a controllers, logs ni responses.
Auth conserva únicamente el uso de la contraseña recibida para su operación Identity;
no se añaden invitaciones/reset ni otro diseño de seguridad.

## I. Articles y alcance protegido

Comparación SHA-256 contra el estado real al comenzar esta fase: Program.cs,
controllers legacy, frontend, Shared, Articles, DW, modelos, repositorios y servicios
preexistentes **sin cambios**. Se preservan los cambios no confirmados de fases previas.
Solo se modificaron dos archivos preexistentes: DisposeAsync de UnifiedUnitOfWork y
el runner de tests para invocar las nuevas pruebas. Los demás archivos de esta fase
son paralelos nuevos o documentación.

## J. Tests

`dotnet run --project tests/tesisproject.unifiedservicetests --no-restore`:
**PASS 16877 assertions**. Pruebas nuevas: UnifiedControllerTests y
UnifiedRequestAdapterTests. Comparación compilada de 239 endpoints y **1416 escenarios
HTTP**, más adaptadores con valores distinguibles en todos los campos, validación de
claves HTTP, metadata IdentityMappingConflict, CT, DI y exclusión MVC. Los dos writes
AcademicPeriods sin llamada de servicio se comparan por contrato y fuente.
No se duplican exhaustivamente las pruebas de negocio de los servicios.

## K. Build

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings preexistentes, 0 nuevos**. Comparadas también las identidades
de warnings con el log final de la fase Identity. Logs locales reproducibles en
artifacts/unified-controllers/build-final.log y tests.log.

## L. Codebase Memory y límites de evidencia

Refresh full completado: **21895 nodos / 89691 relaciones**. Cobertura consultada por
lotes (límite MCP de 128 paths), generación final observada **2026-09-07T21:53:16Z**, completa
y coincidente. Sin gaps registrados en Controllers/Unified ni Repositories/Unified;
permanece el gap conocido de UnifiedAcademicReferencePreparation:46, leído en fuente.
La señal metadata_changed persiste: se verificó fuente y resolución compilada.
Además, 41 controllers se contrastaron por equivalencia de fuente tras sustituir
solo tipos/rutas; los otros 4 contienen las adaptaciones explícitas documentadas.
Auth/RefreshToken conservan su fuente funcional salvo los tipos Unified.

Camino comprobado por tipos inyectados, snippets y DI:

- UnifiedProjectsController → IUnifiedProjectService → UnifiedProjectService →
  IUnifiedUnitOfWork → UnifiedProjectRepository → UnifiedDideDbContext.
- CreateFull/Group/FacultyScope → IUnifiedIdentityProvisioningService →
  UserManager/store EF → la misma instancia scoped de UnifiedDideDbContext.
- UnifiedAuthController → UnifiedAuthService → UnifiedAppUserService → frontera
  Identity; login/refresh/revoke usan managers/store y UnifiedRefreshTokenService.

Las trazas CALLS son heurísticas: CreateFull sigue atribuyendo erróneamente la
llamada al cliente frontend; Register atribuye string.Format a ExportTemplateColumn.
Esas aristas se descartan por el receptor tipado. **El grafo no se presenta como
prueba autónoma de ausencia**: la cadena real se respalda con fuente, firmas compiladas,
registros explícitos y pruebas. GenericRepository compartido vive en carpeta legacy,
pero recibe UnifiedDideDbContext; no incorpora un repository operacional legacy.
No se encontraron dependencias operacionales hacia AppDbContext, IUnitOfWork/repositories
legacy ni ArticlesDbContext en la cadena resuelta. Las fronteras comunes de claims,
tokens y clientes HTTP se declaran expresamente arriba.

Fase cerrada sin activación de rutas Unified, sin SQL y sin cutover.
