# Products Unified — cierre funcional, 2026-09-06

## Alcance y estado

Se resuelven los siete registros ProductService de pending.csv: CreateAsync,
UpdateAsync, GetByIdAsync, ListAsync, DeleteAsync, SyncAuthorsAsync y MapToDetailDTO.
DeleteAsync también estaba marcado como dependiente en fuente y en pending.csv,
aunque el informe inicial lo describía como operativo. Quedan **0 TODO Product**
y **21 TODO de otros dominios**, sin resolver ni modificar sus decisiones.

Los informes anteriores conservan el estado histórico. Esta sección sustituye
únicamente las conclusiones de bloqueo de Products. Los tres FIX_NOW ya corregidos
no se han vuelto a modificar. No se activa runtime ni se cambian Controllers,
Identity, Articles, Faculty, AcademicTerm, imports, provisión de grupos, repositories,
UnitOfWork, configuraciones EF ni migraciones.

## Contratos y consumidores examinados antes de editar

Codebase Memory encontró ProductAuthorResponseDTO usado por el mapper legacy y
ProductDetailResponseDTO; su traza inbound de dos saltos devolvió cinco resultados
sin paginación pendiente. La búsqueda literal de los cuatro DTO Product y
AuthorUserIds en backend, frontend, shared y tests complementó el grafo.

ProductsController publica POST api/products, GET detalle/listado/by-project,
PUT y DELETE; usa IProductService legacy. ProductClientService transporta esos
contratos. ProjectProductsEditor y VisitDetailPanel crean productos con el
ProjectId de su componente y AuthorUserIds vacío; al editar envían autores null.
ProjectDetail y ProjectFinalizationTab transportan el listado. No se encontró un
consumidor de UserId de ProductAuthorResponseDTO en esos componentes, tests o
exportadores. Los DTO de reportes/exportación de Projects son otros contratos.
Los archivos Razor se comprobaron por fuente: la cobertura MCP los marca
not_tracked, por lo que no se presenta el grafo como prueba exhaustiva de Razor.

Se amplía el DTO compartido para mantener el productor legacy aún activo:

| Campo | Institucional Unified | Externo Unified | Legacy actual |
|---|---|---|---|
| AuthorId: int? | Author.AuthorId | Author.AuthorId | null: no existe esa identidad en legacy |
| AuthorType | Institutional (1) | External (2) | Institutional (default) |
| AppUserId: int? | AppUser.IdUser | null | IdUser |
| UserId: int? | alias de AppUserId | null | alias compatible |
| ExternalResearcherId: int? | null | ExternalResearcherId | null |

AuthorId siempre tiene valor en respuestas Unified. Su nulabilidad evita inventar
un AuthorId para el servicio legacy. UserId y AppUserId comparten la misma propiedad
subyacente; no se usan IDs externos en el alias. Se conservan CreatedAt/UpdatedAt
y se añaden los campos reales AuthorOrder, Participation, IsPrimaryAuthor y los
snapshots ParticipantType, Affiliation, Name, Identification y Email. La salida
ordena por AuthorOrder, colocando null al final y usando Id de vínculo como desempate.
Los snapshots no se sustituyen por datos actuales. AppUser no contiene nombre ni
email: no se introduce una consulta Identity para rellenarlos.

ExternalAuthorId permanece intacto en el modelo y fuera del DTO: compatibilidad
legacy Articles, no tercera fuente. No se añaden campos dinámicos de Articles.

DTO modificados: ProductCreateRequestDTO, ProductUpdateRequestDTO (comentario
contractual), ProductDetailResponseDTO, ProductListItemResponseDTO y
ProductAuthorResponseDTO. Se añade ProductAuthorType en shared/Enums.

## Escritura institucional y validaciones

AuthorUserIds conserva su significado AppUser.IdUser. Se omiten IDs no positivos
como en legacy y se deduplican los positivos. Primero se valida la existencia de
todos los AppUser; después se busca Author por AppUserId y se repite la consulta
si falta, antes de preparar Author { AppUserId, ExternalResearcherId = null }.
Si aparece entre consultas, se reutiliza. Las nuevas identidades se preparan
sin tracking hasta que todas las validaciones finalizan.

El service usa exclusivamente la UoW inyectada; no llama al CRUD Author que
confirma por sí mismo. Authors.AddAsync prepara altas sin save; ProductAuthors
usa AuthorId para autores existentes y navegación Author para los nuevos.
Las consultas Author existentes son AsNoTracking: sus instancias NO se pasan
a Add, evitando reinsertar identidades existentes. EF propaga las claves nuevas.
Los constraints XOR y únicos de Author y ProductAuthor se conservan como defensa
ante concurrencia; una carrera en el save sigue siendo PersistenceConflict.

Los errores reutilizan AppUser.NotFound, Author.ExactlyOneSource con
ValidationErrors, Project.NotFound y los códigos existentes de Product/Common.
ServiceResult, ErrorType, ErrorCode y el relay de ValidationErrors permanecen.

null en Update no cambia autores; una lista suministrada sustituye todo el
conjunto, y vacío elimina todos los vínculos, sin borrar Author. Los vínculos
retenidos conservan orden, participación, flags, snapshots y fechas.
Esto también significa que una lista explícita elimina externos que no puede
seleccionar. La lectura externa está soportada; **la selección/escritura externa
todavía no está soportada**, porque AuthorUserIds no se reinterpreta. Esta
limitación del contrato se documenta y no abre la migración de Articles.

## ProjectId y creación genérica

ProjectId es int? en creación, detalle y listado: null representa producción
independiente, sin 0/-1 artificiales. Unified valida existencia de Project solo
cuando se proporciona un ID; valores no positivos fallan la validación.
ListByProjectAsync sigue filtrando por ProjectId == projectId. Update no tenía
campo ProjectId y mantiene esa semántica: no permite reasignar proyectos.

POST api/products es el contrato genérico, aunque los dos editores de Projects
lo usen con proyecto obligatorio. Esos editores conservan su comportamiento.
El productor legacy aún activo recibió únicamente la adaptación imprescindible
al DTO nullable: rechaza null y usa Value después de validarlo, pues su entidad
requiere int. La producción independiente queda operativa en Unified; la ruta
runtime sigue usando legacy hasta una futura fase DI/Controllers.

## Atomicidad

| Owner | Referencia legacy antes | Unified después |
|---|---:|---:|
| CreateAsync | 2 saves | 1 save |
| UpdateAsync | 1 save, con mutación anterior a validar values | 1 save, validación anterior a mutar |

En Unified estos métodos estaban ausentes/bloqueados; los números «antes»
corresponden a sus cuerpos legacy conservados, no a ejecuciones Unified previas.
SyncAuthorsAsync y SyncValuesAsync no hacen SaveChanges. Create construye Values
y Authors y agrega el agregado; Update carga el agregado rastreado, valida values
y autores, sincroniza vínculos/values y finalmente modifica los escalares.
No se llama Update sobre todo el grafo rastreado ni se cargan copias detached de
los vínculos para eliminarlas. Delete usa también el agregado rastreado.

Fallo de validación/consulta anterior a aplicar: cero saves y tracker intacto.
Fallo durante Add/Remove o el save: descartar el scope; puede contener cambios
pendientes. El save incluye todos los cambios de su contexto, de modo que no
debe compartirse con cambios ajenos pendientes. Se preserva la recarga posterior:
un fallo de recarga tras guardar no equivale a rollback del commit.

## Verificación

ProductTests utiliza repositories de lectura instrumentados y el modelo/tracker
real UnifiedDideDbContext con configuración SQL Server. Comprueba altas con Author
existente/faltante, reconsulta, varios autores nuevos, duplicados, valores duplicados,
conservación/remoción de vínculos, null/vacío, IDs distintos (41/42/43 frente a
901/905 y externo 82), ambos mappings, snapshots, JSON legacy/externo, ProjectId
con valor/null, List/ByProject/Get/Delete, errores de fuente/usuario/proyecto/values,
fallo de consulta/aplicación, cancelación y conflicto en el único save final.

Se comprueban las claves temporales de ProductAuthor y ProductValue con sus
principales antes del save, y que no se inserten AppUser, Project o catálogos
existentes. Las lecturas de Author simulan AsNoTracking. Los saves de la prueba
asignan claves y aceptan cambios localmente; no conectan a una BD. Se verifica
traducción SQL de la consulta real con ambas fuentes, sin afirmar pruebas de
commit/rollback relacional ni carreras concurrentes contra una BD desplegada.

Resultados finales:

- `dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`:
  **0 errores, 39 warnings, 0 warnings nuevos**.
- `dotnet run --project tests/tesisproject.unifiedservicetests --no-restore`:
  **PASS, 3613 aserciones** del conjunto completo; ejecución final sin warnings
  de los tests. El número incluye las comprobaciones estructurales preexistentes,
  no representa 3613 escenarios de Products.
- Comparación SHA-256 de todos los archivos existentes al inicio: los únicos
  cambios de aplicación son UnifiedProductService/IUnifiedProductService, los
  cinco DTO indicados y las dos adaptaciones de nulabilidad del ProductService
  legacy. Program.cs de tests añade la ejecución ProductTests y actualiza la
  aserción que antes exigía ausencia de los métodos Product. Los repositories,
  UoW, configuraciones, dominios excluidos y los tres FIX_NOW conservan sus hashes.
- Los nuevos archivos son ProductAuthorType.cs, ProductTests.cs y este informe.
  Los informes existentes reciben anexos; no se borra su histórico.

## Codebase Memory

Al inicio list_projects encontró el proyecto
`C-Users-marlo-source-repos-TesisProject`, index_status respondió ready y se
reutilizó su índice. No hubo Transport closed. Nivel de evidencia: Verify.

El refresco automático actualizó el proyecto después de los cambios. La petición
explícita de index_repository encontró otro indexado activo con distintas opciones;
no se forzó un indexado concurrente. La generación posterior consultada es
**2026-09-07T04:45:24Z**, modo full, recording_status complete y generation_matches
true. index_status respondió ready (20952 nodos / 82250 aristas en esa consulta).

Las trazas completas de un salto muestran Create/Update como callers de
ResolveAuthorsAsync y SyncAuthorsAsync; USAGE confirma las propiedades de
IUnifiedUnitOfWork Products/Authors/AppUsers/ProductAuthors y SaveChangesAsync.
Los snippets actuales de SyncAuthorsAsync, MapToDetailDTO y UnifiedProductRepository,
la fuente del modelo/configuraciones y las pruebas verifican:

```text
UnifiedProductService -> IUnifiedUnitOfWork
                         Products / ProductAuthors / Authors / AppUsers
Product.Authors -> ProductAuthor.Author -> Author.AppUser / ExternalResearcher
```

Límites: check_index_coverage no registra gaps de parseo en los archivos cambiados,
pero sigue indicando freshness=metadata_changed; Razor indica not_tracked.
Se usó fuente directa y pruebas como respaldo, sin equiparar «sin gap registrado»
a completitud. Algunas aristas CALLS son heurísticas incorrectas: AddAsync/Remove
resuelven a ExportField y GetByAppUserIdAsync al service Author homónimo. Se
descartan por el receptor tipado `_uow.Authors`/`_uow.ProductAuthors`; no se afirman
llamadas a esos services ajenos ni un call graph completamente resuelto.

La fase termina aquí; la escritura externa, runtime y los demás dominios quedan
fuera de esta entrega.
