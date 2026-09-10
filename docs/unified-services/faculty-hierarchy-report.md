# Faculty jerárquico Unified — cierre

## A. Contrato externo real

Contrato verificado en fuente Projects y cliente HTTP; no se consultó un proveedor en vivo.
`GET /api/facultades` (ruta configurable) se deserializa como `List<ExternalFacultyCareerFlatModel>`:

| JSON | Propiedad C# | Semántica |
|---|---|---|
| `id_facultad_carrera` | `int Id` | ID institucional del nodo |
| `id_facultad_carrera_pertenece` | `int? ParentId` | ID institucional del padre; null = raíz |
| `nombre` | `string Name` | Nombre |
| `siglas` | `string? Acronym` | Siglas opcionales |

Es una lista plana con enlaces, sin `Children`, tipo, nivel ni estado activo explícitos.
El contrato no impone dos niveles. `ExternalProgramDTO` participa en la proyección
legacy, no en el transporte: contiene ProgramId/FacultyId/Name. La proyección legacy
agrupa programas bajo raíces; eso no limita la profundidad representable por Id/ParentId.
El snapshot Unified conserva todos los nodos y duplicados para validación posterior;
no fue necesario modificarlo.

Fuente: [contrato](C:/Users/marlo/source/repos/TesisProject/tesisproject.shared/Entities/External/ExternalFacultyCareerFlatModel.cs),
[cliente legacy Projects](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Implementations/ExternalAcademicsService.cs:148),
[snapshot Unified](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedAcademicCatalogSnapshotClient.cs:15).

## B. Decisión de modelo

Faculty/Faculties conserva su nombre. Ahora representa **todos los nodos de la jerarquía
académica institucional**, no solo raíces. No se creó Career/Program ni NodeType.
No se persiste ExternalParentId: se reconstruye uniendo ParentFacultyId con FacultyId
del padre y leyendo su ExternalFacultyId. El padre remoto debe poder resolverse por
ExternalFacultyId; no se necesita duplicar el vínculo para detectar movimientos.

## C. Entidad

Campos: FacultyId (PK local), ExternalFacultyId?, **ParentFacultyId? (FK local)**,
Name, Acronym?, IsActive, CreatedAt, LastSyncedAt?. Navegaciones nuevas Parent/Children;
se conserva Articles sin adaptar su lógica.

[Faculty](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Data/UnifiedEntities/Articles/Faculty.cs).

## D. Configuración EF

Self FK `Faculties.ParentFacultyId → Faculties.FacultyId`, nullable, `DeleteBehavior.NoAction`.
Índice no único ParentFacultyId para hijos; índice único filtrado ExternalFacultyId
`[ExternalFacultyId] IS NOT NULL` conservado, junto con configuración/índices anteriores.
La eliminación de un padre no borra hijos en cascada.

[Configuración](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Data/UnifiedConfigurations/AcademicCatalogConfigurations.cs:41).

## E. Migración

Generada **20260908025200_AddFacultyHierarchy**, exclusivamente para UnifiedDideDbContext:
1 columna int nullable, 1 índice, 1 FK autorreferenciada; Down retira esos mismos elementos.
El diff del snapshot contiene únicamente propiedad, índice, relación y navegaciones.
No hay cambios pendientes entre modelo y última migración, confirmado por EF CLI.

El SQL generado contiene `ADD [ParentFacultyId] int NULL`, `CREATE INDEX` y
`FOREIGN KEY ... REFERENCES [dbo].[Faculties] ([FacultyId])`. SQL Server omite
`ON DELETE NO ACTION` porque es el comportamiento predeterminado; la operación EF
verificada tiene `ReferentialAction.NoAction`. Historial: `dbo.__EFMigrationsHistoryUnifiedDide`.
**No se aplicó la migración a ninguna base de datos.** Las filas existentes permanecen
válidas con padre null hasta que la sincronización establezca las relaciones.

[Migración](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Migrations/Unified/20260908025200_AddFacultyHierarchy.cs),
[SQL generado](C:/Users/marlo/source/repos/TesisProject/artifacts/faculty-hierarchy/AddFacultyHierarchy.sql).

## F. Sincronizador

Se modificó el servicio existente. Flujo:
scope limpio → snapshot completo → validación y normalización → una carga local tracked
→ diccionarios por PK local y ExternalFacultyId → grafo propuesto → validación de padres/ciclos
→ altas y cambios → relaciones por navegación Parent → máximo un UoW.SaveChangesAsync.

Los objetos nuevos permanecen sin tracking hasta validar el grafo completo. Se resuelve
primero el conjunto snapshot/local por ExternalFacultyId, sin matching por nombre.
Padres nuevos y existentes se vinculan mediante Parent; EF resuelve claves locales generadas.
El orden recibido no importa. Se conservan PK, IsActive, CreatedAt y relaciones ajenas al sync.
Mover un nodo o convertirlo en raíz actualiza la misma fila; no borra ni desactiva ausencias.

Ante error de persistencia/cancelación restaura valores y ambos lados de las navegaciones,
y separa las altas del tracking. Se comprobó que un save posterior no resucita altas fallidas.
Esto limpia un intento fallido; no compensa una transacción cuyo commit ya se haya confirmado.

[Servicio](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedFacultySynchronizationService.cs).

## G. Roots / Children

Después de sincronizar la jerarquía, raíz = ParentFacultyId null; hijo = FK local no null.
La jerarquía completa se reconstruye con un diccionario FacultyId → nodo y los enlaces
ParentFacultyId. Un null heredado de antes del primer sync jerárquico completo **no certifica**
que el proveedor considere ese nodo raíz. LastSyncedAt de la fase anterior tampoco certifica eso.

## H. Carreras/programas

Sí: los nodos hijos, nietos y demás descendientes se almacenan como Faculty.
Todos son SYNC_CATALOG. En un resultado exitoso Skipped=0 y Failed=0; un snapshot
inválido produce Fail completo, no éxito parcial. Los CSV reflejan la nueva clasificación.

## I. Repository

Se reutilizan IUnifiedFacultyRepository/UnifiedFacultyRepository, sin métodos ni repositorios
nuevos. Sync usa Query(false).ToListAsync y AddAsync; el save pertenece al UoW.
Se carga **todo el catálogo en una consulta**, porque un padre local omitido puede tener
ancestros fuera del snapshot. Eso permite validar ciclos cruzados sin N+1; el coste
en memoria es proporcional al catálogo local más el snapshot.

Las lecturas usan Query() sin tracking, filtro/proyección SQL y ToListAsync.
El lookup GetByExternalFacultyIdAsync permanece disponible.

## J. Query service

Nuevo IUnifiedFacultyQueryService/UnifiedFacultyQueryService:
GetHierarchyAsync (lista completa con enlaces), GetRootsAsync y GetChildrenAsync(parentFacultyId **local**).
Cada operación realiza una consulta, sin HTTP ni save. Incluye inactivos para no perder
ancestros ni fragmentar la jerarquía. Hijos de una hoja o padre desconocido devuelve lista vacía.
Cancelación se propaga.

UnifiedCatalogQueryService existente está restringido a CatalogEntityBase y devuelve
KeyValueItemDTO; Faculty no hereda esa base. Forzarlo implicaría ampliar el catálogo genérico.

## K. Controller

Se conserva `POST /api/catalog-sync/faculties`. Lecturas nuevas en UnifiedFacultiesController:
`GET /api/faculties/hierarchy`, `GET /api/faculties/roots`,
`GET /api/faculties/{parentFacultyId:int}/children`.
Todos siguen con NonController y rol superadmin, sin activación MVC.
AddUnifiedDide añade únicamente query service/controller scoped. Program.cs intacto.
ValidateOnBuild/ValidateScopes y resolución de la composición aislada pasan.

## L. DTOs

CatalogSynchronizationResult y DTO externo se reutilizan. Se revisaron KeyValueItemDTO
(Id/Name), FacultyScopeFacultyItemDTO (IDs/IsActive), ExternalFacultyDTO y ExternalProgramDTO
(proyección externa de dos niveles): ninguno representa los enlaces locales completos.
Se añadió **un único FacultyHierarchyNodeDTO** con FacultyId, ExternalFacultyId,
ParentFacultyId, Name, Acronym, IsActive, LastSyncedAt. La lista de adyacencia permite
reconstruir Children a cualquier profundidad sin recursión ni límite de anidamiento JSON.
Los DTOs existentes permanecen intactos.

## M. Idempotencia

Primera prueba: raíz y dos hijos → 3 inserts, 1 save.
Segunda ejecución idéntica, incluso recargando el contexto → 0 inserts, 0 updates, 0 saves.
LastSyncedAt cambia con Name/Acronym/Parent, o se inicializa si era null.
La inicialización aislada de timestamp conserva el contador funcional Unchanged.

## N. Hierarchy validation

IDs positivos, padre positivo si existe, IDs únicos, nombres/siglas dentro de longitudes,
sin autopadre, padres resolubles y sin ciclos. Se priorizan nodos del snapshot; un padre
omitido solo se acepta si ya existe local por ExternalFacultyId. Si falta en ambos: INVALID_RESPONSE.
Se valida iterativamente el grafo propuesto con enlaces locales conservados, incluidos
ancestros omitidos; no solo las aristas del payload. También se rechazan inconsistencias
locales detectadas (padre local inexistente, duplicación externa o ciclo), sin guardar.

Límite de evidencia: las pruebas no ejecutan SQL contra un servidor. Una FK por sí sola
no impide ciclos ni convierte dos sincronizaciones concurrentes en una operación serializada;
la validación del servicio utiliza el estado leído por cada ejecución.

## O. Distributivos

Permanece LIVE_OPERATIONAL, sin sincronización ni cambios. Su FacultyId (`id_facultad`)
y CareerId (`id_carrera`) son externos y deberán resolverse contra Faculty.ExternalFacultyId;
PeriodId (`id_periodo`) contra AcademicTerm.ExternalPeriodId. Nunca se asignan directamente
a FK locales. ExternalDirectoryClient permanece IDENTITY_DIRECTORY; Identity intacto.

## P. Business services

No se sustituyeron llamadas en Project, Group, DocumentRecognition, ProjectFlatReport
ni ProjectsFilters. Se actualizó catalog-sync-next-phase.csv: conserva 12 sitios previos
y añade 4 capacidades de contrato claramente identificadas, no supuestos nuevos callers.
Los consumidores que hoy enumeran todo Faculty deberán considerar la distinción raíces/hijos
en la siguiente fase; su lógica no se adaptó aquí.

## Q. Articles

Sin modificaciones a sus servicios, controllers, DTOs, ArticlesDbContext ni migraciones.
Únicamente cambia Faculty del modelo operacional Unified y su configuración/migración.
Impacto futuro: cuando ese catálogo compartido contenga descendientes, cualquier selector
que requiera solo raíces deberá aplicar esa semántica. No se realizó esa adaptación.
DW y frontend permanecen intactos.

## R. Tests

**17.175 assertions PASS** en la suite de servicios. FacultyHierarchyTests cubre A–R:
IDs externos 500/800 frente a locales 17/29, orden inverso, 3 niveles, movimiento,
paso a raíz, huérfano, autopadre, ciclos de 2/3, duplicados, nombres iguales, metadata,
ausencias, fallo de proveedor y padre local omitido. Adicionalmente: 512 niveles,
ciclos cruzados con ancestros locales, rotación válida, rollback de navegaciones,
cancelación, reintento, consultas locales y aislamiento del controller.

Metadata EF: FK nullable, PK objetivo, NoAction, índices y navegaciones. Prueba offline
con proveedor SQL Server: Parent se resuelve a clave local temporal antes del save.
Operaciones Up/Down inspeccionadas por tests y SQL revisado. InMemory sirve para
comportamiento del servicio; no se presenta como prueba de integridad real de SQL Server.

[Pruebas](C:/Users/marlo/source/repos/TesisProject/tests/tesisproject.unifiedservicetests/FacultyHierarchyTests.cs),
[resultado](C:/Users/marlo/source/repos/TesisProject/artifacts/faculty-hierarchy/tests.log).

## S. Build

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings, 0 nuevos**, comparados con la salida de la fase anterior.
Comparación SHA256 frente al inicio de esta fase confirma el alcance de archivos.

[Build](C:/Users/marlo/source/repos/TesisProject/artifacts/faculty-hierarchy/build-final.log),
[alcance](C:/Users/marlo/source/repos/TesisProject/artifacts/faculty-hierarchy/scope-check.json).

## T. Codebase Memory

Refresco full final: 22.083 nodos / 90.717 aristas; generación **2026-09-08T02:57:03Z**,
metadata completa y generación coincidente. Búsquedas y trazas acotadas paginadas sin
resultados pendientes. Cobertura consultada para 28 archivos y migraciones Unified.
Huecos relevantes: helper de errores línea 16 y CatalogSynchronizationTests línea 133,
verificados en fuente. Las rutas restantes no registran huecos, pero el proveedor sigue
marcando metadata_changed incluso tras refrescar; no se asume completitud por ello.

Las trazas CALLS proponen asociaciones heurísticas incorrectas (por ejemplo, el endpoint
Faculty hacia el contrato de AcademicTerm y Query/SaveChanges hacia otros servicios).
Se descartaron: el camino se verificó con receptores tipados, fuente, composición DI y tests:

`/api/facultades → List<ExternalFacultyCareerFlatModel> → snapshot completo →
UnifiedFacultySynchronizationService → uow.Faculties/IUnifiedFacultyRepository →
GenericRepository<Faculty>(UnifiedDideDbContext) → Faculties → ParentFacultyId → FacultyId`.

`uow.SaveChangesAsync → UnifiedUnitOfWork._context.SaveChangesAsync` sobre el mismo contexto.
La autorrelación queda respaldada por metadata EF y SQL, no por una arista heurística.

Fase cerrada. No se inició la sustitución de llamadas de negocio.
