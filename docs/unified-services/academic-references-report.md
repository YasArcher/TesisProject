# Cierre Faculty / AcademicTerm Unified — 2026-09-07

Fase terminada: **21 TODO iniciales, 9 resueltos y 12 pendientes por Identity**.
Los entry points dependientes de Identity permanecen ausentes; sus etapas de
preparación académica están implementadas y probadas, sin activar el runtime.
Este informe complementa el histórico de `report.md` y Products.

## A. Infraestructura

Se añadieron `IUnifiedFacultyRepository` / `UnifiedFacultyRepository` y
`IUnifiedAcademicTermRepository` / `UnifiedAcademicTermRepository` con lookup por
`ExternalFacultyId` y `ExternalPeriodId`, respectivamente. Usan `AsNoTracking` y
`SingleOrDefaultAsync` sobre `UnifiedDideDbContext`; no llaman proveedores externos
ni ejecutan saves. No existía una API equivalente. Las entidades sincronizadas
no heredan `CatalogEntityBase`, por lo que no encajan en `IUnifiedCatalogRepository<T>`.

`IUnifiedUnitOfWork` y `UnifiedUnitOfWork` pasan de 48 a **50 repositories** con
propiedades y parámetros explícitos de constructor, compartiendo contexto.
No se introdujeron service locator, reflexión de resolución ni registros runtime.

`UnifiedAcademicReferencePreparation` centraliza validación y lookup.
`PreparedImportAcademicReferences` separa preparación de aplicación de una fila.
`InternalsVisibleTo` permite probar esos helpers internos directamente.

La revisión de repositories, servicios y clientes externos no encontró un owner
existente que sincronice estos dos catálogos locales. Se requiere una fila local
ya sincronizada: una referencia ausente falla; no se crea, actualiza ni sincroniza
silenciosamente. Las entidades, índices únicos filtrados por ID externo y sus
configuraciones existentes permanecen intactos.

## B. Faculty

`ExternalFacultyId 501 → lookup Faculty.ExternalFacultyId → FacultyId 17`.

FacultyScope resuelve el conjunto completo y construye sus relaciones con IDs
locales. Project.Create recibe un contrato Unified explícito y pasa el ID local
al mapper. Conserva la consulta del AppUser de negocio ya existente del actor;
esta variante no requiere provisión Identity y queda habilitada.

CreateFull conserva la selección de carrera/coordinador a partir del directorio,
periodos y distributivos; `PrepareFullProjectFacultyAsync` devuelve Faculty local
sin modificar el request. AddMember prepara la carrera seleccionada mediante
`PrepareCoordinatorFacultyAsync`: carrera externa → facultad externa → fila local.
Ambos entry points siguen pendientes por Identity.

Bootstrap consulta facultades locales activas y entrega IDs locales compatibles
con los filtros sobre Project. El reporte plano usa nombre e ID de la facultad
local y carga GroupType mediante la navegación del repository de Groups. Conserva
enriquecimiento de personas por directorio externo, sin consultar AspNetUsers.
Sus tres exportadores dependientes quedan habilitados.

## C. AcademicTerm

`ExternalPeriodId 20261 → lookup AcademicTerm.ExternalPeriodId → AcademicTermId 8`.

`PrepareImportAcademicReferencesAsync` resuelve la facultad y todos los periodos
de las visitas ejecutadas antes de permitir aplicar una fila. Conserva la selección
legacy por similitud del nombre y el fallback al último ID de periodo externo,
pero ambos caminos requieren lookup local. Si hay visitas ejecutadas y el catálogo
externo está vacío, devuelve el error compartido de periodos no disponibles; ya
no permite omitir silenciosamente esas visitas. Si no hay visitas ejecutadas, no
exige un periodo artificial.

`PreparedImportAcademicReferences.ApplyTo` asigna la facultad local y construye
visitas con `AcademicTermId` local y documentos asociados, sin persistir. El owner
real de importación continúa pendiente por Identity; las pruebas usan un owner
simulado para verificar aplicación y único save. Estos helpers no activan la carga
completa de matrices.

Los DTO operativos de Visit no reciben un periodo externo; ese flujo no necesitaba
traducción. `ProjectExtension.AcademicTermId = null` conserva su adaptación previa.

## D–E. TODO y métodos

La unidad de conteo es cada método de `pending.csv`, incluyendo el mapper privado;
no el número de líneas con comentarios TODO. Los 12 restantes figuran también aquí
con su motivo actual exacto.

| Service Unified | Method | Before | After | Status |
|---|---|---|---|---|
| FacultyScope | CreateAsync | IDs externos como FK; dos saves legacy | Todos los IDs resueltos; relaciones locales; un save | Resuelto |
| FacultyScope | SetFacultiesAsync | Identidad de IDs ambigua | Resolución completa antes de reactivar, añadir o desactivar relaciones tracked | Resuelto |
| Project | CreateAsync | TODO amplio de referencias/Identity | Variante sin provisión Identity; Faculty local; un save | Resuelto |
| Project | MapToEntity | FacultyId de entrada ambiguo | Parámetro localFacultyId obtenido del lookup | Resuelto |
| ProjectsFilters | GetBootstrapAsync | Catálogo externo para filtros locales | Catálogo local y claves locales | Resuelto |
| ProjectFlatReport | GetFlatReportAsync | Join entre FK local y catálogo externo | Join con Faculty local, consultas por UoW | Resuelto |
| ExportTemplateExcel | GenerateExcelAsync | Dependía del reporte pendiente | Consume reporte Unified habilitado | Resuelto |
| MatrixExcelExport | GenerateExcelAsync | Dependía del reporte pendiente | Consume reporte Unified habilitado | Resuelto |
| MatrixTemplateExcelExport | GenerateExcelAsync | Dependía del reporte pendiente | Consume reporte Unified habilitado | Resuelto |
| FacultyScope | AssignScopeToUserAsync | EnsureAppUser con commit anticipado | Separar provisión Identity de asignación de scope | TODO Identity |
| Group | AddMemberAsync | Faculty + EnsureAppUser/roles | Facultad preparada; falta provisión Identity y roles | TODO Identity |
| Group | GetExternalUserByAspNetIdAsync | Lectura AspNetUsers | Definir frontera de lectura Identity fuera del UoW Unified | TODO Identity |
| Group | GetExternalUsersByGroupAsync | Lectura AspNetUsers | Definir frontera de lectura Identity fuera del UoW Unified | TODO Identity |
| Group | GetProjectMembersReportAsync | Lectura AspNetUsers | Definir frontera de lectura Identity fuera del UoW Unified | TODO Identity |
| ProjectMatrix | UploadAsync | Import pendiente por Identity y referencias | Preparación académica lista; depende del entry point Import y su Identity | TODO Identity |
| Project | CreateFullAsync | Faculty + provisión de miembros/roles | Faculty preparada; falta EnsureAppUsers y roles | TODO Identity |
| Project | ImportFromMatrixAsync | Faculty + AcademicTerm + Identity | Referencias preparadas; falta provisión Identity de miembros | TODO Identity |
| Project | InsertGroupMembersFromDirectoryAsync | TODO genérico con referencias | El cuerpo legacy no asigna Faculty/AcademicTerm; faltan EnsureAppUsers y roles | TODO Identity |
| AppUser | EnsureAppUserAsync | Provisión Identity con saves implícitos | Definir frontera y compensación de provisión/roles fuera de transacción de dominio | TODO Identity |
| AppUser | EnsureAppUsersAsync | Provisión Identity con saves por usuario | Definir frontera y compensación de provisión/roles fuera de transacción de dominio | TODO Identity |
| AppUser | EnsureSingleInternalAsync | Provisión Identity con saves implícitos | Definir frontera y compensación de provisión/roles fuera de transacción de dominio | TODO Identity |

## F. DTO y errores

La revisión de consumidores precedió los cambios: TechnicianGroupsTab carga
facultades externas y reutiliza las claves al editar; ProjectCreate envía el ID
externo en el DTO legacy; ProjectDetail selecciona una carrera externa para el
coordinador; ProjectsHome compara bootstrap con la facultad del proyecto.

Se añadieron `UnifiedCreateFacultyScopeRequestDTO` y
`UnifiedSetFacultyScopeFacultiesRequestDTO`, con `ExternalFacultyIds`, y
`UnifiedAddProjectRequestDTO`, con `ExternalFacultyId`. Los contratos de entrada
legacy continúan intactos. `FacultyScopeFacultyItemDTO` incorpora un
`ExternalFacultyId` nullable adicional: Unified entrega ambos conceptos; el mapper
legacy mantiene su comportamiento y deja el campo adicional vacío. Una futura
conexión de esa UI a Unified deberá usar ese campo externo para sus selecciones.

Bootstrap y reporte no necesitan ambos IDs: sus IDs son locales en Unified.
Los DTO externos conservan su semántica externa. Los requests legacy de CreateFull
y AddMember se leen solamente en preparaciones internas; sus campos ambiguos se
traducen explícitamente antes de producir resultados locales. No se expuso un
entry point Unified con esos contratos mientras continúa pendiente Identity.

Se reutiliza `Common.InvalidId` para referencias no positivas, con
`ErrorType.Validation` y `ValidationErrors`. Se añadieron únicamente los errores
compartidos `AcademicReferences.FacultyNotSynchronized` y
`AcademicReferences.AcademicTermNotSynchronized`, ambos `ErrorType.NotFound`.
Los fallos se propagan conservando mensaje, tipo, código y errores por campo.
Los exportadores reutilizan los errores Shared existentes.

## G. Atomicidad y fronteras

| Flujo | Saves legacy | Saves Unified |
|---|---:|---:|
| FacultyScope.Create | 2 | 1 |
| FacultyScope.SetFaculties | 1 | 1 |
| Project.Create | 1 | 1 |
| Lookups y preparaciones internas | No aplica | 0 |
| ApplyTo de fila preparada | No aplica | 0 |
| Bootstrap, reporte y exportadores | 0 | 0 |

Todos los IDs solicitados se validan/resuelven antes de cambiar el agregado.
Un fallo de resolución produce cero saves y cero cambios añadidos, modificados o
eliminados. Una entidad puede estar cargada como Unchanged; tracker limpio no
significa tracker vacío. No se añadieron transacciones explícitas.

Los GET al directorio/distributivos/periodos son fronteras externas de lectura;
no se promete rollback HTTP con EF. La preparación no sincroniza datos locales.
Después de la fase de aplicación, un fallo de persistencia requiere descartar el
scope/contexto según su ciclo de vida; no se promete limpiar un tracker ya mutado.

## H. Pruebas

`dotnet run --project tests/tesisproject.unifiedservicetests --no-restore`:
**PASS, 4108 aserciones**. Se añadió `AcademicReferenceTests.cs` y se actualizó el
conteo del UoW a 50. Se preservan las regresiones anteriores, incluidas Products.

Cobertura: IDs físicamente distintos (501→17 y 20261→8); referencias inexistentes
e inválidas; un ID erróneo entre varios; deduplicación; listas vacías; reactivación
y desactivación; cero mutaciones y saves al fallar; un save en owners exitosos;
cero saves en helpers; request CreateFull intacto; selección de carrera externa;
importación con segundo periodo no sincronizado; fallback que requiere fila local;
visitas construidas con FK local; bootstrap/reporte y Excel con la clave local;
ausencia de entry points completos aún bloqueados por Identity.

Las pruebas usan el modelo y ChangeTracker reales de EF con repositories de prueba
y guardado simulado. Comprueban traducción SQL de los predicados de lookup, pero
**no abren una base de datos** ni prueban una transacción SQL Server real.

## I. Build y alcance

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings, 0 warnings nuevos** respecto de la baseline.
`git -c core.whitespace=cr-at-eol diff --check` pasa.

La comparación de hashes contra el inicio de esta fase confirma que Products,
Identity, Articles, Controllers, Program.cs/DI runtime, modelo/migraciones y los
arreglos FIX_NOW no recibieron cambios. Se preservan los cambios previos del usuario
y de las fases anteriores. Unified sigue sin consumidores runtime.

## J. Codebase Memory

Proyecto `C-Users-marlo-source-repos-TesisProject`, nivel Verify. Refresh completo
solicitado y terminado (`status: indexed`), **21089 nodos / 83493 aristas**.
La cobertura reporta generación `2026-09-07T05:10:28Z`, modo `full`, registro
`2026-09-07T11:33:03Z`, `recording_status: complete`, `generation_matches: true`.
Estas son las marcas devueltas por MCP; no se interpretan como certificación de
frescura: sigue reportando `metadata_changed` tras el refresh.

Se trazaron ambos sentidos de FacultyAsync y AcademicTermAsync, sin paginación
restante. Caminos verificados con snippets y receptor tipado en fuente:

1. Request/carrera/facultad externa → preparación → UoW.Faculties →
   GetByExternalFacultyIdAsync → FacultyId local → Project/FacultyScope.
2. Periodo externo seleccionado → preparación de importación → UoW.AcademicTerms →
   GetByExternalPeriodIdAsync → AcademicTermId local → fila preparada → Visit.

Las aristas a interfaces de lookup son heurísticas (confianza 0.28); se verificaron
contra las implementaciones concretas y propiedades tipadas del UoW. Se descartaron
relaciones USAGE espurias a Article.Faculty y tipos Task homónimos. No implican una
dependencia del dominio Articles ni garantizan un camino runtime activo.

Cobertura consultada para archivos nuevos/modificados, entidades/configuración,
Visit/Extension y consumidores, con scopes de servicios, repositories y DTO. Hay
dos huecos relevantes de parser: `UnifiedAcademicReferencePreparation.cs:46` y
`AcademicReferenceTests.cs:166` (inicializadores de colecciones), leídos en fuente.
Los cuatro consumidores Razor reportan `not_tracked` y se verificaron directamente.
Las búsquedas de asignaciones en el ámbito Unified revisado no encontraron uso
directo de IDs externos como FK de Project/Visit sin lookup. El índice registra
otros huecos fuera del alcance; no se afirma cobertura exhaustiva del repositorio.

La fase se detiene aquí; los 12 TODO de Identity se mantienen en `pending.csv`.
