# Projects Unified — consumidores de catálogos locales

Faculty y AcademicTerm se consumen como **último snapshot local sincronizado**, no como
información en tiempo real. Directory y Distributivos conservan su naturaleza live.
No se hizo cutover ni se aplicaron migraciones.

## A. Inventario inicial

Se partió de catalog-sync-next-phase.csv y de los informes de sincronización/jerarquía,
verificando los sitios en fuente antes de editarlos.

| Service Unified / método | Cliente y método anteriores | Semántica / sustituto | Estado final |
|---|---|---|---|
| Project / ImportFromMatrixAsync | ExternalPeriodsClient.GetAllAsync | Nombres/fechas e IDs externos para matching; TermsAsync + mapa local para Visit | Migrado |
| Project / ImportFromMatrixAsync | ExternalAcademicsService.GetFacultiesAsync | Raíces para similitud de nombre; FacultiesAsync/FacultyRoots + mapa local para Project | Migrado |
| Project / PrepareFullProjectFacultyAsync | ExternalPeriodsClient.GetAllAsync | Fechas e IDs externos para selección live de carrera; PeriodsAsync | Migrado |
| Group / LoadExternalPeriodsAndDistributivosAsync | ExternalPeriodsClient.GetAllAsync | Periodos ordenados para cruces con Distributivos; PeriodsAsync | Migrado |
| DocumentRecognition / EnrichResearchersWithExternalDirectoryAsync | ExternalPeriodsClient.GetAllAsync | Periodos para la selección existente de carrera; PeriodsAsync | Migrado |
| ProjectsFilters / GetBootstrapAsync | Ya local | Selector de raíces activas con PK local | Corregido filtro jerárquico |
| ProjectFlatReport / GetFlatReportAsync | Ya local | Diccionario por FK local real del Project; no es selector | Conservado sin cambios |

[Tabla inicial completa](C:/Users/marlo/source/repos/TesisProject/docs/unified-services/local-catalog-consumers-initial.csv).
Los sitios legacy y las cuatro capacidades de contrato sin caller de negocio registrado
no se confundieron con llamadas Unified por migrar.

## B. ExternalAcademics

Eliminada la dependencia IExternalAcademicsService de UnifiedProjectService y su llamada
GetFacultiesAsync. La lectura usa IUnifiedUnitOfWork.Faculties/Query sin tracking.
**0 llamadas de negocio Unified a ExternalAcademics** en el alcance verificado.

## C. ExternalPeriods

Eliminadas IExternalPeriodsClient de Project, Group y DocumentRecognition, y las cuatro
llamadas GetAllAsync. Se reutiliza IUnifiedAcademicTermRepository mediante el helper
interno UnifiedAcademicCatalogReads, sin nuevo servicio DI ni repositorio.
**0 llamadas de negocio Unified a ExternalPeriods**.

## D. Faculty por consumidor

- **Import:** raíces con ExternalFacultyId válido; matching por nombre conservado y
  resolución a PK local mediante mapa. No se añadió IsActive al listado institucional.
- **CreateFull y coordinador de Group:** la afiliación/carrera se selecciona con datos
  live; su ID externo se resuelve en Faculty y se siguen ParentFacultyId locales hasta
  la raíz. Un movimiento del programa se refleja aunque Directory conserve el padre anterior.
  Se conserva el mismo nodo/carrera elegido; no se busca por nombre.
- **Recognition:** conserva FacultyCareerId externo en ResearcherInfo y valida los nodos
  seleccionados en un batch local; datos personales siguen viniendo de Directory.
- **Bootstrap:** ParentFacultyId null + IsActive. IDs devueltos: FacultyId local.
  Si todas las raíces están inactivas, lista vacía válida; si no hay raíces, catálogo no disponible.
- **Flat report:** se mantiene el diccionario de todos los nodos por FacultyId para resolver
  la FK almacenada, incluidos registros históricos. Filtrarlo por raíces perdería referencias.

FacultyRoots proyecta raíces y **hijos directos** a ExternalFacultyDTO/ExternalProgramDTO,
preservando la profundidad de esa proyección anterior. Los nietos siguen en el catálogo,
sin incorporarse artificialmente como programas directos. No hay nuevos endpoints de programas.

## E. AcademicTerm

Una consulta obtiene filas con ExternalPeriodId positivo y fechas presentes/coherentes,
ordenadas por inicio y ExternalPeriodId. Se proyectan a ExternalAcademicPeriodModel para
reutilizar los selectores existentes: **PeriodId sigue siendo externo**.
El import conserva similitud por nombre y elección del último ID externo como fallback
de etiqueta; esa regla opera exclusivamente sobre el snapshot local.
Los mapas separados ExternalPeriodId → AcademicTermId aseguran las FK locales de Visit.
Filas locales sin mapping/fechas no pueden sustituir un periodo institucional válido.

## F. Distributivos

Permanecen **3 sitios directos LIVE_OPERATIONAL**, todos GetDistributivosByCorreosAsync:
Project.PrepareFullProjectFacultyAsync, Group.LoadExternalPeriodsAndDistributivosAsync
y DocumentRecognition.EnrichResearchersWithExternalDirectoryAsync.
Reciben correos como antes; no existe en estos sitios un parámetro de periodo enviado.
El cruce de la respuesta utiliza PeriodId externo y CareerId/FacultyId externos.
La PK local nunca se envía ni se compara como PeriodId institucional.

## G. Directory

Permanecen **10 sitios directos**: Project 2, Group 6, DocumentRecognition 1,
ProjectFlatReport 1. Se conservan GetAllAsync/GetByEmailsAsync para perfiles,
afiliaciones, preparación de identidad y REPORT_ENRICHMENT. AppUser no se amplió ni
se utilizó como réplica de perfiles actuales.

## H. IDs comprobados

Pruebas con FacultyId **17** / ExternalFacultyId **500**, programa local **29** /
externo **800**, AcademicTermId **8** / ExternalPeriodId **20261**.
El selector live elige Software 800 cruzando 20261; Project prepara FacultyId 17 y
las visitas AcademicTermId 8. Tras mover Software al padre local 20 / externo 700,
Project y Group devuelven 20 aunque Directory siga declarando el padre externo 500.

## I. DTOs

**0 DTOs nuevos; 0 contratos request/response modificados.** Se reutilizan
ExternalFacultyDTO, ExternalProgramDTO, ExternalAcademicPeriodModel, ResearcherInfo,
ResolvedUserProfileDTO, KeyValueItemDTO y los DTOs de import existentes.
El helper puede transportar entidades internamente; ningún controller las expone.
ServiceResult<bool> es un resultado interno de enriquecimiento para propagar errores,
sin introducir un contrato HTTP.

## J. Fallback y errores

No hay fallback HTTP ante ausencia local. Se reutilizan FACULTY_NOT_SYNCHRONIZED y
ACADEMIC_TERM_NOT_SYNCHRONIZED, ErrorType.NotFound y mensajes Shared.
IDs malformados/no positivos siguen produciendo Validation/InvalidId.
Una referencia positiva ausente no permite saber si el proveedor la eliminó o falta
sincronizarla: el código informa indisponibilidad local, sin acusar dato inválido del usuario.

Recognition propaga el fallo de catálogo al resultado de reconocimiento; Group conserva
ErrorCode/ValidationErrors al retransmitir fallos del lector compartido. El fallback
existente de selección de carrera por perfil ante Distributivos vacío se conserva;
no equivale a consultar automáticamente los endpoints de catálogos.

## K. Queries y save ownership

ImportFromMatrix carga Faculty y AcademicTerm una vez y reutiliza diccionarios durante
todas las filas/visitas. La preparación aislada sin cachés usa lookup Faculty y un batch
de periodos; no una consulta por visita. FacultiesAsync de preparación pasó de un loop
de lookups a una query Contains, preservando deduplicación y orden del solicitante.
La resolución de ancestros carga el catálogo una vez, sin consultas por nivel.

Los helpers no insertan, sincronizan ni guardan. Los owners de negocio conservan sus
SaveChanges existentes. No se cambió Identity ni su frontera de persistencia.

## L. Services modificados

- [UnifiedProjectService](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedProjectService.cs)
- [UnifiedGroupService](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedGroupService.cs)
- [UnifiedDocumentRecognitionService](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedDocumentRecognitionService.cs)
- [UnifiedProjectsFiltersService](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedProjectsFiltersService.cs)
- Helper existente [UnifiedAcademicReferencePreparation](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedAcademicReferencePreparation.cs)
- Helper nuevo [UnifiedAcademicCatalogReads](C:/Users/marlo/source/repos/TesisProject/tesisproject.backend/Services/Unified/Implementations/UnifiedAcademicCatalogReads.cs)

UnifiedProjectFlatReportService ya resolvía la FK correctamente y permanece intacto.
No se añadió IUnifiedAcademicTermQueryService: el helper interno compartido satisface
la lectura sin otra abstracción DI. AddUnifiedDide tampoco cambió.

## M. Legacy

Servicios/repositorios legacy, AppDbContext y Program.cs intactos, verificados por SHA256
contra el inicio de esta fase. Runtime legacy sigue activo.

## N. Controllers

Ningún controller, ruta, contrato de servicio público ni DTO cambió. NonController permanece.
DI aislada con ValidateOnBuild/ValidateScopes pasa y todos los controllers resuelven.

**Límite de alcance explícito:** UnifiedExternalAcademicsController,
UnifiedExternalPeriodsController y UnifiedAcademicPeriodsController aún dependen directamente
de los clientes externos anteriores. Permanecen desactivados e intactos por la instrucción
de no modificar controllers. No se cuentan como llamadas de services de negocio ni se
afirma que toda la aplicación, incluido legacy, dejó de consultar esas APIs.

## O. Frontend

Sin modificaciones. No se hizo cutover ni rediseño de contratos.

## P. Articles

Sin modificaciones a lógica, controllers, DTOs, contextos, entidades ni integraciones
Articles. DW también permanece intacto. La fase solo modifica consumidores Unified Projects.

## Q. Migraciones

Sin migraciones nuevas ni aplicadas. 20260908025200_AddFacultyHierarchy, su designer y
snapshot permanecen exactamente como al inicio. La validación continúa offline/aislada.
Estas lecturas requieren el esquema jerárquico y su snapshot completo cuando se pruebe
posteriormente contra SQL Server; un ParentFacultyId null anterior al sync no prueba raíz remota.

## R. External calls after

[CSV final](C:/Users/marlo/source/repos/TesisProject/docs/unified-services/unified-external-calls-after-local-catalogs.csv):
13 llamadas BUSINESS_SERVICE_EXTERNAL_CALL conservadas (Directory 10 + Distributivos 3),
5 llamadas académicas eliminadas marcadas `no`, y 2 entradas SYNCHRONIZATION_EXTERNAL_CALL
separadas/excluidas del total de negocio. Son **sitios estáticos**, no frecuencia de ejecución.
catalog-sync-next-phase.csv marca las cinco sustituciones Unified como MIGRATED_LOCAL.

## S. Tests

`dotnet run --project tests/tesisproject.unifiedservicetests --no-restore`:
**17.265 assertions PASS**, sin reducir pruebas anteriores.

LocalCatalogConsumerTests usa la composición real con repositorios Unified sobre InMemory
y doubles de clientes académicos que cuentan y fallan si se llaman: ambos contadores quedan
en cero. Comprueba llamadas live, selección por periodo externo, roots/programs, movimiento,
metadata IsActive, referencias faltantes, ausencia de fallback, batch y reutilización de mapas.
Las pruebas previas de imports/CreateFull/visitas y ownership siguen pasando con fixtures
locales completos. Contadores de Query/lookup verifican la forma de acceso sin afirmar
que InMemory mida comandos SQL ni pruebe FK de SQL Server.

[Pruebas nuevas](C:/Users/marlo/source/repos/TesisProject/tests/tesisproject.unifiedservicetests/LocalCatalogConsumerTests.cs),
[salida](C:/Users/marlo/source/repos/TesisProject/artifacts/local-catalog-consumers/tests.log).

## T. Build

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
**0 errores, 39 warnings, 0 warnings nuevos**, contrastados con el log de la fase anterior.
Diff revisado con tratamiento CRLF y alcance comparado mediante hashes.

[Build](C:/Users/marlo/source/repos/TesisProject/artifacts/local-catalog-consumers/build-final.log),
[alcance](C:/Users/marlo/source/repos/TesisProject/artifacts/local-catalog-consumers/scope-check.json).

## U. Codebase Memory

Refresco full: **22.141 nodos / 91.068 aristas**. Generación final consultada
2026-09-08T03:21:35Z, metadata completa/coincidente. Trazas en ambas direcciones para
lectores Faculty/Terms, preparación CreateFull y loader Group; páginas relevantes completas.
Cobertura de 20 archivos y del scope Services/Unified/Implementations.
Huecos relevantes leídos en fuente: preparación línea 67, AcademicReferenceTests línea 167;
helper de errores de sync línea 16 verificado para el scope. Persiste metadata_changed tras
refresco; no se interpreta ausencia de huecos como garantía de completitud.

Caminos confirmados mediante receptores tipados, fuente, DI y tests:

`Project.Import → UnifiedAcademicCatalogReads → uow.Faculties/AcademicTerms →
repositorios Unified → GenericRepository<TEntity>(UnifiedDideDbContext)`.

`Group loader → PeriodsAsync → TermsAsync → AcademicTerms local`, manteniendo
`Group/Project/Recognition → GetDistributivosByCorreosAsync → cliente live`.

`CreateFull → selección live → RootForCareerAsync → Faculty local → ParentFacultyId`.

Las aristas heurísticas incorrectas hacia Budget.Query u otros módulos se descartaron.
La búsqueda de las antiguas dependencias/campos académicos dio cero en el grafo y se
contrastó con búsqueda de fuente en todas las implementaciones Unified; el único HttpClient
de ese scope pertenece al snapshot client administrativo. No se usó el grafo como única
evidencia de ausencia de llamadas.

Fase cerrada sin activar Unified ni iniciar integración SQL Server.
