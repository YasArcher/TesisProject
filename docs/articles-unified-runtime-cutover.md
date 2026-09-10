# Articles runtime: activación aplicada, aceptación bloqueada por datos

## Cambios aplicados

- En `.\DINNOVA/tesis_unified` se aplicaron `20260908194950_AddArticleReadView` y `20260908201802_HardenArticleReadModel`. Sin conflictos de migration ni correcciones de datos.
- Activos: UnifiedArticlesController, UnifiedArticlesConfigurationController, UnifiedArticlesCatalogsController y UnifiedArticlesRegistrationMatricesController.
- Equivalentes legacy conservados físicamente con NonController. Sus registrations no se eliminaron.
- AddUnifiedDide registra los cuatro controllers; el runtime resuelve sus Services, contexto de usuario, UoW, repositories e Identity Unified.
- 52 rutas Articles, solo Unified, sin duplicados Articles. Rutas preservadas en api/articles, api/articles/registration-matrices, api/config, api/scientific-production/config, api/catalogs y api/scientific-production/catalogs.

## Evidencia real local

El probe utilizó las funciones ConfigureDatabase/Authentication/Options/DependencyInjection/Pipeline de la aplicación, sus configuraciones Development/Local y su BD Unified de aplicación. No sustituyó Services ni autenticación por doubles. Inició un host local temporal y lo detuvo después; no reinició otros procesos de aplicación. No ejecutó el bootstrap de contextos legacy/DW.

Login con JWT real, auth/me (401 anónimo, 200 autenticado), catálogos, formularios y listados Articles/matrices: PASS. Formulario resuelto: 404 CONFIG_FORM_NOT_FOUND. Detalle inexistente: 404 ARTICLE_NOT_FOUND. DI real ValidateOnBuild/ValidateScopes PASS; Identity y repositories del UoW comparten UnifiedDideDbContext. Ninguna dependencia legacy en la cadena Articles activa verificada.

Se creó una cuenta exclusivamente de smoke con privilegio superadmin para probar los endpoints autorizados. Se conserva identificada en artifacts/articles-runtime/fixture.json; no se guardó contraseña ni token en la evidencia. No se hizo cleanup de legacy ni se alteraron usuarios preexistentes.

## Bloqueo de aceptación

La aplicación tiene únicamente ProductType 1 llamado `Producto smoke`. No existe ProductType 2. Hay **0 ProductAttributeDefinitions, 0 formularios, 0 campos configurados**, por lo que no puede completarse registro Unified.

SQL observado: Products=3, ProductValues=0, Articles=0, ProductAuthors=3, ProductAuthorDynamicFieldValues=0, filas ArticleReadView=3. La VIEW reconstruye los Products existentes aunque aún no tengan extensión Article; esto no demuestra registros de Articles exitosos.

Existen Projects reales (IDs 3 y 4), pero falta cargar/validar los catálogos base y la configuración de formularios aprobados. No se renombró ni reasignó el ProductType preexistente, ni se inventó una configuración de producción para obtener un PASS.

Pendiente de smoke real por este bloqueo: A/B/C, autoría institucional/externa/orden, verificación de ProductValues 3–10 y relaciones creadas, lectura de sus valores, CRUD Draft/ownership/CanManageAll de matrices, DOI duplicado, ProjectId inválido, conflicto de identidad y ValidationErrors de registro. Las suites aisladas cubren estos comportamientos, pero no reemplazan su aceptación en la BD local de aplicación.

## Regresiones

631 comprobaciones PASS: controllers 512, configuración 48, write path 31, Services 22, auth/me 18. Migrations de estas suites solo en sus bases GUID temporales; instancia LocalDB de pruebas retirada. Build TesisProject.sln Release PASS, 0 errores. Assertions de activación actualizadas a Unified activo/legacy inactivo.

Evidencia: artifacts/articles-runtime-migrations.log, articles-runtime-inspect.log, articles-runtime-smoke.log, articles-runtime/routes.json y articles-runtime/http.json.

Conclusión: activación de código y migrations realizadas; **cutover todavía no aceptado funcionalmente**. No se declara READY_FOR_ARTICLES_LEGACY_CLEANUP. Primero debe cargarse/validarse la configuración canónica en la BD de aplicación y repetirse el smoke completo. Sin cambios frontend/DW/Staging ni cleanup legacy.
