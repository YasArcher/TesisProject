# Corrección de atomicidad interna Unified — 2026-09-06

## Resultado y alcance

Tres riesgos internos resolubles pasaron de FIX_NOW a FIXED. Se modificaron cuatro
services Unified y se añadió un componente interno de preparación de atributos.
Se preservaron las 38 interfaces, sus contratos y los 28 TODO. No se activó runtime.
La comparación SHA-256 contra la captura al inicio de esta fase detectó cambios
solo en esos cuatro services entre los archivos previamente existentes de Services,
Repositories, UnitOfWork, Data, Controllers, Program.cs y shared/Errors; pending.csv
conserva exactamente su contenido. El trabajo pendiente de git anterior permanece.

## A. Riesgos revisados

Se reutilizaron las diez filas de commits parciales y cuatro fronteras externas
del informe anterior. No se levantó otro inventario. Identity/roles se considera
parte de las cinco filas con provisión, sin duplicar el conteo.

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

| Clasificación | Antes | Después |
|---|---:|---:|
| FIX_NOW pendiente | 3 | 0 |
| FIXED | 0 | 3 |
| BLOCKED_BY_LOGIC | 2 | 2 |
| IDENTITY_BOUNDARY | 5 | 5 |
| EXTERNAL_SIDE_EFFECT | 4 | 4 |

Son 14 entradas del informe acotado; los once riesgos fuera de FIX_NOW permanecen.
No son un conteo exhaustivo de todos los métodos de la solución.

## B–D. Cambios, owners y Service → Service

| Operación pública/owner | Saves antes (éxito) | Saves después (éxito) |
|---|---:|---:|
| UnifiedProductTypeDesignService.SaveDesignAsync | N + 2 | 1 |
| UnifiedConvocationService.CreateAsync | 2 | 1 |
| UnifiedExportTemplateService.CreateTemplateAsync | 2 | 1 |
| UnifiedProductAttributeService.CreateAsync autónomo | 1 | 1 |
| UnifiedProductAttributeService.UpdateAsync autónomo | 1 | 1 |

N es el número de atributos enviados. No hay transacciones explícitas nuevas ni
flags `deferSave`. Cada flujo obtiene sus repositorios de la única UoW inyectada.

**Diseño:** `PrepareProductTypeAsync` valida sin mutar la entidad rastreada;
`UnifiedProductAttributePreparation` prepara las altas/ediciones de atributos;
se consultan definiciones y referencias en uso y se construyen cambios separados.
Solo después se aplican altas, ediciones y eliminaciones y guarda el owner.
Las navegaciones enlazan tipos/atributos nuevos con definiciones antes de disponer
de IDs persistidos. Se conserva el último mapeo de un Id repetido, incluido cero;
se retienen definiciones en uso y se omiten IDs de definición desconocidos.

La llamada a `IUnifiedProductAttributeService.CreateAsync/UpdateAsync` con commit
se reemplazó por preparación interna compartida. El constructor de diseño recibe
ahora solamente IUnifiedUnitOfWork, eliminando la dependencia del CRUD autónomo.
No se agregaron métodos de preparación a las interfaces de aplicación. Ambos
CRUD públicos reutilizan el mismo validador y siguen guardando por sí mismos.
Se preservan nombre requerido, duplicado, ID inválido, inexistencia y bloqueo,
así como sus mensajes/códigos. El owner de diseño mantiene su envoltura de error
ErrorSavingProductAttribute y ValidationErrors[Attributes].

Los nombres preparados participan en las siguientes validaciones de la misma
solicitud: se excluyen de la consulta los valores antiguos de las filas editadas
y se comprueban sus valores preparados y las altas. La comparación de nombres
pendientes pasa por el proveedor SQL, incluyendo catálogo vacío, sin imponer una
comparación ordinal/case-insensitive nueva en memoria. Los cambios preparados no
se almacenan entre solicitudes. Se verificó traducción SQL Server sin conectar;
no se ejecutó una prueba de collation contra una base real.

**Convocatoria:** se consultan las convocatorias activas rastreadas antes de añadir
la nueva. Se desactivan las anteriores y se inserta la nueva activa en un save.
SetActiveExclusiveAsync no cambia; este flujo ya no necesita consultarlo después
de generar un ID. Se conserva la regla de activación existente y los errores.

**Plantilla:** BuildColumnsEntitiesAsync se ejecuta sobre objetos separados antes
de añadir la cabecera. Columns/Template enlazan el agregado para que EF propague
claves en un save. Se preservan orden, defaults, trimming y omisión con log de
FieldId desconocido; no se transforma esa omisión en una validación nueva.
No se añaden al contexto los ExportField consultados. La recarga final permanece.

## E. Límites que permanecen

- BLOCKED_BY_LOGIC: Product/autores/DTO y Faculty external/local. AcademicTerm
  external/local continúa pendiente donde participa en imports/reportes.
- IDENTITY_BOUNDARY: provisión de AppUser/Identity y roles en Project, Group y
  asignaciones. No se implementó Ensure ni se reinterpretaron identificadores.
- EXTERNAL_SIDE_EFFECT: Document Upload/Replace/Delete y consultas HTTP del
  diagnóstico. No se cambiaron compensaciones, filesystem, outbox ni reintentos.

Los 28 TODO se conservaron byte por byte en pending.csv y sin cambios en sus
services. Articles, Identity/AspNetUser, Controllers y DI runtime quedan fuera.

## F. Pruebas y estado del tracker

`dotnet run --project tests/tesisproject.unifiedservicetests --no-restore`

Resultado: **PASS, 3339 assertions** del conjunto completo. Este número incluye
las comprobaciones estructurales existentes; no representa 3339 casos de atomicidad.
Las nuevas regresiones están en AtomicityTests.cs y cubren:

- Éxito con un save y fallo temprano/secundario con cero saves y cero Add/Update/Remove
  para cada uno de los tres owners.
- Diseño: segundo atributo inválido, bloqueado, ausente o duplicado; fallo de consulta
  de definiciones; valores previos intactos; validación de tipo y campos de error.
- Renombres secuenciales, nombres liberados, actualizaciones repetidas, Id cero,
  referencias generadas, retención en uso y eliminación de definiciones no usadas.
- CRUD autónomo de atributos: un save por alta/edición y errores sin saves adicionales.
- Convocatoria: exclusividad sobre activas/inactivas y normalización.
- Exportación: orden/defaults/trimming, campos desconocidos y fallo en carga de campos
  sin cabecera añadida.
- Fallo del save final: un único intento, mapeos de error/propagación preservados.
- Tracker real de UnifiedDideDbContext: fallo secundario sin HasChanges; claves
  temporales y relación de definición existente hacia atributo nuevo antes del save.
- Traducción SQL Server de la comparación de nombres preparados sobre catálogo vacío.

Los repositorios/UoW de las pruebas son dobles instrumentados. Se usa el modelo y
tracker EF reales y traducción SQL sin abrir conexión. No se prueba contra una BD
real el commit/rollback, constraints, collation desplegada ni carreras concurrentes.

Preparación deja intactas las entidades rastreadas. **Después de un fallo durante
Apply/Add/Update/Remove o SaveChanges, descartar el scope completo**: pueden quedar
cambios pendientes, y capturar la excepción no los revierte. No se añadió Clear
al tracker, que podría borrar cambios de otros owners. Tampoco debe iniciarse una
operación con cambios ajenos pendientes: SaveChanges los incluiría. Una recarga
fallida después del commit puede devolver error sin revertir la escritura.

## G. Build

`dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`

Resultado: **0 errores / 39 warnings / 0 warnings nuevos** frente a la baseline.
El backend aislado también compiló con 0 errores y sus 19 warnings preexistentes.
No se modificaron warnings legacy para ocultar el resultado.
`git diff --check` reportó whitespace preexistente en GenericRepository,
ProjectObjectiveRepository y VisitObjectiveActivityProgressRepository; sus hashes
coinciden con el inicio de esta fase y no se alteraron. La comprobación separada
de los archivos de código editados/añadidos en esta fase no encontró whitespace
al final de línea.

## H. Codebase Memory y evidencia

Se intentaron index_status, búsqueda, list_projects y check_index_coverage para
C-Users-marlo-source-repos-TesisProject. El transporte MCP respondió **Transport
closed**. Al cierre fallaron también check_index_coverage de los archivos cambiados
e index_repository(mode=full). No se afirma refresco exitoso, generación vigente
ni call graph verificado por MCP. La verificación del grafo queda pendiente de
restablecer el servidor.

Rutas verificadas mediante lectura directa y pruebas compiladas, como fallback:

- SaveDesignAsync → PrepareProductTypeAsync / preparación de atributos / consulta
  de definiciones → aplicación → IUnifiedUnitOfWork.SaveChangesAsync (1) → GetDesignAsync.
- ProductAttribute CreateAsync/UpdateAsync → preparación compartida → ApplyAsync
  sin commit → SaveChangesAsync (1 por operación autónoma).
- Convocation CreateAsync → Query de activas → Add + desactivación → SaveChangesAsync (1).
- ExportTemplate CreateTemplateAsync → BuildColumnsEntitiesAsync → Add del agregado
  → SaveChangesAsync (1) → recarga de detalle.

La fuente de los repositorios y UoW confirma el contexto compartido y que estas
llamadas de repositorio no guardan. La configuración futura de scopes runtime
sigue pendiente; esta fase no la activa ni la certifica.

Se detiene aquí la fase; no se inicia la siguiente migración.

## Actualización posterior: refresco por la interfaz local

El reintento solicitado por el usuario se completó mediante la API utilizada por
http://localhost:9749: POST /api/index para el mismo proyecto y repositorio.
GET /api/index-status devolvió status=done sin error; los logs registraron
ui.index.done rc=ok y salida del indexador con código 0.
La salud posterior es healthy, con 20 874 nodos y 81 442 aristas (antes: 20 791 y
80 958). Por tanto, el refresco del índice ya no está pendiente.

La conexión MCP de esta sesión continúa devolviendo Transport closed. La API RPC
de la interfaz no permite index_status (HTTP 403); no se intentó sortear esa
restricción. Este refresco no sustituye la verificación detallada de cobertura y
call graphs, que permanece pendiente por MCP. No se modificó código de aplicación.

## Actualización posterior: Products funcional

La entrada Product.CreateAsync deja BLOCKED_BY_LOGIC: autoría y DTO resueltos,
un save por agregado. Los tres FIX_NOW anteriores permanecen sin modificaciones.
Ver [products-functional-report.md](products-functional-report.md) para esta fase.
