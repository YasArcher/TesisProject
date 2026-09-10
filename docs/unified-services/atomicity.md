# SaveChanges y atomicidad

## Actualización: atomicidad interna — 2026-09-06

Se corrigieron los **3 FIX_NOW** del diagnóstico previo. Cada owner prepara sus
cambios y llama una sola vez a `IUnifiedUnitOfWork.SaveChangesAsync`. Los restantes
riesgos conservan su clasificación pendiente; no se certifica atomicidad global.
El detalle y las pruebas están en [atomicity-fixes-report.md](atomicity-fixes-report.md)
y los conteos en [atomicity-fixes.csv](atomicity-fixes.csv).

| Service | Method | Clasificación | Estado |
|---|---|---|---|
| UnifiedProductTypeDesignService | SaveDesignAsync | FIX_NOW | FIXED |
| UnifiedConvocationService | CreateAsync | FIX_NOW | FIXED |
| UnifiedExportTemplateService | CreateTemplateAsync | FIX_NOW | FIXED |
| UnifiedProductService | CreateAsync | BLOCKED_BY_LOGIC | BLOCKED_BY_LOGIC |
| UnifiedFacultyScopeService | CreateAsync | BLOCKED_BY_LOGIC | BLOCKED_BY_LOGIC |
| UnifiedProjectService | CreateFullAsync | IDENTITY_BOUNDARY | IDENTITY_BOUNDARY |
| UnifiedProjectService | ImportFromMatrixAsync | IDENTITY_BOUNDARY | IDENTITY_BOUNDARY |
| UnifiedGroupService | AddMemberAsync | IDENTITY_BOUNDARY | IDENTITY_BOUNDARY |
| UnifiedFacultyScopeService | AssignScopeToUserAsync | IDENTITY_BOUNDARY | IDENTITY_BOUNDARY |
| AppUserService | EnsureAppUsersAsync | IDENTITY_BOUNDARY | IDENTITY_BOUNDARY |
| UnifiedDocumentService | UploadAsync | EXTERNAL_SIDE_EFFECT | EXTERNAL_SIDE_EFFECT |
| UnifiedDocumentService | ReplaceFileAsync | EXTERNAL_SIDE_EFFECT | EXTERNAL_SIDE_EFFECT |
| UnifiedDocumentService | DeleteAsync | EXTERNAL_SIDE_EFFECT | EXTERNAL_SIDE_EFFECT |
| ExternalDirectory/Periods/Academics/Distributivos | Consultas desde services | EXTERNAL_SIDE_EFFECT | EXTERNAL_SIDE_EFFECT |

Se mantienen los 28 TODO. Las filas de Project con Identity también están
bloqueadas por las decisiones funcionales descritas en sus notas. Identity/roles
de la tabla histórica es la misma frontera de provisión, no un riesgo nuevo.
La fila HTTP conserva la agrupación original y no representa un conteo de endpoints.

**Scopes fallidos:** los fallos durante preparación no dejan mutaciones nuevas de
estos flujos. Si falla aplicar cambios o el save final, el caller debe descartar
el scope/UoW/contexto completo. No se limpian entidades ajenas ni se promete que
un catch revierta el ChangeTracker. La recarga posterior al commit puede fallar
cuando la escritura ya se confirmó. Un único save tampoco introduce exclusión
entre solicitudes concurrentes de activación ni una transacción con filesystem.

**Corrección de lectura del histórico:** Product.DeleteAsync sigue siendo TODO
Unified. Su mención bajo “Operaciones con un solo save” describe el camino legacy,
no una implementación operativa Unified.

## Diagnóstico histórico de la fase de copia (conservado)

Lo siguiente registra el estado anterior. Las tres filas ahora FIXED ya no
describen el estado actual de sus implementaciones Unified; el resto permanece.

## Alcance y owner

La capa Unified permanece sin DI runtime. Cada service de persistencia recibe
`IUnifiedUnitOfWork`; los repositorios especializados se obtienen de esa instancia.
`UnifiedCatalogAccess` selecciona exclusivamente los 17 catálogos de la UoW, sin
service locator. `UnifiedVisitIssueService` obtiene VisitIssues y Visits de la UoW.
El registro futuro debe dar el mismo scope a todos los services, UoW y contexto.
La inyección por sí sola no garantiza compartir instancia si ese registro es incorrecto.

Los CRUD públicos son owners de su operación. La preferencia es preparar todo y
guardar una vez al final. Esta fase conserva los límites legacy de las copias
mecánicas: **no certifica atomicidad global y no corrige masivamente los commits**.
Los riesgos siguientes siguen siendo trabajo pendiente antes de activar runtime.
Los métodos bloqueados por cambios funcionales no existen en las interfaces Unified.

`savechanges.csv` contiene los 96 métodos legacy Projects/core que contienen saves
explícitos. `service-calls.csv` y `service-to-service.md` contienen las 45 llamadas
Service → Service encontradas mediante los receptores declarados y sus métodos.
En la tabla, las líneas del caller incluyen sus helpers; para UpsertAttributeAsync
e InsertGroupMembersFromDirectoryAsync incluyen el owner SaveDesignAsync/import.
Un helper puede no guardar directamente y aun así formar parte de un flujo con commits.

## Commits parciales demostrados

| Operación legacy | Secuencia y fallo posterior posible | Estado Unified / owner deseado |
|---|---|---|
| ProductTypeDesignService.SaveDesignAsync | UpsertProductTypeAsync guarda en 209 o 248; UpsertAttributeAsync llama CreateAsync/UpdateAsync de ProductAttributeService (saves 85/121); otro atributo puede fallar antes de SyncDefinitionsAsync y del save 163. | Copia operativa con ATOMICITY_RISK explícito. Owner deseado SaveDesignAsync. Reutilizar validaciones de atributos mediante un contrato de preparación sin commit; no sustituirlas por Add/Update ciegamente. |
| ProductService.CreateAsync | Guarda Product en 114; SyncAuthorsAsync o inserción de Values pueden fallar antes del segundo save 129. | TODO por autoría. Resolver Author antes de persistir agregado; owner CreateAsync. |
| ConvocationService.CreateAsync | Guarda convocatoria activa en 68; SetActiveExclusiveAsync consulta y modifica otras convocatorias; save 71 puede fallar dejando más de una activa. | Copia operativa preservada, ATOMICITY_RISK. Owner CreateAsync; evaluar preparar todas las entidades con una sola confirmación. El repository no hace commit. |
| ExportTemplateService.CreateTemplateAsync | Guarda cabecera en 120 para obtener ID; BuildColumnsEntitiesAsync consulta campos y crea columnas; save 126 puede fallar dejando cabecera incompleta. | Copia operativa preservada, ATOMICITY_RISK. Owner CreateTemplateAsync; evaluar relaciones por navegación para insertar el agregado en un save. |
| FacultyScopeService.CreateAsync | Guarda scope en 103; guarda facultades en 115. | TODO por external/local. Owner CreateAsync. |
| ProjectService.CreateFullAsync | EnsureAppUsersAsync (701) confirma cada usuario mediante EnsureSingleInternalAsync (224); luego puede fallar composición de participantes/objetivos/presupuesto antes del save 877. | TODO por Identity y Faculty. Owner deseado CreateFullAsync; provisión Identity es frontera separada. |
| ProjectService.ImportFromMatrixAsync | InsertGroupMembersFromDirectoryAsync → EnsureAppUsersAsync (1095); confirma por usuario; otra fila puede fallar antes del save final 1738. | TODO por Identity y Faculty/AcademicTerm. Definir además atomicidad por lote o fila; no se inventó esa regla. |
| GroupService.AddMemberAsync | EnsureAppUserAsync (506) ocurre antes de validar asociación duplicada, Faculty del coordinador y proyecto asociado; el save del grupo llega después. | TODO. Owner AddMemberAsync para el dominio; provisión separada. |
| FacultyScopeService.AssignScopeToUserAsync | EnsureAppUserAsync (263) confirma Identity/AppUser antes de recargar AppUser y guardar asignación (296). | TODO. Owner AssignScopeToUserAsync. |
| AppUserService.EnsureAppUsersAsync | Recorre solicitudes y EnsureSingleInternalAsync guarda en cada iteración; fallo de un usuario posterior no revierte los anteriores. | Provisión OUT_OF_SCOPE_IDENTITY. Solo se migró la lectura IdLocal → IdUser. |

Un SaveChanges sobre un contexto compartido guarda **todas** las modificaciones
pendientes de ese contexto, incluidas las preparadas por el caller. Compartir UoW
no evita los commits prematuros. Las llamadas a Identity mediante UserManager
también pueden confirmar mediante el store registrado sobre AppDbContext.
EnsureSingleInternalAsync intenta eliminar un usuario recién creado si falla el rol
o el save de AppUser; eso es una compensación limitada, no un rollback de todo el
proyecto/lote, de usuarios existentes ni de asignaciones de rol ya confirmadas.

## Operaciones con un solo save

Los CRUD de Budget/transactions, ResearchCategory, ProjectObjective,
ObjectiveActivity, ObjectiveActivityUser, VisitIssue, VisitObjectiveActivityProgress,
ExternalResearcher/participación, ProductAttribute/definitions, catálogos, así como
Project.UpdateAsync/DeleteAsync/UpdateResearchCategoriesAsync y Product.DeleteAsync
conservan una confirmación por camino exitoso normal. El owner es el método público
indicado en `savechanges.csv`; esto no autoriza llamarlos dentro de otra operación
compuesta sin revisar el commit.

CatalogCrudService.UpdateAsync tiene ramas excluyentes: guardar clon y desactivar
anterior en un save, o retornar SaveAndOkAsync como helper terminal. No son dos
commits consecutivos. VisitService.FinalizeAsync guarda los cambios del flujo en
un solo save. Author Create/Update/Delete validan antes de mutar y guardan una vez;
sus consultas no guardan ni crean autores automáticamente.

Varios CRUD recargan referencias después del save (por ejemplo Visit.CreateAsync,
ProjectExtension y Document.UpdateAsync). Si esa lectura falla, el resultado puede
ser un error aunque la escritura ya se confirmó: no confundir fallo de respuesta
con rollback. También deben descartarse scopes con cambios pendientes después de
un resultado fallido; no reutilizarlos para otra operación que haga save.

## Efectos externos

| Operación | Orden | Riesgo / recomendación futura |
|---|---|---|
| Document.UploadAsync | Escribe archivo → inserta Document → save | Si SQL falla, TryDeleteFile intenta compensar. Puede quedar archivo huérfano si la limpieza falla. Compensación/reconciliación idempotente. |
| Document.ReplaceFileAsync | Escribe nuevo → save de ruta → borra viejo → recarga BD | Si falla la recarga después del commit, el catch llama TryDeleteFile(newPhysicalPath): la BD puede apuntar a un archivo eliminado y el viejo también haberse borrado. Separar claramente fase confirmada y limpieza/recarga; no implementado aquí. |
| Document.DeleteAsync | Borra registro → save → TryDeleteFile | Un fallo del filesystem no revierte SQL; puede quedar archivo huérfano. Cola/outbox de limpieza y retry idempotente. |
| ExternalDirectory/Periods/Academics/Distributivos | Consultas HTTP desde Project, Group, filtros, reconocimiento y reportes | Frontera externa auditada como EXTERNAL_SIDE_EFFECT; las operaciones observadas son consultas, no se demostró escritura remota irreversible. No prometer snapshot común con EF ni rollback remoto. Retry idempotente según contrato del proveedor. |
| Identity/roles | UserManager.CreateAsync/AddToRoleAsync/DeleteAsync y UoW legacy | Persistencia separada del owner de Projects, aunque el store comparta AppDbContext. Mantener frontera fuera de Unified; evaluar compensación/process manager según la futura decisión funcional. |

Los Excel exportadores usan streams en memoria; no se demostró un envío de archivo,
correo o escritura remota. Reconocimiento lee el archivo recibido y consulta APIs.
No se añadieron Outbox, Saga, transacciones distribuidas ni compensaciones nuevas.

## Transacciones explícitas

La búsqueda acotada y lectura de fuente no encontraron BeginTransaction,
Commit/Rollback, CreateExecutionStrategy ni TransactionScope en Services Projects/core.
ArticleRegistrationCommandService sí contiene BeginTransactionAsync y CommitAsync;
se registró exclusivamente como OUT_OF_SCOPE_ARTICLES y no se migró. EF puede hacer
atómico un SaveChanges relacional; esto no hace atómicas varias llamadas separadas,
la recarga posterior ni los efectos de filesystem/HTTP/Identity.

## Actualización: Products funcional

Product.CreateAsync pasa de los dos saves legacy a un único save Unified.
Update conserva un save y valida autores/values antes de mutar el agregado.
SyncAuthorsAsync no guarda; las altas Author pertenecen al mismo owner.
Ver [products-functional-report.md](products-functional-report.md).
