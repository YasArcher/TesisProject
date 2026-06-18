# Ruta de adaptacion de articulos a arquitectura de proyectos

Fecha: 2026-06-17

## Regla rectora

El sistema de proyectos es la base arquitectonica. El sistema de articulos debe adaptarse a esa estructura y no reemplazarla.

## Elementos que prevalecen desde proyectos

- `Program.cs` del backend.
- `Program.cs` del frontend.
- `AppDbContext` principal de proyectos.
- Sistema de autenticacion, usuarios, roles, JWT y autorizacion de proyectos.
- `Repositories`, `UnitOfWork` y patron de servicios de proyectos.
- `MainLayout`, `SideBar`, `App.razor`, `_Imports.razor` y navegacion base de proyectos.
- `ApiClient`, `AuthMessageHandler`, `LocalTokenStore` y flujo base de consumo HTTP del frontend de proyectos.
- `docker-compose.yml`, `.env`, `appsettings` y configuracion base de despliegue de proyectos.

## Elementos de articulos que se migran como modulo

### Backend

Origen actual:

- `tesisproject.backend/ArticlesMigration/Controllers/Modules/*`
- `tesisproject.backend/ArticlesMigration/Services/Modules/*`
- `tesisproject.backend/ArticlesMigration/Data/Entities/*`
- `tesisproject.backend/ArticlesMigration/Data/Configurations/*`
- `tesisproject.backend/ArticlesMigration/DataWarehouse/*`
- `tesisproject.backend/ArticlesMigration/Reporting/*`

Destino recomendado:

- `tesisproject.backend/Controllers/Articles/*`
- `tesisproject.backend/Services/Interfaces/IArticle*.cs`
- `tesisproject.backend/Services/Implementations/Article*.cs`
- `tesisproject.backend/Repositories/Interfaces/IArticle*.cs`
- `tesisproject.backend/Repositories/Implementations/Article*.cs`
- `tesisproject.backend/Data/Configurations/Articles/*`
- `tesisproject.backend/DataWarehouse/Articles/*` o contexto analitico separado, segun decision tecnica.

### Frontend

Origen actual:

- `tesisproject.frontend/ArticlesMigration/Features/*`
- `tesisproject.frontend/ArticlesMigration/Services/*`
- `tesisproject.frontend/ArticlesMigration/Models/*`
- `tesisproject.frontend/ArticlesMigration/SharedUI/*`

Destino recomendado:

- `tesisproject.frontend/Features/Articles/Pages/*`
- `tesisproject.frontend/Features/Articles/Components/*`
- `tesisproject.frontend/Features/Reporting/Articles/*` si se conserva reportería separada.
- `tesisproject.frontend/Services/Interfaces/IArticlesClientService.cs`
- `tesisproject.frontend/Services/Implementations/ArticlesClientService.cs`

### Shared

Origen actual:

- `tesisproject.shared/ArticlesMigration/DTOs/*`
- `tesisproject.shared/ArticlesMigration/Validation/*`
- `tesisproject.shared/ArticlesMigration/Wrappers/*`

Destino recomendado:

- `tesisproject.shared/DTOs/Articles/*`
- `tesisproject.shared/DTOs/ArticlesReporting/*`
- `tesisproject.shared/DTOs/ArticlesWorkflow/*`
- `tesisproject.shared/DTOs/ArticlesConfiguration/*`
- `tesisproject.shared/Validation/Articles/*` si no existe equivalente en proyectos.

## Decisiones tecnicas pendientes

### 1. DbContext

Opciones:

A. Integrar entidades de articulos en `AppDbContext` de proyectos.

Ventaja:

- Un solo contexto transaccional.
- Mejor integracion con UnitOfWork.

Riesgo:

- Mayor choque inicial con entidades existentes, migraciones y catalogos.

B. Crear contexto temporal `ArticlesDbContext`.

Ventaja:

- Menor riesgo inicial.
- Permite levantar articulos como modulo independiente dentro del mismo backend.

Riesgo:

- Luego habra que decidir si queda separado o si se unifica.

Recomendacion inicial:

- Usar contexto temporal para la primera activacion tecnica y luego evaluar integracion completa al `AppDbContext` de proyectos.

### 2. Repositories y UnitOfWork

El sistema de proyectos usa repositorios y UnitOfWork. Por tanto, los servicios de articulos deben adaptarse gradualmente:

- Crear repositorios para entidades principales de articulos.
- Registrar repositorios en DI.
- Exponerlos en `IUnitOfWork` solo cuando sean estables.
- Evitar que los servicios de articulos dependan directamente de `DbContext` si ya pasan a fase integrada.

### 3. Seguridad y roles

Debe prevalecer seguridad de proyectos.

Los roles/permisos de articulos deben mapearse a roles o permisos del sistema base:

- Administrador
- Autor
- Revisor UODIDE
- Revisor Area Tecnica
- Acceso a reportería
- Acceso a APIs externas
- Acceso a IA

### 4. Frontend

No se deben copiar `App.razor`, `MainLayout`, `SideBar`, `Program.cs` ni `_Imports.razor` de articulos sobre proyectos.

Solo se deben migrar paginas y componentes como features:

- `Features/Articles`
- `Features/ArticlesRegistration`
- `Features/ArticlesWorkflow`
- `Features/ArticlesReporting`
- `Features/ArticlesIntelligence`
- `Features/ArticlesExternalSources`

### 5. Configuracion y Docker

Debe prevalecer Docker/configuracion de proyectos. Las variables de articulos se agregan de forma controlada:

- conexiones OLTP/DW si se conservan separadas,
- claves de APIs externas,
- configuracion Scopus,
- opciones de IA,
- opciones de reportería.

## Orden recomendado de integracion

1. Shared: DTOs no conflictivos de articulos.
2. Backend: modelo de datos de articulos en contexto temporal o area aislada.
3. Backend: repositorios basicos de articulos.
4. Backend: servicios de registro/catalogos de articulos.
5. Backend: controladores de registro/catalogos.
6. Frontend: clientes HTTP de articulos adaptados al `ApiClient` de proyectos.
7. Frontend: pagina de listado/registro de articulos como feature.
8. Workflow de articulos.
9. Reporterias y Data Warehouse.
10. APIs externas e ingesta masiva.
11. Modulo IA.
12. Unificacion de navegacion, permisos y experiencia visual.
13. Depuracion de duplicados y eliminacion de `ArticlesMigration` residual.

## Regla de comentarios

Todo archivo o bloque activado desde articulos debe marcarse con:

```csharp
// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
```

Cuando quede completamente adaptado:

```csharp
// [ARTICLES-INTEGRATED] Codigo integrado desde articulos y adaptado a arquitectura de proyectos.
```
