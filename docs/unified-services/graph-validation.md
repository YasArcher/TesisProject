# Verificación Codebase Memory

Proyecto: `C-Users-marlo-source-repos-TesisProject`. Nivel **Verify**.
Se confirmó raíz y branch mediante list_projects/index_status antes de descubrir
símbolos. La generación inicial fue `2026-09-06T17:54:41Z`.

## Descubrimiento y evidencia

Se localizaron contexto, UoW, repositorios de Product/ProductAuthor/Author y clases
de Services mediante search_graph. El inventario inicial de métodos devolvió 700
resultados en una página completa (`has_more=false`); quedó guardado como
`graph-methods.json`. Los informes anteriores se leyeron antes de crear esta capa.

Se trazaron SaveDesignAsync, sus helpers, el servicio de atributos y la creación
de AppUser; se leyeron snippets de EnsureSingleInternalAsync y de los saves. La
consulta de CALLS devolvió 771 aristas candidatas. La tabla final de 45 llamadas
Service → Service se construyó verificando en fuente el **tipo real del receptor**,
incluidas llamadas genéricas que la resolución heurística omitía o confundía.

## Reindexación y comprobación final

Una reindexación explícita `index_repository(repo_path=..., mode=full)` terminó
con `status=indexed`: **20.784 nodos, 80.938 aristas**, cero archivos omitidos por
fallo y 15 archivos con parsing parcial fuera de la capa nueva. El watcher también
actualizó el proyecto durante la fase. Las solicitudes simultáneas rechazadas
porque otro índice estaba activo se reintentaron sin reemplazar/eliminar el índice.

La cobertura posterior confirmó generación **2026-09-06T18:52:46Z**, metadata
registrada en `2026-09-06T18:53:50Z`, modo full, recording_status complete y
generation_matches true. Las 84 rutas consultadas (77 archivos de Services nuevos,
errores compartidos, pruebas y puntos materiales UoW/Author repository) no tenían
huecos registrados; el scope Services/Unified tampoco tenía incidencias ni páginas
pendientes. Todos indicaban `metadata_changed`: **no se trató esa señal como prueba
de completitud/frescura**. Se contrastaron fuentes y compilación; los últimos
ajustes de texto de respuesta y pruebas no cambian la arquitectura.

Search_graph encontró **38 clases Unified*Service y 38 interfaces**, sin paginación
pendiente. Una consulta de USAGE encontró 34 archivos con dependencia directa de
IUnifiedUnitOfWork (incluye UnifiedCatalogAccess). Los transformadores y reportes
delegan a interfaces Unified o permanecen TODO; no necesitan un contexto propio.

Las consultas de USAGE desde Services/Unified hacia UnitOfWork/Interfaces legacy
y Repositories/Implementations legacy devolvieron cero resultados. El snippet
exacto de UnifiedUnitOfWork.SaveChangesAsync confirma `_context.SaveChangesAsync(ct)`;
el campo y constructor usan UnifiedDideDbContext. Las tres implementaciones de
Product/ProductAuthor/Author reciben ese contexto y sus interfaces pertenecen a
Repositories/Unified. La abstracción genérica de repositorios sigue reutilizando
GenericRepository<T>, como ya estaba acordado en la fase previa.

## Aristas inesperadas verificadas

El grafo **no está libre de falsos positivos**. No se eliminó una dependencia ni se
afirmó migración de Articles basándose únicamente en una coincidencia nominal:

| Destino aparente del grafo | Fuente real verificada |
|---|---|
| RefreshTokenService.SaveChangesAsync | `_uow.SaveChangesAsync`, con receptor IUnitOfWork o IUnifiedUnitOfWork según la capa. |
| IUnifiedExportFieldRepository.AddAsync desde Author | `_uow.Authors.AddAsync`, cuyo contrato es IUnifiedAuthorRepository. |
| Articles/AcademicTerm.EndDate desde Group/Recognition | Propiedad de ExternalAcademicPeriodModel devuelto por el cliente de períodos; solo comparación de fechas externas. |
| Articles/ArticleFile.FileName desde Document | `request.File.FileName` (archivo recibido), nombre local y nombre del elemento de una tupla; no ArticleFile. |
| Articles/DynamicFieldOption.FieldId desde ExportTemplate | `col.FieldId` de ExportTemplateColumnUpsertDTO. |
| Articles/Article.ExternalId desde Group | `p.ExternalId` con parámetro ExternalUserProfileModel. |
| ArticleRegistrationCommandService.Date/Text; ExternalAcademicsService.Unexpected; DisabledArticleQueryService.Message | Miembros de DateTime, enums o ServiceResult; no llamadas a esos services. |

Estas aristas nominales impiden certificar aislamiento usando exclusivamente CALLS
o USAGE. La búsqueda sobre los 77 archivos nuevos, excluyendo comentarios, no
encontró AppDbContext, ArticlesDbContext, IUnitOfWork legacy, repositorios legacy,
GetByExternalAuthorId ni ApiResponse. Los namespaces UnifiedEntities.Articles no se
importan desde la capa nueva. La prueba inspecciona campos y parámetros de
constructores del ensamblado compilado y valida el aislamiento de persistencia.

## Límites

Se verificó estructura y comportamiento de Author con dobles de repositorios,
identificadores distintos, errores y conteo de saves; también la lectura
IdLocal → IdUser. No se conectó a BD ni se ejercitaron APIs, Identity o filesystem.
El grafo y los tests aislados no prueban atomicidad distribuida, traducción SQL,
collation ni ausencia de carreras. Los riesgos legacy documentados siguen vigentes.
Program.cs conserva los registros legacy y AddEntityFrameworkStores<AppDbContext>.

## Actualización: Products funcional

MCP accesible; se reutilizó el proyecto existente. El refresco automático produjo
la generación 2026-09-07T04:45:24Z (full, complete). Se verificaron snippets y
USAGE de Products/Authors/AppUsers/ProductAuthors en la UoW, con trazas paginadas
completas de un salto. Persisten aristas CALLS heurísticas incorrectas y cobertura
metadata_changed; se contrastaron con fuente y tests. Detalles y límites en
[products-functional-report.md](products-functional-report.md).
# Actualización Faculty / AcademicTerm (2026-09-07)

Refresh completo terminado: 21089 nodos / 83493 aristas. Generación reportada
2026-09-07T05:10:28Z, registro de cobertura 2026-09-07T11:33:03Z, modo full.
Persisten señales metadata_changed y consumidores Razor not_tracked, suplidos con
fuente directa. Dos rangos parciales relevantes: UnifiedAcademicReferencePreparation
línea 46 y AcademicReferenceTests línea 166, inspeccionados. Trazas bidireccionales
de FacultyAsync y AcademicTermAsync sin páginas pendientes; receptores tipados
verificados para aristas heurísticas. Detalle de caminos, cobertura y limitaciones
en [academic-references-report.md](academic-references-report.md#j-codebase-memory).


## Cierre Identity / AppUser

Refresh full terminado: 21308 nodos / 85346 aristas; generación reportada
2026-09-07T18:17:23Z, registro 2026-09-07T18:22:19Z. Se verificaron las
fronteras Identity, la composición de stores Unified y los callers del dominio.
Las aristas heurísticas incorrectas se descartaron por receptor tipado en fuente.
Persiste metadata_changed; se completó la evidencia con lectura directa y pruebas
compiladas/ejecutadas. Detalle en [identity-boundary-report.md](identity-boundary-report.md#o-codebase-memory).
