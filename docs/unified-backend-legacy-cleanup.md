# Unified backend legacy cleanup

2026-09-09 — `UNIFIED_BACKEND_LEGACY_CLEANUP_READY`.

Cleanup transversal de implementaciones reemplazadas de Projects/Articles y sus dependencias operativas. No se cambia lógica Unified aprobada, frontend, DW, Staging ni configuración base de Articles.

## Eliminación y clasificación

Inventario por archivo y grupos conservados: [unified-backend-legacy-inventory.csv](unified-backend-legacy-inventory.csv). Contiene 208 entradas DELETE: 206 archivos backend y dos DTO sin consumidores activos.

- Backend: 49 archivos de controllers, 49 de services/helpers reemplazados, 32 de repositories, dos de UoW y 74 interfaces sin consumidores necesarios.
- Shared: `UpdateArticleRequest` y `ArticleImportResultDto`, sin referencias activas backend/frontend/Shared ni tests. No se elimina su archivo histórico archivado.
- DI: 75 registrations individuales y ocho registrations de ramas condicionales Articles retirados, además de `AddDbContext<ArticlesDbContext>`. Detalle: [unified-backend-legacy-removed-di.txt](unified-backend-legacy-removed-di.txt).
- No se borra ni altera ninguna migration Unified aplicada. No se crean migrations ni seeds en este cleanup.

Las eliminaciones se verificaron primero con Codebase Memory, proyecto `C-Users-marlo-source-repos-TesisProject`, nivel Verify. Generación consultada: `2026-09-09T20:03:19Z`. La cobertura indicó `metadata_changed`: las decisiones se contrastaron con fuente y búsquedas de referencias activas, excluyendo bin/obj y archivos archivados. Los huecos conocidos en los helpers Unified de referencias académicas y errores de sincronización se leyeron en fuente. El grafo no se considera prueba de ausencia por sí solo.

## Contextos y referencias conservadas

| Elemento | Clasificación y motivo concreto |
| --- | --- |
| `AppDbContext`, registration, bootstrap, DefaultConnection, factory y migrations App | KEEP_EXTERNAL_DEPENDENCY: `Services/Analytic/Implementations/DwEtlService.cs.cs:19,26` lo requiere como fuente operacional. Se conserva ese límite de dependencia sin revisar ni cambiar DW. Projects/Articles/Identity operativos no usan este contexto. |
| `Data/Articles/**`, `Migrations/Articles/**` | ARCHIVED_ALREADY: preservados como fuente histórica y excluidos de compilación mediante csproj en esta fase. Sin registration ni factory Articles. No se modifican migrations históricas. |
| `ArticlesMigration/**` | ARCHIVED_ALREADY: exclusión de compilación preexistente; contenido no auditado. |
| `ArticlesOltpConnection` | ARCHIVED_ALREADY: clave conservada como referencia histórica y para el comando explícito `BaselineSource`; no la consume la composición runtime. |
| `GenericRepository<T>` / `IGenericRepository<T>` | KEEP_SHARED_RUNTIME: base/contrato de repositorios Unified. El constructor protegido recibe UnifiedDideDbContext; el overload AppDbContext no tiene registration operativo. El probe verifica la instancia concreta de contexto de los 58 slots del UoW. |
| `IArticleConfigurationService`, `IArticleQueryService`, `IArticleRegistrationCommandService`, `IArticleUserContext`, `IRegistrationMatrixService` | KEEP_SHARED_RUNTIME: interfaces heredadas por sus equivalentes IUnified. No hay implementación ni binding legacy. Desacoplarlas ahora sería un cambio contractual innecesario. |
| `CurrentUserService`, `JwtTokenService` y sus interfaces | KEEP_SHARED_RUNTIME: helpers de claims/token reutilizados por Unified. No constituyen otra identidad. |
| ExternalAcademicsService, ExternalDirectoryClient, ExternalDistributivosService, ExternalPeriodsClient e interfaces | KEEP_EXTERNAL_DEPENDENCY: clientes externos todavía usados por Unified. |
| Shared Entities de AppDbContext | KEEP_EXTERNAL_DEPENDENCY: contexto todavía requerido por el consumidor DW; se conservan también tipos reutilizados por contratos. |
| `ArticleBiExportDto` | KEEP_EXTERNAL_DEPENDENCY por límite de alcance BI/reporting. No se encontró consumidor OLTP activo; no se afirma que esté en runtime. Se deja fuera de este cleanup para no alterar DW/reporting. |
| DTO y wrappers compartidos | KEEP_FRONTEND_CONTRACT / KEEP_SHARED_RUNTIME según consumidores en CSV y reporte frontend. |
| ArticlesModuleOptions, policies y mappings Unified | KEEP_SHARED_RUNTIME: siguen controlando acceso y operación Unified. |

Las coincidencias textuales con nombres eliminados que quedan son comentarios/logs (`ProjectsController`, `ProjectMatrixService`, `ProjectService`, `ProjectFlatReportService`), segmentos del namespace `UnitOfWork.Unified` o clases privadas homónimas como `MemberResolvedInfo` y `ColumnDef` definidas dentro de implementaciones Unified. No son enlaces a los tipos eliminados. Las interfaces compartidas anteriores son deliberadas; `UnifiedArticleRegistrationCommandService` importa Services.Interfaces para `IExternalDirectoryClient`.

## Contratos frontend

Reporte requerido: [frontend-merge-impacts.md](frontend-merge-impacts.md). Incluye consumidores activos, IDs Product/Article/ProductAuthor, ProductTypeId explícito, ProjectId nullable, IsProjectResult derivado, resolución de participantes, formularios/matrices y auth/me.

El snapshot `tests/controller-contracts-before-cleanup.json` se capturó antes de borrar controllers legacy y fija el contrato de los controllers Unified aprobados. Los tests comparan rutas, DTO, binding, defaults y autorización contra ese snapshot, además de ejecutar acciones y verificar ServiceResult/status codes. Se actualizaron fixtures que asumían 50 repositorios, solo repositorios genéricos de catálogo o la presencia compilada de ArticlesDbContext; ahora verifican 58 slots, genéricos correctos y ausencia del contexto archivado.

## Verificación ejecutada

| Verificación | Resultado |
| --- | --- |
| Solution build Release, SDK 9.0.318, `--no-restore` | PASS, 0 errores, 21 warnings; no cambios de frontend para resolver warnings |
| Unified services / HTTP / DI | PASS, 14.347 aserciones; ValidateOnBuild/ValidateScopes, 1.608 escenarios HTTP en la suite transversal |
| Articles controllers | PASS, 504 checks de acciones, rutas, permisos, errores, contratos y descubrimiento Unified |
| Articles configuración SQL | PASS, 46 checks |
| Articles write SQL | PASS, 31 checks; rollback agregado e Identity conservada, autorías, dynamic fields, DOI y view |
| Articles services SQL | PASS, 22 checks; ownership/CanManageAll, Draft CRUD, identidad/conflictos, registro/lectura |
| Auth/me SQL + HTTP | PASS, 18 checks |
| Articles read SQL | PASS, 26 checks |
| Articles persistencia SQL | PASS, 17 checks |
| Articles hardening SQL | PASS, 12 checks; migrations desde cero y unicidad de DOI/definitions |
| Modelo Unified | PASS, 187 checks; 82 entidades/tablas más ArticleReadView keyless, 110 FK, 140 índices |

Las suites SQL crean bases temporales únicas y las eliminan al terminar. El primer intento SQL dentro del sandbox no pudo conectar; se repitieron con acceso local autorizado y pasaron. No se cambió la BD de aplicación para acomodar tests.

### Runtime real contra tesis_unified

`RuntimeProbe` inicia Kestrel con la configuración y métodos reales de StartupExtensions, DI/pipeline/controllers y SQL de aplicación; no ejecuta bootstrap ni ETL DW. Valida 52 controllers totales (51 Unified más el controller analítico conservado), 301 endpoints y **cero duplicados**. Articles: cuatro controllers Unified, 52 rutas. Repositorios/UoW/Identity resuelven la misma instancia scoped de UnifiedDideDbContext.

- GET auth/me anónimo: 401.
- Login: 200; GET auth/me autenticado: 200.
- GET Projects: 200.
- Forms y catálogos administrativos Articles: 200.
- Formulario resuelto sin configuración: 404 `CONFIG_FORM_NOT_FOUND`.
- Listado Articles: 200; detalle inexistente: 404 `ARTICLE_NOT_FOUND`.
- Listado de matrices Articles: 200.

Historial leído de aplicación: InitialUnifiedDide, AddFacultyHierarchy, AddArticleReadView, HardenArticleReadModel. Cero migrations pendientes; ninguna aplicada por este cleanup.

El probe aprovisionó una cuenta de prueba identificada como `articles-cutover-*` con rol superadmin para login/permisos; quedó registrada en `artifacts/articles-runtime/fixture.json`, sin contraseña/token. Evidencia HTTP y rutas en esa carpeta; logs de suites y build en `artifacts/cleanup-*.log`.

**NOT_EXECUTED:** smoke completo de registro Articles con datos reales A/B/C. No existe configuración/base suficiente de Articles. El usuario aceptó expresamente que esto no bloquea cleanup. Las pruebas aisladas funcionales usan sus propios fixtures temporales; no se inventan datos, seeds o formularios para tesis_unified.

No hay blockers reales de este cleanup. La dependencia AppDbContext del límite DW se conserva por alcance, y los ajustes frontend se documentan para otra fase.
