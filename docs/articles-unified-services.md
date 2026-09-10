# Articles: Services Unified

## Implementaciones disponibles sin cutover

| Service | Dependencias de persistencia | Responsabilidad |
| --- | --- | --- |
| UnifiedRegistrationMatrixService | IUnifiedUnitOfWork: ArticleRegistrationMatrices, ArticleFields, RegistrationMatrixCells | Ownership, CanManageAll, validación, estado Draft, CRUD y transacción |
| UnifiedArticleUserContext | IHttpContextAccessor + IUnifiedUnitOfWork.AppUsers | Claims; IdLocal de Identity a IdUser de AppUser, sin búsqueda ni relink por email |
| UnifiedArticleQueryService | IUnifiedArticleReadRepository | Paginación, validaciones, ServiceResult y mapping de DTOs desde ArticleReadView y relaciones Unified |

AddUnifiedDide registra interfaces IUnified propias. No sustituye los contratos activos legacy ni activa controllers. Registro, provisioning Identity, selector de formulario y configuración aprobados permanecen sin cambios.

Matrices conserva mensajes, elegibilidad/orden de campos, anchos, numeración de filas, normalización de celdas y restricciones Draft/LastImportBatchId. La autorización se comprueba dentro del service: includeAll no concede permisos por sí mismo. Crear sin propietario autenticado coincidente devuelve ARTICLES_MATRIX_OWNER_REQUIRED para evitar matrices inaccesibles. Staging sigue devolviendo ARTICLES_MATRIX_STAGING_PENDING.

## Huecos específicos cubiertos en repositories existentes

- Matrix repository agrega ejecución asíncrona de detalle y resúmenes con predicados decididos por el service; evita trasladar EF a Services.
- Read repository agrega página y detalle con relaciones estructurales/autores necesarios para los DTOs actuales. Filtra y pagina en SQL antes de cargar relaciones de esa página. ArticleReadAggregate es un resultado de lectura, no una entidad persistente.
- No se crea otra capa de repositories, ni migrations, ni cambios a ArticleReadView.

El DTO actual identifica Article.Id; por ello página/detalle requieren extensión Article. Los Products sin esa extensión siguen disponibles en Query() de la VIEW, pero no se inventa un ArticleId para exponerlos en el contrato antiguo. DOI/Year/Journal/Quartile/ISSN/SJR proceden de la VIEW. IsProjectResult se deriva de ProjectId. ParticipantIds y los IDs del DTO de participantes representan ProductAuthor.Id; los nombres y datos de participación proceden de snapshots.

## Verificación de dependencias

Los tres nuevos Services no usan EF, DbContext ni repositories legacy. Los Services Articles Unified no tienen referencias directas a UnifiedDideDbContext, ArticlesDbContext o AppDbContext.

La limpieza final eliminó también los imports Microsoft.EntityFrameworkCore y Microsoft.Data.SqlClient de UnifiedArticleConfigurationService y UnifiedArticleRegistrationCommandService. Configuración ejecuta búsquedas, listas, includes y comprobaciones de existencia mediante métodos async del repository existente; las validaciones, normalizaciones, resolución de campos canónicos y ServiceResult permanecen en el service. Registro reutiliza GetAllAsync para definiciones y FindAuthorAsync en su repository para mantener la consulta SingleOrDefault con tracking. No se crearon repositories nuevos.

UnifiedPersistenceErrors, dentro de persistencia, clasifica las excepciones EF/SQL sin reemplazarlas ni alterar la transacción. El service conserva los mensajes/códigos de duplicación DOI, conflicto, referencias inválidas y configuración en uso. No se modificó UoW ni el orden de provisioning Identity. No quedan referencias EF/SqlClient, DbContext o repositories legacy en los Services Unified de Articles revisados. Los contratos de servicio heredados se reutilizan para compatibilidad; sus repositories legacy no se inyectan.

La verificación fue acotada a estos Services y sus dependencias. Codebase Memory señaló metadata_changed; se verificó la fuente actual de los archivos afectados.

## Validación

- tests/tesisproject.articleservicetests: 22 comprobaciones SQL/EF PASS; matrices/ownership/CanManageAll/CRUD/estados, contexto institucional, conflicto sin relink, registro aprobado seguido de lectura canónica y composición DI ValidateOnBuild/ValidateScopes.
- Migrations desde cero sobre base GUID temporal; eliminada en finally. Instancia LocalDB exclusiva de esta ejecución, retirada al finalizar. No se aplicaron migrations a la BD de aplicación.
- TesisProject.sln Release: 0 errores, 20 warnings.
- El entorno de esta ejecución solo ofrecía runtime .NET 10 y manifests de workloads incompletos. Se usaron MSBuildEnableWorkloadResolver=false y DOTNET_ROLL_FORWARD=Major en el proceso de verificación; el proyecto conserva net9.0. Los tests SQL pasaron sobre LocalDB, no sobre DINNOVA (rechazaba conexiones por single-user).

Para reproducir con SDK/runtime compatibles: definir ARTICLES_TEST_SQL_SERVER con una instancia SQL de pruebas y ejecutar `dotnet run --project tests/tesisproject.articleservicetests -c Release`. El test crea y elimina únicamente su base temporal. Build: `dotnet build TesisProject.sln -c Release`.

## Verificación de la limpieza final

Las suites existentes de configuración, write path y Services pasaron respectivamente 48, 31 y 22 comprobaciones (101 total), incluyendo DOI duplicado por SQL, rollback, conservación Identity, formularios resueltos y DI. Las suites de configuración y registro ahora admiten ARTICLES_TEST_SQL_SERVER, conservando DINNOVA como valor predeterminado. Esta ejecución utilizó bases GUID en una instancia LocalDB exclusiva y SDK/runtime .NET 9.0.318 instalado en una carpeta temporal, porque el dotnet global no encontraba SDK. No se usó roll-forward a .NET 10 en esta verificación.

Build final de TesisProject.sln Release: 0 errores, 21 warnings. Instancia LocalDB temporal eliminada al finalizar.
