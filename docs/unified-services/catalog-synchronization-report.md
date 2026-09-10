# Sincronización de catálogos Projects — cierre

> Registro histórico de la fase inicial. La clasificación de carreras/programas, el filtro de raíces y `Skipped` de Faculty quedan reemplazados por [faculty-hierarchy-report.md](faculty-hierarchy-report.md): todos los nodos Id/ParentId son SYNC_CATALOG. Los CSV se actualizaron a esa decisión; las llamadas de negocio permanecen intactas.

## A. Inventario External API Projects

Inventario previo a implementación: **4 clientes, 18 métodos públicos, 37 sitios de
llamada en servicios Projects/core legacy y Unified**. Los endpoints relativos son
los defaults de ExternalApiOptions; host, rutas y nombres de parámetros son configurables.

| Cliente | Endpoints GET / métodos consumidos | Consumidores de negocio | Datos / clasificación |
|---|---|---|---|
| ExternalAcademicsService | /api/facultades; GetFacultiesAsync, GetFacultiesKeyValuesAsync | ProjectService/UnifiedProjectService; ProjectFlatReportService y ProjectsFiltersService legacy | Facultades: SYNC_CATALOG. Carreras del mismo payload: NEEDS_REVIEW. |
| ExternalPeriodsClient | /api/periodos; GetAllAsync | Project, Group, DocumentRecognition, legacy y Unified | Periodos: SYNC_CATALOG. |
| ExternalDirectoryClient | /api/usuarios; GetAllAsync, GetByEmailsAsync (?emails=...) | Project, Group, DocumentRecognition; ProjectFlatReport | IDENTITY_DIRECTORY; en ProjectFlatReport, REPORT_ENRICHMENT. |
| ExternalDistributivosService | /api/distributivos?correos=...; GetDistributivosByCorreosAsync | Project, Group, DocumentRecognition, legacy y Unified | LIVE_OPERATIONAL: asignaciones/cargas por persona y periodo. |

Detalle completo, con rutas, DTOs, consumidor y línea de fuente:
[external-api-projects.csv](external-api-projects.csv).
[external-api-client-methods.csv](external-api-client-methods.csv) distingue los
18 métodos disponibles de los realmente llamados por servicios. Las consultas por
facultad/carrera filtran un snapshot obtenido desde /api/facultades; no se inventan
endpoints /facultades/{id}. Distributivo por ID también filtra la lista completa.
AcademicPeriodsController reutiliza además GetByIdAsync (/api/periodos/{id}); los
controllers de consulta externa publican los listados correspondientes.

No se detectó otro cliente HTTP en las implementaciones Unified Projects examinadas.
Se trata de evidencia acotada de grafo + fuente, no de una auditoría global del proyecto.
Los clientes exclusivos de Articles quedan OUT_OF_SCOPE_ARTICLES y no se reutilizan.

## B. Catálogos confirmados

| Entidad / tabla | Matching exclusivo | Campos sincronizados | Metadata local preservada |
|---|---|---|---|
| Faculty / Faculties | ExternalFacultyId ← ExternalFacultyCareerFlatModel.Id, solo ParentId null | Name (trim), Acronym (trim; blanco → null), LastSyncedAt | FacultyId, IsActive, CreatedAt, relaciones; filas con ExternalFacultyId null |
| AcademicTerm / AcademicTerms | ExternalPeriodId ← ExternalAcademicPeriodModel.PeriodId | Name (trim), StartDate/EndDate normalizadas a Date, LastSyncedAt | AcademicTermId, relaciones; filas con ExternalPeriodId null |

Name/Acronym nunca identifican una fila. Un nombre igual con otro ExternalId produce
otra fila. Acronym es propiedad del proveedor: un null remoto válido puede limpiarlo;
no se confunde con metadata local. Las nuevas Faculty reciben los defaults existentes
(IsActive=true y CreatedAt); una Faculty desactivada localmente no se reactiva.
No se cambia el modelo ni se generan migraciones. Las entidades están físicamente
en UnifiedEntities/Articles, pero esta fase usa solo estos dos catálogos compartidos,
sin analizar ni operar sus navegaciones Articles.

## C. Candidatos no implementados

- **Carreras/programas — NEEDS_REVIEW:** payload plano y ExternalProgramDTO existentes,
  pero sin catálogo local de carreras confirmado para Projects. Se cuentan como Skipped
  en sync Faculty; no se crean entidades o relaciones de carreras.
- **Distributivos — LIVE_OPERATIONAL:** carga/asignación por persona y periodo usada
  para seleccionar carrera. No hay tabla Unified decidida para copiar ese catálogo.
- **Directorio — IDENTITY_DIRECTORY:** perfiles institucionales y datos necesarios para
  resolver personas. AppUser almacena el puente de IDs; no equivale a una réplica del directorio.
- **Perfiles de reportes — REPORT_ENRICHMENT:** enriquecimiento de nombres/datos desde
  el directorio en ProjectFlatReport; sin decisión de persistir snapshots adicionales.
- Tipos académicos/otras estructuras: no se encontró otro catálogo externo confirmado
  en el alcance Projects. Los catálogos CRUD locales no se convierten en integraciones.

## D. Repositories

**0 repositorios nuevos y 0 métodos redundantes añadidos.** Se reutilizan
IUnifiedFacultyRepository/UnifiedFacultyRepository e
IUnifiedAcademicTermRepository/UnifiedAcademicTermRepository:
Query(asNoTracking:false), filtro por conjunto de ExternalIds, ToListAsync y AddAsync.
Los lookups existentes GetByExternalFacultyIdAsync y GetByExternalPeriodIdAsync siguen
intactos. Repositories no realizan HTTP. IUnifiedUnitOfWork conserva **50 slots**.

## E. Servicios y algoritmo

Nuevos pares IUnifiedFacultySynchronizationService/UnifiedFacultySynchronizationService
y IUnifiedAcademicTermSynchronizationService/UnifiedAcademicTermSynchronizationService.
Ambos exponen SynchronizeAsync(CancellationToken).

1. Exigir token válido y scope sin cambios pendientes (evita confirmar otro agregado).
2. Obtener snapshot HTTP completo, sin mutaciones locales.
3. Validar nulls, IDs positivos, duplicados, nombres/longitudes y fechas; normalizar.
4. Consultar filas tracked por ExternalId, nunca por nombre o PK local.
5. Insertar nuevas y modificar solo campos remotos cambiados; preservar metadata local.
6. Hacer **un único UoW.SaveChangesAsync si hay cambios; cero si no los hay**.
7. Ante cancelación/error de save, restaurar valores tracked y separar altas del catálogo.

El sincronizador posee el save. El contexto Unified inyectado se usa para comprobar
cambios pendientes y limpiar el tracking tras fallo, no para reemplazar los repositorios.
El scope DI comparte esa misma instancia con UoW/repositorios. No hay saves por fila.
La limpieza de tracking no pretende deshacer una transacción ya confirmada: ante un
resultado de commit incierto debe releerse el estado; no se usa compensación destructiva.

El nuevo **IUnifiedAcademicCatalogSnapshotClient/UnifiedAcademicCatalogSnapshotClient**
usa los mismos endpoints/opciones y los DTOs externos existentes. Es necesario porque
ExternalAcademicsService descarta duplicados al crear su diccionario, y los clientes
anteriores mezclan []/null/404 o atrapan cancelación como error genérico. Modificarlos
cambiaría consumidores actuales: se mantienen byte a byte. El cliente nuevo conserva
filas repetidas para validación, distingue 200 [] de null/error HTTP y propaga CT.

El contrato observado devuelve una lista JSON completa: no hay paginación implementada
en los clientes existentes. No se guarda por página. Un envelope con items/next se
rechaza como contrato inválido; no se afirma haber verificado el servidor real.

## F. Controller administrativo

UnifiedCatalogSynchronizationController, con ApiController, NonController y
**Authorize(Roles="superadmin")**, reutilizando el rol administrativo ya empleado
por FacultyScopes/AcademicPeriods. No se crea ningún rol nuevo.

| Endpoint preparado | Request | Response |
|---|---|---|
| POST /api/catalog-sync/faculties | Sin body; CancellationToken | ServiceResult<CatalogSynchronizationResult> |
| POST /api/catalog-sync/academic-terms | Sin body; CancellationToken | ServiceResult<CatalogSynchronizationResult> |

Controller solo delega y llama al ToActionResult existente: 200 éxito, 400 validación,
409 conflicto de persistencia/cambios pendientes, 500 proveedor no disponible; conserva
ErrorCode/ValidationErrors/Message. El acceso administrativo exige autenticación.
**Las rutas no están activas**: NonController excluye el discovery actual. No hay jobs,
cron, hosted services ni programación automática. Program.cs sigue ejecutando legacy.

## G. DTOs

Reutilizados: ExternalFacultyCareerFlatModel, ExternalAcademicPeriodModel, ServiceResult,
ErrorType y el mapeo HTTP actual. **Un único resultado nuevo backend**, porque la búsqueda
acotada no encontró un contrato existente con contadores de sincronización:
CatalogSynchronizationResult(TotalExternal, Inserted, Updated, Unchanged, Skipped, Failed, SyncedAt).
Es el contrato de los dos nuevos endpoints administrativos; no duplica DTOs de dominio
ni cambia contratos/frontend existentes. Failed es 0 en éxito; un rechazo atómico
retorna ServiceResult.Fail sin datos/conteos de éxito parciales.

Se agregan códigos en ErrorCodes.CatalogSynchronization y mensajes en el partial
ErrorMessages.CatalogSynchronization. No se introducen strings locales de error en
services/controllers ni mensajes con excepciones del proveedor/SQL.

## H. Idempotencia y ausencias

Pruebas de cada catálogo: primera ejecución **2 inserts / 1 save**; segunda idéntica
**0 inserts / 0 updates funcionales / 0 saves adicionales**. Los cambios de nombre,
acrónimo o fechas producen update del mismo ID local; un ExternalId nuevo inserta.

Política LastSyncedAt: instante UTC de última sincronización de contenido insertado
o cambiado; se inicializa si era null. Una fila idéntica ya sincronizada conserva
su timestamp. SyncedAt del resultado refleja cada ejecución exitosa, incluida una vacía.
Updated cuenta cambios funcionales, no la inicialización aislada de LastSyncedAt.

**Ausentes: no eliminar ni desactivar.** La API no proporciona una semántica inequívoca
de baja; Faculty.IsActive es local y AcademicTerm no tiene flag equivalente. Se mantiene
pendiente decidir si una ausencia estable representa baja y cómo autorizarla.
200 [] devuelve éxito con ceros y deja intacta la base local.

## I. Errores y atomicidad

| Situación | Resultado | Mutaciones / saves |
|---|---|---|
| HTTP 404/401/403/5xx, transporte/timeout | CATALOG_SYNC_PROVIDER_UNAVAILABLE | 0 / 0 |
| JSON malformed, null, envelope inesperado, filas inválidas | CATALOG_SYNC_INVALID_RESPONSE, ValidationErrors cuando hay campo | 0 / 0 |
| ExternalId repetido remoto, incluso con otro nombre | CATALOG_SYNC_DUPLICATE_EXTERNAL_ID, rechaza snapshot entero | 0 / 0 |
| Scope con cambios previos | CATALOG_SYNC_PENDING_CHANGES, Conflict | Sin HTTP ni save; preserva cambios ajenos |
| DbUpdateException / carrera de índice único | CATALOG_SYNC_PERSISTENCE_FAILED, Conflict | Un intento de save; limpia tracking propio tras fallo |
| Cancelación solicitada | OperationCanceledException propagada | Sin save si previa; limpia cambios propios si ocurre durante preparación/save |
| Lista vacía válida | Éxito, contadores cero | 0 / 0 |

Los constraints relacionales siguen defendiendo carreras concurrentes; no hay retry
ciego ni eliminación para resolver conflictos. La transacción del único SaveChanges
provee atomicidad de persistencia en el proveedor relacional; esta fase no prueba SQL real.

## J. DI

AddUnifiedDide registra explícitamente los **dos services scoped**, el controller scoped
y el snapshot client HTTP **transient** con el cliente nombrado ExternalApi existente.
No se reemplazan clientes ni servicios de negocio actuales. La prueba de DI sigue
resolviendo los 50 repositorios, todos los servicios y ahora **46 controllers** (45
paralelos previos + 1 sync), con ValidateOnBuild/ValidateScopes. La comparación HTTP
histórica conserva sus 239 endpoints; los dos nuevos tienen pruebas propias.

## K. Tests

CatalogSynchronizationTests usa **repositorios reales + UnifiedDideDbContext InMemory**,
UoW test double para contar/injectar fallos del save, y HttpMessageHandler aislado.
Cubre ambos catálogos: inserción inicial/repetición, IDs locales y externos físicamente
distintos, rename/acrónimo/fechas, metadata local, null ExternalId sin matching por nombre,
altas nuevas con nombres iguales, listas vacías/ausentes, proveedor fallido, null/duplicados,
longitudes inválidas, fechas ausentes/invertidas, rollback de tracking y cancelación.
Comprueba índices únicos filtrados de ExternalFacultyId/ExternalPeriodId y el constraint
CK_AcademicTerms_Dates mediante el modelo EF. No se modifica el esquema.

Prueba HTTP: rutas, rol, exclusión MVC, delegación al service correcto, CT y preservación
del resultado para 200/400/409. Transporte: [] frente a null, JSON/envelope inválido,
duplicados conservados, errores HTTP y CT. La prueba de DI aislada resuelve la nueva cadena.

Suite completa: **PASS 17074 assertions**, incluyendo las pruebas previas.
No hubo conexión SQL, APIs reales, migraciones ni usuarios reales.

## L. Build y alcance

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings preexistentes, 0 nuevos**, contrastados con el log final anterior.
Logs: artifacts/unified-sync/build-final.log y tests.log.

Comparación SHA-256 contra el estado real de inicio: servicios de negocio legacy/Unified,
los cuatro clientes previos, Program.cs, frontend, entidades/configuraciones/repositorios,
Articles y DW sin cambios. Los únicos archivos preexistentes editados en esta fase son
UnifiedApplicationRegistration, ErrorCodes y dos archivos del runner/composición de tests.
Se preservan los cambios no confirmados de fases anteriores.

## M. Codebase Memory

Se usa el proyecto C-Users-marlo-source-repos-TesisProject. Búsqueda acotada, coverage,
lectura de fuente y refresh final: **22013 nodos / 90320 relaciones**, generación
**2026-09-07T22:33:59Z**, cobertura completa/coincidente según metadatos. Se leyeron
los gaps del helper de errores (:16) y del test (:133); metadata_changed obliga a
respaldar la evidencia con fuente y pruebas compiladas. Las aristas heurísticas se contrastan por receptor
tipado; no se toman como demostración autónoma de ausencia. Por ejemplo, el grafo
confunde SynchronizeFaculties con la interfaz de términos y SaveChanges con RefreshToken;
esas aristas se descartan. Las pruebas comprueban el service concreto invocado.

Caminos: UnifiedCatalogSynchronizationController → IUnifiedFacultySynchronizationService /
IUnifiedAcademicTermSynchronizationService → IUnifiedAcademicCatalogSnapshotClient →
HttpClient ExternalApi → DTOs externos existentes. Persistencia: SynchronizationService →
IUnifiedUnitOfWork → Faculty/AcademicTerm repository → UnifiedDideDbContext.
No hay dependencia operacional hacia AppDbContext, IUnitOfWork/repositorios legacy,
ArticlesDbContext o Article services. La base GenericRepository es infraestructura
compartida y recibe UnifiedDideDbContext por su constructor de DbContext.

## N. Siguiente fase — inventario, sin sustitución

[catalog-sync-next-phase.csv](catalog-sync-next-phase.csv) contiene **12 sitios de llamada**
con servicio actual, método externo, línea, catálogo local, método repository existente
y adaptación de IDs requerida. Los métodos reutilizables son Query/GetAllAsync para
listas y GetByExternalFacultyIdAsync/GetByExternalPeriodIdAsync para referencias externas.
Las combinaciones con distributivos deben conservar ExternalPeriodId: no deben usar
AcademicTermId local bajo el nombre PeriodId externo. La importación debe seguir usando
FK locales. Faculty local no contiene carreras.

Dos de esos sitios están en filtros/reporte legacy cuya implementación Unified ya
lee Faculty local desde una fase anterior; se identifican como ALREADY_LOCAL_IN_UNIFIED,
no como cambios de esta fase. **No se sustituyó ninguna llamada de negocio.**
La infraestructura queda lista para poblar catálogos cuando se habilite explícitamente
su ejecución; esta fase termina sin cutover ni inicio de Articles.
