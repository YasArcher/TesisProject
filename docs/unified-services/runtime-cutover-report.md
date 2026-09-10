# Projects Legacy → Unified: runtime cutover

Fecha: 2026-09-07 (America/Guayaquil). **PROJECTS_UNIFIED_RUNTIME_ACTIVE**.
Actualización 2026-09-08: completadas las pruebas aplazadas, incluido CreateFull con Directory/Distributivos LIVE. **READY_FOR_LEGACY_CLEANUP**. Legacy preservado; no se inició limpieza.

| Área | Resultado |
|---|---|
| Program / DI | PASS: `AddUnifiedDide`, sin duplicar su Identity, clientes HTTP ni servicios compartidos |
| UnifiedDideDbContext / UoW / repositories | ACTIVE, scoped; 50 repositorios comparten el contexto Unified |
| Identity | ACTIVE: UserManager, RoleManager, SignInManager y un único par de stores EF sobre Unified |
| DB runtime | `Archer\DINNOVA` → `tesis_unified`, Windows authentication |
| Migraciones | PASS: InitialUnifiedDide + `20260908025200_AddFacultyHierarchy` en `dbo.__EFMigrationsHistoryUnifiedDide` |
| Schema smoke | PASS: ParentFacultyId, self FK, AcademicTerms, Identity, Projects, Products, Authors, AppUsers |
| Faculty / AcademicTerm sync | PASS: 53 nodos (11 raíces, 42 descendientes), 23 períodos con IDs externos y fechas |
| Controllers | 47 Unified activos: 45 reemplazos + Faculties + CatalogSynchronization; 45 legacy inactivos |
| Rutas | 244 Unified = 239 reemplazos + 5 catálogos; 300 MVC totales; 0 duplicados |
| Startup / Swagger / health | PASS, Development, HTTPS localhost:7015 |
| Auth HTTP | PASS register, login, refresh, logout y rechazo de refresh posterior |
| Projects HTTP | PASS listado vacío/get inexistente y create/get existente con FK Faculty local; CreateFull PASS con Directory/Distributivos LIVE |
| Group HTTP | PASS create, list, AddMember con Identity/SQL Unified |
| FacultyScope HTTP | PASS listado, create/get existente/assign y semántica FacultyId local / ExternalFacultyId |
| Products HTTP | PASS listado, create y read de producto independiente con autor institucional |
| Otros HTTP | PASS Document GET inexistente, Objectives listado, Visits listado vacío; metadata ServiceResult conservada |
| External | Academics/Periods de negocio: 0; controllers explícitos externos conservados; Directory/Distributivos siguen LIVE, sin sustitutos |
| auth/me | OUT_OF_SCOPE, LEGACY_PRESERVED, NOT_BLOCKER; temporalmente no publicado (404), sin bridge |
| Tests | PASS: 17.265 assertions Unified, 239 contratos y 1.416 escenarios HTTP comparados; 684 checks preflight runtime |
| Build | PASS: 0 errores, 39 warnings baseline, 0 nuevos |
| Legacy / alcance | PRESERVED, PROJECTS_RUNTIME_INACTIVE; sin cleanup, cambios Articles/DW/frontend/modelo o negocio |
| Blockers de build/startup | 0; pruebas aplazadas completadas |

## Frontera temporal y configuración

`Program.cs` registra `UnifiedDideConnection` sin fallback a DefaultConnection.
Se verificó el destino antes de migrar: solo tenía la migración inicial y cero
filas de negocio. No se usaron ni modificaron las dos bases de integración.
La migración fue explícita mediante `dotnet ef database update --project
tesisproject.backend --context UnifiedDideDbContext --no-build -- --environment
Development`. No se introdujeron migraciones/sync automáticos ni scheduling.
Development mantiene `DatabaseBootstrap:ApplyMigrations=false`; el bloque previo
de migraciones legacy/DW no se ejecutó ni se amplió a Unified.

`AppDbContext` permanece para `ArticleUserContext → IAppUserRepository →
AppUserRepository(AppDbContext)` y la composición legacy pendiente. Se conservan
IUnitOfWork, repositories/services legacy y sus registros para esa frontera y
rollback; ninguno es dependencia operacional de los controllers/services Unified.
Los registros existentes de ArticlesDbContext y DwContext se conservaron.
Esto no valida ni migra las sesiones/permisos de Articles.

Development carecía de Jwt:Key/Issuer/Audience. Se habilitó la lectura opcional de
`appsettings.Local.json` (como ya hacía la factory EF), preservando prioridad de
environment/CLI. Se generó una clave local aleatoria; el archivo está ignorado por
Git. No se guardaron passwords/tokens en este informe ni en los resultados del smoke.
No se modificó la deuda de passwords de Project/Group/FacultyScope.

## Evidencia y ejecución reproducible

- `artifacts/cutover-runtime/mvc-endpoints.json`: endpoints del pipeline MVC real,
  sin iniciar el servidor, con verbo, ruta, controller y acción.
- `preflight.log`: composición de los métodos reales de Program con
  ValidateOnBuild/ValidateScopes; resolución de controllers, stores y contexto de
  cada repository, además de ausencia de dependencias legacy en Unified.
- `schema.log`, `migration.log`: destino y schema runtime.
- `smoke-final.log`, `http-smoke.json`: ejecución HTTP final; los aplazados se
  registran como SKIPPED, nunca PASS.
- `smoke.log`: intento original contra el proveedor: sincronizaciones HTTP 500
  `CATALOG_SYNC_PROVIDER_UNAVAILABLE`; no se atribuye a SQL/DI.
- `tests.log`, `build-final.log`, `backend-final.log`: suite, build y startup.

```powershell
dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet
dotnet run --project tests/tesisproject.unifiedservicetests --no-restore
dotnet run --project tests/tesisproject.cutoversmoketests --no-restore
dotnet run --project tesisproject.backend --no-build --launch-profile https
# En otra terminal, sin proveedor (aplazamiento actual):
dotnet run --project tests/tesisproject.cutoversmoketests --no-restore -- smoke-local
# Cuando el proveedor esté disponible, sync explícito y flows académicos:
dotnet run --project tests/tesisproject.cutoversmoketests --no-restore -- smoke
```

El runner usa exclusivamente la BD local `tesis_unified`, genera credenciales
aleatorias y datos pequeños con prefijo `CT-`. Register conserva su restricción de
roles públicos: el runner eleva solo su usuario local recién creado mediante
UserManager y retira roles/bloquea esa cuenta al terminar. Los pequeños fixtures
persisten como evidencia; no se fabrican Faculty/AcademicTerm ni se copian datos de
integración. La rama académica del runner está preparada pero no ejecutada en esta
fase. No se afirma validación de CreateFull ni de sus proveedores LIVE.

## FacultyScopes y rollback

Los siete endpoints pendientes de ajuste frontend mantienen su diferencia
semántica: GET lista, GET por id, POST create, PUT update, PUT faculties,
GET users/{userId}/allowed-faculties y GET me/allowed-faculties. FacultyId es local;
ExternalFacultyId identifica al proveedor. No se cambió el frontend ni se ocultó
esa diferencia. El smoke de valores requiere catálogos sincronizados.

Rollback conceptual: restaurar la composición anterior de Program y retirar
NonController de los 45 legacy; volver a poner NonController en los 47 Unified.
No requiere recuperar archivos borrados. Program anterior se conserva también en
`artifacts/cutover-runtime/Program.before.cs.txt`. No ejecutar down-migrations ni
redirigir Unified a la BD legacy. La suite de descubrimiento debe corresponder al
runtime elegido. No se inició cleanup.

Codebase Memory: verificación Tier 2, generación observada
`2026-09-08T04:35:49Z`, completa. Se leyeron en fuente los gaps conocidos de
AcademicReferencePreparation:67, CatalogSynchronizationErrors:16 y la prueba
CatalogSynchronizationTests:133. Metadata cambiante y trazas heurísticas se
complementaron con fuente y comprobaciones compiladas; la ausencia de gaps no se
considera prueba de completitud.

## Reanudación puntual — 2026-09-08

Solo se retomaron las pruebas que se habían aplazado. Evidencia:
`artifacts/cutover-runtime/academic-followup.log` y
`artifacts/cutover-runtime/academic-followup/results.json`.
Las 10 operaciones HTTP ejecutadas (sync x2, roots/hierarchy/periods, Project
create/get, FacultyScope create/get/assign) devolvieron HTTP 200 y conservaron
ServiceResult. El runner comprobó la FK local de Project y ambos IDs del response
FacultyScope. No se repitieron el build de solución, la suite general ni Auth/Products.

CreateFull no llegó a ejecutarse: la selección de fixture devolvió fallo de
Distributivos; al retomar, localhost:3000 rechazaba nuevamente las conexiones.
El detalle original de ese primer fallo no fue conservado por el runner; se amplió
su diagnóstico sin tocar el cliente ni la lógica de negocio. Para continuar solo
ese flujo, con el auxiliar activo:

```powershell
dotnet run --project tests/tesisproject.cutoversmoketests --no-build -- smoke-createfull
```

Los resultados anteriores permanecen preservados. No se inició cleanup.
## Cierre del pendiente CreateFull — 2026-09-08

**PASS HTTP 200**, `ServiceResult.success=true`, metadata preservada y persistencia
SQL confirmada. Se usaron Directory y Distributivos LIVE. Evidencia:
`artifacts/cutover-runtime/createfull-followup.log`,
`createfull-followup/results.json` y `createfull-followup/persistence.log`.

Se ejecutó solo `smoke-createfull`; no se repitieron sync, Project básico,
FacultyScope, Auth suite, Products, build de solución ni suite general.
La selección del fixture se corrigió exclusivamente en el runner: la búsqueda por
período no encontraba filas; se usó la respuesta LIVE de Distributivos filtrada por
ExternalPeriodId en memoria. El flujo de negocio mantuvo su consulta por correo.
El backend principal estaba detenido y se inició con su perfil HTTPS existente.
El coordinador temporal se registró con credenciales aleatorias y quedó bloqueado
sin roles tras la prueba. No se alteró código de negocio, contratos ni datos legacy.

Este cierre sustituye los estados pendientes históricos de las secciones previas.
**PROJECTS_UNIFIED_RUNTIME_ACTIVE — READY_FOR_LEGACY_CLEANUP**.
No se inició cleanup ni Articles.