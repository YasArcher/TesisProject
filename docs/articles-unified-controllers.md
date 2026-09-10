# Articles Controllers Unified preparados

| Controller | Service |
| --- | --- |
| UnifiedArticlesController (nuevo) | IUnifiedArticleQueryService + IUnifiedArticleRegistrationCommandService |
| UnifiedArticlesRegistrationMatricesController (nuevo) | IUnifiedRegistrationMatrixService + IUnifiedArticleUserContext para referencia HTTP del usuario |
| UnifiedArticlesConfigurationController (existente, verificado) | IUnifiedArticleConfigurationService |
| UnifiedArticlesCatalogsController (existente, verificado) | IUnifiedArticleConfigurationService |

Los cuatro conservan `[NonController]`. No se cambió la composición runtime, los controllers legacy ni Services/repositories/UoW. No se necesita registrar los controllers nuevos para dejarlos preparados e inactivos.

Rutas conservadas: `api/articles`, `api/articles/registration-matrices`, `api/config`, `api/scientific-production/config`, `api/catalogs` y `api/scientific-production/catalogs`. Verbos, parámetros, DTOs, autorización y traducción ServiceResult a HTTP se mantienen. Configuración y catálogos conservan `[Authorize]`; no se introdujo una política funcional diferente. Articles y matrices conservan el 503 `ARTICLES_MODULE_DISABLED` como disponibilidad HTTP del módulo.

Matrices no calcula ownership ni permisos. Aporta `IUnifiedArticleUserContext.OwnerReference` y solicita `includeAll: true`; el Service aprobado decide si el usuario puede ver todo y, en caso contrario, restringe a su propietario autenticado. Esto conserva la visibilidad del endpoint legacy sin trasladar autorización al controller. Staging sigue delegando al Service y continúa pendiente.

## Contrato que deberá asumir el frontend después

- ProductTypeId explícito: 1 ScientificProduction o 2 RegionalProduction.
- ProjectId nullable es la fuente canónica de vinculación; IsProjectResult no identifica un proyecto.
- La respuesta de registro contiene ProductId y ArticleId.
- ParticipantIds y los IDs de participantes representan ProductAuthor.Id, no ArticleParticipant legacy.
- La lectura usa el QueryService aprobado. El controller no reconstruye atributos ni agregados.

Los resultados se pasan a ToActionResult sin reconstruir el envelope: Message, ErrorCode, ValidationErrors y Data se conservan. Ningún cambio de frontend en esta fase.

## Verificación

`tests/tesisproject.articlecontrollertests` ejecuta una matriz compacta sobre todas las acciones: contratos legacy/Unified, forwarding de argumentos, identidad del ServiceResult y estados 200/400/401/403/404/409/500. Además usa un host HTTP local exclusivamente de pruebas con las políticas ArticlePolicies reales para verificar rutas/aliases, challenge 401, forbid 403, respuestas autorizadas, serialización de ValidationErrors y 503 de módulo deshabilitado. Los Services son doubles: la prueba no utiliza BD ni activa el runtime de la aplicación.

El descubrimiento MVC normal conserva los cuatro controllers legacy y excluye los cuatro Unified; las rutas activas del módulo no se duplican. El host de pruebas habilita únicamente los cuatro Unified mediante un feature provider local al test.

Resultado: 512 comprobaciones PASS. Build Release de la solución PASS. Búsqueda acotada en los cuatro controllers Unified sin DbContext, repositories, IQueryable, EF, SaveChanges, selección de formularios, normalización de dominio o decisiones de ownership. Sin excepciones arquitectónicas ni blockers. No se auditó DW, Projects, frontend ni ArticlesMigration.
