# Infraestructura paralela de repositories Unified

La capa paralela quedó creada y compilada, sin activarse en runtime:

```text
LEGACY                                  UNIFIED
AppDbContext                            UnifiedDideDbContext
Repositories existentes                Repositories/Unified
IUnitOfWork + UnitOfWork                sin UnitOfWork todavía
Services y Controllers actuales         sin consumidores todavía
```

No se modificaron Program.cs, DI, IUnitOfWork, UnitOfWork, Services, Controllers, DTOs, frontend, migraciones ni la base de datos.

## A. Inventario

El inventario completo contiene 34 implementaciones existentes:

| Clasificación | CREATED | SPECIAL | PENDING | LEGACY_ONLY |
| --- | ---: | ---: | ---: | ---: |
| DIRECT | 30 | 0 | 0 | 0 |
| ADAPTABLE | 0 | 0 | 1 | 0 |
| SPECIAL | 0 | 3 | 0 | 0 |

Los 30 `CREATED` incluyen 28 repositories concretos, `UnifiedCatalogRepository<T>` y la reutilización de `GenericRepository<T>`. No apareció un repository activo `LEGACY_ONLY`; DW está implementado mediante servicios/contexto y el código archivado de ArticlesMigration está excluido, por lo que no se duplicó.

La tabla exacta está en [inventory.csv](inventory.csv).

## B. Repositories Unified creados

Se crearon **29 interfaces y 29 implementaciones**:

- `Repositories/Unified/Interfaces/IUnified*Repository.cs`
- `Repositories/Unified/Implementations/Unified*Repository.cs`

Todas las implementaciones reciben `UnifiedDideDbContext`. Las que heredaban de `GenericRepository<T>` conservan esa herencia. Los repositories legacy que implementaban directamente su almacenamiento —Budget y Export— conservaron la misma forma para no alterar sus queries ni su tracking. `UnifiedCatalogRepository<T>` reutiliza `GenericRepository<T>` y restringe `T` al `CatalogEntityBase` Unified.

El listado interfaz → implementación → entidades está en [created.csv](created.csv).

## C. Repositories SPECIAL

| Tema | Repository | Bloqueo exacto |
| --- | --- | --- |
| ProductAuthor | `ProductAuthorRepository` | `ExistsForUserAsync(productId, userId)` compara `ProductAuthor.UserId`; Unified necesita `AuthorId`. Copiar o renombrar el parámetro produciría una equivalencia falsa. |
| Articles / ArticleParticipant | `ArticleReadRepository` | Usa `ArticlesDbContext`, `Article.Title`, `Article.Participants`, campos dinámicos e includes del agregado Articles. La nueva estructura requiere Product + Article + Author + ProductAuthor. |
| Product | `ProductRepository` | `GetByProjectAsync` tolera `ProjectId` nullable, pero `GetByIdWithRefsAsync` y `QueryWithRefs` incluyen `Product.Authors`. Se dejó `ADAPTABLE/PENDING` para conservar una interfaz completa y coherente. |
| Identity | `AspNetUserRepository` | Opera directamente `IdentityUser<int>` y el store actual de Identity pertenece a `AppDbContext`. No es una entidad Unified de negocio ni se migró DI. |
| Faculty external/local | — | Los repositories creados trabajan con `FacultyId` local. No se añadió resolución `ExternalFacultyId → FacultyId`; los consumidores permanecen legacy. |
| AcademicTerm external/local | — | `UnifiedVisitRepository` no usa `AcademicPeriodId` ni recibe un ID externo. No se añadió resolución `ExternalPeriodId → AcademicTermId`. |

No se creó un esqueleto de ProductAuthor porque una interfaz sin semántica acordada sería un contrato engañoso. Tampoco se duplicaron repositories de catálogos Articles que no tenían un repository legacy independiente en el inventario actual.

## D. Cambios legacy restaurados

Volvieron a `AppDbContext` y a sus entidades legacy:

- ConvocationRepository.
- ExportFieldRepository.
- ExportTemplateColumnRepository.
- ExportTemplateRepository.
- ObjectiveActivityUserRepository.
- ProductAttributeDefinitionRepository.
- VisitIssueRepository.
- VisitObjectiveActivityProgressRepository.
- AppConfigurationRepository.

También se restauraron sus interfaces y todos los aliases Unified que la pasada anterior había añadido a Services. Cada uno tiene ahora una versión paralela `Unified...Repository`.

`ProjectObjectiveRepository` y `VisitObjectiveActivityProgressRepository` conservan cinco accesos equivalentes mediante `_ctx.Set<T>()`. Esto es necesario porque el campo base de `GenericRepository<T>` ahora es `DbContext`; sus constructores y entidades siguen siendo legacy. No contienen `UnifiedDideDbContext` ni `UnifiedEntities`.

## E. GenericRepository

No se creó `UnifiedGenericRepository<T>`. La misma implementación admite:

```csharp
public GenericRepository(AppDbContext ctx) : this((DbContext)ctx) { }
protected GenericRepository(DbContext ctx) { ... }
```

Conserva `FindAsync`, filtros `Expression`, `AsNoTracking`, `Add`, `AddRange`, `Update`, `Remove`, `RemoveRange`, conteo y `Query` con tracking opcional. La validación por reflexión construyó los 29 tipos Unified con una única instancia de `UnifiedDideDbContext` y comprobó la herencia genérica y contratos.

## F. Grafo

Se usó Codebase Memory sobre `C-Users-marlo-source-repos-TesisProject` con nivel Verify. El índice final contiene **18.957 nodos y 68.496 aristas**.

Las consultas localizaron 34 clases e interfaces legacy, sus bases, campos y las 561 propiedades de `UnifiedEntities`. Los snippets confirmaron las fronteras materiales:

- `ProductRepository.QueryWithRefs` y `GetByIdWithRefsAsync` incluyen `Authors`.
- `ProductAuthorRepository.ExistsForUserAsync` usa `UserId`.
- `ArticleReadRepository` incluye `Participants` y usa `ArticlesDbContext`.
- `UnifiedAppUserRepository` conserva por separado `IdUser`, `IdLocal` e `IdAsp`.
- `UnifiedVisitRepository` no usa el período académico.

Después de reindexar, el grafo encontró exactamente 29 clases `Unified*Repository`; los únicos campos de contexto declarados en ellas son `UnifiedDideDbContext`. `check_index_coverage` no registró huecos en las 58 rutas ni en el scope Unified, aunque informó `metadata_changed`; por eso la afirmación negativa se comprobó además leyendo todos los archivos y con reflexión sobre el ensamblado compilado.

## G. Build

La reconstrucción final de toda la solución (`-t:Rebuild`) produjo:

```text
0 errores
39 warnings
```

Es el mismo resultado del baseline de reconstrucción: 39 warnings y 0 errores. La compilación incremental produjo 19 warnings, también iguales a su baseline incremental. No se corrigieron warnings preexistentes. La prueba aislada de arquitectura terminó correctamente y no abrió conexiones, ejecutó queries, migraciones ni `SaveChanges`.

## H. Siguiente fase

[next-uow.csv](next-uow.csv) enumera **45 propiedades posibles** para el futuro `IUnifiedUnitOfWork`: 28 contracts concretos ya creados y 17 instancias tipadas de `IUnifiedCatalogRepository<T>`.

La siguiente fase puede crear `IUnifiedUnitOfWork` y `UnifiedUnitOfWork` exclusivamente con esos contracts, usando una sola instancia Scoped de `UnifiedDideDbContext`. Deben permanecer fuera hasta resolverlos: Product, ProductAuthor, Articles y AspNetUser. DI y `SaveChangesAsync` tampoco se implementaron aquí.

## Actualización de la fase Product/Author

La clasificación y los conteos anteriores registran el cierre de aquella fase. En
la fase posterior se resolvieron Product y ProductAuthor mediante la abstracción
Unified `Author`, y se crearon sus tres pares de repository. `next-uow.csv` contiene
ahora 48 propiedades: 31 repositories especializados y 17 catálogos. Solo Articles
y AspNetUser continúan fuera de la UoW Unified.
