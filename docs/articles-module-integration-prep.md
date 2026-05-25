# Preparacion del modulo de articulos para integracion

Este documento registra los ajustes de compatibilidad para migrar el sistema de articulos cientificos hacia el sistema anfitrion de proyectos de investigacion sin romper las rutas actuales.

## Criterio general

- El sistema de proyectos sera el host y no debe moverse por esta preparacion.
- El sistema de articulos se conserva funcional y se expone tambien bajo un prefijo propio.
- Las rutas actuales se mantienen para no afectar pruebas, usuarios ni enlaces existentes.
- Las rutas nuevas sirven como base para montar el modulo dentro del sistema integrado.

## Prefijo funcional

El prefijo reservado para el modulo es:

```text
scientific-production
```

## Rutas frontend nuevas

| Vista | Ruta actual | Ruta preparada |
| --- | --- | --- |
| Dashboard | `/panel/principal` | `/scientific-production/dashboard` |
| Listado de articulos | `/articulos/listado` | `/scientific-production/articles` |
| Registro de articulo | `/articulos/registrar` | `/scientific-production/articles/register` |
| Edicion de articulo | `/articulos/editar/{ArticleId:int}` | `/scientific-production/articles/edit/{ArticleId:int}` |
| Espacio del autor | `/management/author-workspace` | `/scientific-production/author-workspace` |
| Matriz de registro | `/management/registration-matrix` | `/scientific-production/registration-matrix` |
| Carga masiva | `/management/exportinformation` | `/scientific-production/bulk-import` |
| Staging de revision | `/management/article-review-staging` | `/scientific-production/article-review-staging` |
| Workflow | `/management/workflow-review` | `/scientific-production/workflow-review` |
| Reporteria | `/management/statisticalreports` | `/scientific-production/reporting` |
| Inteligencia | `/management/intelligence` | `/scientific-production/intelligence` |
| APIs externas | `/management/externalapis` | `/scientific-production/external-apis` |
| Ingesta externa | `/management/external-ingestion` | `/scientific-production/external-ingestion` |
| Configuracion de articulos | `/configuration` | `/scientific-production/configuration` |

## Rutas API nuevas

| Modulo | Ruta actual | Ruta preparada |
| --- | --- | --- |
| Articulos | `api/articles` | `api/scientific-production/articles` |
| Registro agregado | `api/articles/aggregate` | `api/scientific-production/articles/aggregate` |
| Staging / importacion | `api/import-batches` | `api/scientific-production/import-batches` |
| Workflow | `api/workflows/import-batches` | `api/scientific-production/workflows/import-batches` |
| Matriz de registro | `api/registration-matrices` | `api/scientific-production/registration-matrices` |
| Configuracion workflow | `api/registration-workflow-settings` | `api/scientific-production/registration-workflow-settings` |
| Reporteria | `api/reporting` | `api/scientific-production/reporting` |
| Inteligencia | `api/intelligence` | `api/scientific-production/intelligence` |
| APIs externas | `api/external-api-explorer` | `api/scientific-production/external-api-explorer` |
| Catalogos | `api/catalogs` | `api/scientific-production/catalogs` |
| Formularios dinamicos | `api/config` | `api/scientific-production/config` |
| Revistas / venues | `api/venues` | `api/scientific-production/venues` |

## Catalogo Project

El sistema de articulos mantiene una entidad `Project` como catalogo minimo para referencia de produccion cientifica asociada a proyectos. No representa el modulo real de gestion de proyectos.

Por compatibilidad se conserva sin cambios. En la fusion final debe tratarse como referencia temporal o mapearse hacia el `Project` real del sistema anfitrion.

## Proxima fase recomendada

1. Crear un mapa de roles entre articulos y proyectos.
2. Revisar nombres de DTOs y endpoints que puedan chocar con el host.
3. Definir si el sistema integrado consumira rutas antiguas, rutas nuevas o ambas durante una fase de transicion.
4. Evaluar despues si conviene adaptar servicios de articulos a repositorios/unit of work del host.

## Mapa preliminar de roles y permisos

El sistema de articulos maneja roles con dos naturalezas:

- Perfiles operativos: representan tipos de usuario o responsabilidades institucionales.
- Permisos funcionales: habilitan una accion concreta sin obligar a crear otro perfil completo.

En la fusion, el sistema de proyectos debe mantenerse como host. Por eso no se recomienda reemplazar estos permisos por los roles generales del host. La ruta segura es mapearlos como capacidades del modulo de articulos.

### Roles actuales del modulo de articulos

| Rol | Tipo | Funcion dentro de articulos |
| --- | --- | --- |
| `Admin` | Perfil | Acceso administrativo general. |
| `Analyst` | Perfil | Gestion operativa, reportería, carga y configuracion segun politicas. |
| `Author` | Perfil | Registro, seguimiento y correccion de envios como autor. |
| `WorkflowReviewerUodide` | Perfil | Revision inicial UODIDE. |
| `WorkflowReviewerAreaTecnica` | Perfil | Revision final de area tecnica. |
| `WorkflowProcessorAreaTecnica` | Perfil heredado | Procesamiento final; se conserva por compatibilidad, aunque visualmente se normaliza hacia area tecnica. |
| `SecurityAdministrator` | Permiso/Perfil | Administracion de usuarios y seguridad. |
| `RoleManager` | Permiso/Perfil | Gestion de roles y asignaciones. |
| `ArticleRegistrationUser` | Permiso | Puede registrar articulos sin depender exclusivamente del rol `Author`. |
| `RegistrationMatrixUser` | Permiso | Puede usar matriz de registro. |
| `DirectArticleSaveUser` | Permiso | Puede guardar articulos directamente. |
| `BulkImportUser` | Permiso | Puede usar carga masiva/staging. |
| `ExternalApiUser` | Permiso | Puede usar APIs externas e ingesta asociada. |
| `WorkflowTrackingUser` | Permiso | Puede dar seguimiento a envios y correcciones. |
| `ReportingViewer` | Permiso | Puede ver reportería. |
| `ReportingExporter` | Permiso | Puede exportar reportes. |
| `ReportingAdvancedUser` | Permiso | Puede usar reportería avanzada. |
| `IntelligenceViewer` | Permiso | Puede ver resultados de IA. |
| `IntelligenceTrainer` | Permiso | Puede ejecutar reentrenamiento IA. |
| `ConfigurationManager` | Permiso | Puede administrar formularios y configuracion. |
| `CatalogManager` | Permiso | Puede administrar catalogos. |

### Politicas actuales del backend de articulos

| Politica | Roles habilitados |
| --- | --- |
| `SecurityAdministration` | `Admin`, `SecurityAdministrator`, `RoleManager` |
| `AuthorSubmission` | `Admin`, `Analyst`, `Author`, `WorkflowReviewerUodide`, `ArticleRegistrationUser`, `RegistrationMatrixUser` |
| `ArticlesWrite` | `Admin`, `Analyst`, `DirectArticleSaveUser` |
| `WorkflowAccess` | `Admin`, `Author`, `WorkflowTrackingUser`, `WorkflowReviewerUodide`, `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica` |
| `WorkflowReview` | `Admin`, `WorkflowReviewerUodide`, `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica` |
| `WorkflowProcess` | `Admin`, `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica` |
| `BulkImportAccess` | `Admin`, `Analyst`, `BulkImportUser`, `WorkflowReviewerUodide`, `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica` |
| `ConfigurationAdministration` | `Admin`, `Analyst`, `ConfigurationManager`, `CatalogManager` |
| `ExternalApiAccess` | `Admin`, `Analyst`, `Author`, `ExternalApiUser` |
| `ReportingAccess` | `Admin`, `Analyst`, `ReportingViewer`, `ReportingExporter`, `ReportingAdvancedUser`, `IntelligenceViewer`, `IntelligenceTrainer`, `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica` |

### Equivalencia preliminar con el sistema de proyectos

El sistema de proyectos observado usa roles mas generales como `admin`, `superadmin`, `technical`, `financial`, `coordinador` y `user`. La equivalencia debe ser de capacidades, no de reemplazo directo.

| Rol host de proyectos | Capacidades sugeridas para articulos |
| --- | --- |
| `superadmin` | Todas las capacidades del modulo de articulos. |
| `admin` | `Admin`, `SecurityAdministrator`, `RoleManager`, y permisos administrativos segun alcance institucional. |
| `technical` | `WorkflowReviewerAreaTecnica`, `WorkflowProcessorAreaTecnica`, `BulkImportUser`, `ReportingViewer`, `ReportingExporter`. |
| `coordinador` | `WorkflowReviewerUodide`, `WorkflowTrackingUser`, `ReportingViewer`, `ArticleRegistrationUser` si la institucion lo permite. |
| `financial` | Sin acceso operativo por defecto al modulo de articulos; opcionalmente `ReportingViewer` si requiere consulta institucional. |
| `user` | Sin permisos de articulos por defecto. Para autores institucionales, asignar `Author` o `ArticleRegistrationUser` segun el flujo vigente. |

### Recomendacion de integracion

1. Mantener los roles/permisos de articulos como capacidades del modulo `scientific-production`.
2. No depender solo de los roles generales del host para acciones criticas como procesar, reentrenar IA o administrar configuracion.
3. Crear una capa de asignacion en el sistema integrado:

```text
Rol del host -> Capacidades del modulo de articulos
```

4. Durante la migracion, permitir que un usuario tenga roles del host y permisos especificos de articulos al mismo tiempo.
5. Posteriormente, si el host soporta permisos configurables, migrar estos roles-permiso a permisos formales del sistema integrado.

### Riesgo principal

El mayor riesgo es dar demasiado acceso por equivalencia amplia. Por ejemplo, mapear `technical` directamente a `Admin` seria peligroso. Lo correcto es asignarle solo capacidades tecnicas del flujo de articulos.

## Analisis de choques de nombres y contratos

El sistema de articulos ya esta razonablemente modularizado, pero al fusionarlo con el sistema de proyectos hay nombres que pueden chocar porque el host tambien maneja proyectos, usuarios, workflow, configuracion, documentos y reportería.

La regla recomendada es: no renombrar por gusto dentro de articulos antes de migrar, pero si documentar y encapsular cada contrato para que, al copiarlo al host, quede claro que pertenece al modulo `scientific-production`.

### Riesgo alto

| Elemento en articulos | Motivo del riesgo | Ruta recomendada |
| --- | --- | --- |
| `Project` / `Projects` | El host de proyectos ya tiene `Project` como entidad central. En articulos es solo catalogo minimo. | Mantenerlo por ahora. En la fusion tratarlo como `ArticleProjectReference` o mapearlo al `Project` real del host. |
| `AppDbContext` | Ambos sistemas tienen `AppDbContext`. | Al migrar, no copiar el contexto completo. Mover entidades de articulos al `AppDbContext` del host o crear configuraciones por modulo. |
| `ApplicationUser` / `ApplicationRole` | Ambos sistemas manejan Identity. | Usar Identity del host. Migrar solo roles/capacidades de articulos, no reemplazar usuarios del host. |
| `Workflow*` | El host puede tener flujos propios de proyectos. | Encapsular como workflow de articulos: `ArticleWorkflow` o `ScientificProductionWorkflow` en rutas/documentacion. |
| `ConfigurationController` / `api/config` | Nombre muy general y probable choque con configuracion del host. | Usar alias `api/scientific-production/config`. En fusion, montar bajo modulo de articulos. |
| `CatalogsController` / `api/catalogs` | Nombre generico; proyectos tambien maneja catalogos. | Usar alias `api/scientific-production/catalogs`. Separar catalogos de articulos de catalogos globales. |

### Riesgo medio

| Elemento en articulos | Motivo del riesgo | Ruta recomendada |
| --- | --- | --- |
| `ReportingController` / `ReportingDbContext` | El host podria tener reportería de proyectos. | Mantener como reportería de produccion cientifica. Preferir `ScientificProductionReporting` en integracion. |
| `IntelligenceController` | La IA futura podria ser institucional y cubrir ambos dominios. | Mantener IA de articulos como submodulo de produccion cientifica; luego crear IA integrada si aplica. |
| `BulkImport*` / `ImportBatch*` | Proyectos tambien puede importar matrices o datos externos. | Mantener como importacion de articulos/staging de articulos. Prefijo de API ya preparado. |
| `RegistrationMatrix*` | Puede confundirse con matrices de proyectos. | Nombrarlo funcionalmente como matriz de registro de articulos durante la fusion. |
| `ExternalApiExplorer*` | El host podria consumir otras APIs externas. | Mantener bajo `scientific-production/external-apis`; no convertir en servicio global todavia. |
| `AuditLog` | Auditoria podria ser global en el host. | A futuro puede mapearse a auditoria global; por ahora mantener como auditoria del modulo. |
| `Venue` / `IndexingSource` | Son conceptos propios de publicaciones, no de proyectos. | Mantener, pero dentro del modulo de articulos. |

### Riesgo bajo

| Elemento en articulos | Motivo | Ruta recomendada |
| --- | --- | --- |
| `Article`, `ArticleParticipant`, `ArticleIndexing`, `ArticleFile` | Dominio claramente propio de articulos. | Migrables como entidades del modulo. |
| `Faculty`, `ResearchLine`, campos OCDE | Pueden existir en ambos sistemas, pero son catálogos institucionales comunes. | Evaluar si se unifican con catálogos del host o se mantiene compatibilidad inicial. |
| DTOs bajo `tesisproject.shared.DTOs.Articles` | Ya estan claramente separados. | Copiar/migrar como contratos del modulo. |
| DTOs bajo `Reports`, `Intelligence`, `Workflow` | Funcionan, pero el namespace es mas generico. | En una fusion ideal, mover a subnamespace `ScientificProduction`. No es obligatorio antes de migrar. |

### Contratos compartidos que requieren cuidado

Estos contratos son utiles pero genericos:

- `PagedResult<T>`
- `ApiResult<T>`
- `Result<T>`
- DTOs de `Auth`
- DTOs de `Audit`
- DTOs de `Catalogs`
- DTOs de `Configuration`

En la fusion deben compararse con los equivalentes del sistema de proyectos. Si el host ya tiene contratos similares, se recomienda:

1. Usar los contratos del host para plataforma global.
2. Mantener los contratos de articulos solo si tienen semantica propia.
3. Evitar duplicar nombres iguales en el mismo namespace.

## Preparacion recomendada antes de migrar

### Sin tocar funcionalidad

1. Mantener rutas actuales y aliases `scientific-production`.
2. Mantener `Project` como catalogo temporal, sin expandirlo.
3. No convertir todavia a repositorios/unit of work.
4. No mover Identity de articulos al host hasta definir migracion de usuarios/roles.
5. Documentar los controllers y servicios que se copiaran como modulo.

### Al momento de fusionar

1. Crear o reservar namespace/carpeta del modulo:

```text
ScientificProduction
```

2. Montar APIs bajo:

```text
api/scientific-production/*
```

3. Montar vistas bajo:

```text
/scientific-production/*
```

4. Registrar servicios de articulos sin tocar servicios de proyectos.
5. Integrar permisos como capacidades del modulo.
6. Mantener ETL/reportería de articulos separado del DW de proyectos al inicio.
7. Crear una relacion controlada entre articulo y proyecto real solo cuando el modulo ya compile dentro del host.

## Decision arquitectonica sugerida

Para una primera fusion estable:

```text
Sistema de proyectos = host
Modulo de articulos = scientific-production
BD/transaccional = tablas de articulos agregadas al host o esquema separado
DW = separado inicialmente
Identity = host
Permisos de articulos = capacidades especificas del modulo
```

Despues de esa fase, se puede avanzar hacia integracion profunda:

```text
Articulo -> Proyecto real del host
Reporteria integrada proyectos + articulos
IA institucional usando ambos dominios
Repositorios/unit of work alineados al host
```

## Inventario de migracion por carpetas

Este inventario indica como llevar el sistema de articulos al sistema anfitrion de proyectos sin mover primero la arquitectura del host.

### Backend

| Carpeta / archivo | Accion recomendada | Motivo |
| --- | --- | --- |
| `Controllers/Modules/Articles` | Copiar como modulo | Dominio propio de articulos. Ya tiene alias `api/scientific-production/articles`. |
| `Controllers/Modules/Registration` | Copiar como modulo | Registro individual/agregado de articulos. Debe conservar politicas de articulos. |
| `Controllers/Modules/BulkImport` | Copiar como modulo | Staging/carga masiva de articulos. Debe mantenerse aislado de cargas de proyectos. |
| `Controllers/Modules/MassRegistration` | Copiar como modulo | Matriz de registro de articulos. Riesgo medio por nombre, pero funcionalmente es propio. |
| `Controllers/Modules/Workflow` | Copiar con prefijo de modulo | Workflow de articulos. No mezclar con workflow de proyectos. |
| `Controllers/Modules/Reporting` | Copiar con prefijo de modulo | Reporteria de produccion cientifica. Mantener separada de reportería de proyectos al inicio. |
| `Controllers/Modules/Intelligence` | Copiar con prefijo de modulo | IA actual trabaja sobre produccion cientifica. Despues puede integrarse a IA institucional. |
| `Controllers/Modules/ExternalSources` | Copiar con prefijo de modulo | APIs externas/Scopus alimentan articulos. No volverlo API global todavia. |
| `Controllers/Modules/DynamicForms` | Adaptar | Usa `api/config`, formularios y campos dinamicos de articulos. Debe montarse como configuracion de produccion cientifica. |
| `Controllers/Modules/CatalogAdministration` | Adaptar | Maneja catalogos de articulos y algunos catálogos potencialmente globales. |
| `Controllers/Modules/Venues` | Copiar como submodulo de publicaciones | `Venue` es propio de revistas/congresos. |
| `Controllers/Platform/Security` | No copiar completo | El host debe conservar su seguridad. Solo migrar capacidades/roles necesarios. |
| `Services/Modules/*` | Copiar como bloque modular | Contiene logica funcional de articulos. Mantener nombres bajo `ScientificProduction` en integracion ideal. |
| `Services/Platform/Auth` | Adaptar/no copiar completo | Debe conectarse a Identity del host. |
| `Services/Platform/Audit` | Adaptar | Si el host tiene auditoria, mapear; si no, copiar temporalmente. |
| `Configuration/ServiceCollectionExtensions.cs` | No copiar completo | Extraer solo registros DI de servicios de articulos y politicas/capacidades necesarias. |
| `Configuration/WebApplicationExtensions.cs` | No copiar completo | Contiene bootstrap, seeds y validaciones de esquema. El host debe controlar startup. |
| `Identity/*` | No copiar completo | El host debe gobernar usuarios/roles. Migrar constantes como referencia de permisos. |
| `Data/AppDbContext.cs` | No copiar completo | Agregar entidades de articulos al contexto del host o crear configuraciones por entidad. |
| `Data/Entities/Article*` | Copiar | Entidades centrales del modulo. |
| `Data/Entities/Venue*`, `IndexingSource`, `PublicationStatus`, `ResearchLine`, campos OCDE | Copiar o mapear | Pueden ser propios del modulo o catálogos institucionales compartidos. |
| `Data/Entities/Project.cs` | Conservar solo como referencia temporal | No debe reemplazar el `Project` real del host. |
| `Data/Entities/Workflow*`, `ImportBatch*`, `RegistrationMatrix*` | Copiar como entidades de articulos | Aislar conceptualmente como workflow/staging/matriz de articulos. |
| `Data/Entities/IntelligenceTraining*`, `ReportingPerformanceMetric` | Copiar si se conserva IA/reporteria actual | Relacionadas con metricas del modulo. |
| `DataWarehouse/*` | Copiar separado inicialmente | No fusionar DW de articulos con DW de proyectos en primera pasada. |
| `Reporting/*` | Copiar junto con reporteria | Contexto de lectura/modelos para reportería. |
| `Migrations/*` | No copiar directo sin revisar | El host debe generar migraciones nuevas o scripts controlados. |
| `Options/*` | Copiar/adaptar | Opciones de APIs externas e identidad institucional deben integrarse a configuracion del host. |
| `Errors/*` | Copiar si no existe equivalente | Puede sustituirse por manejo global del host. |

### Frontend

| Carpeta / archivo | Accion recomendada | Motivo |
| --- | --- | --- |
| `Features/Articles` | Copiar | Listado y visualizacion de articulos. |
| `Features/Registration` | Copiar | Registro individual, matriz del autor y workspace. |
| `Features/BulkImport` | Copiar | Staging/carga masiva de articulos. |
| `Features/Workflow` | Copiar como workflow de articulos | No mezclar visualmente con workflow de proyectos. |
| `Features/Reporting` | Copiar como reportería de produccion cientifica | Ya tiene UI compleja y especifica. |
| `Features/ExternalSources` | Copiar | APIs externas/ingesta de articulos. |
| `Features/Intelligence` | Copiar | IA de produccion cientifica. |
| `Features/Configuration` | Copiar/adaptar | Configura formularios/catalogos de articulos. |
| `Features/Dashboard` | Adaptar | Puede chocar con dashboard del host. Montar como dashboard de produccion cientifica. |
| `Features/Auth` | No copiar completo | El host debe mantener login/auth. Solo reutilizar si se decide unificar visualmente. |
| `Features/Security` | No copiar completo | La administracion de usuarios debe vivir en el host. Migrar solo UI si el host no tiene equivalente. |
| `Features/Profile` | No copiar completo | Usar perfil del host. |
| `Features/Support` | Opcional | Puede integrarse como ayuda del modulo. |
| `Services/Interfaces` y `Services/Implementations` | Copiar/adaptar | Copiar clientes propios del modulo; no duplicar auth/security si el host ya los tiene. |
| `SharedUI` | Evaluar componente por componente | El host ya tiene componentes. Reutilizar solo los que no existan o sean necesarios para articulos. |
| `Layout` | No copiar completo | Usar layout del host. Solo ajustar enlaces al sidebar/menu. |
| `Models` | Copiar modelos propios del modulo | Revisar `ArticleModels.cs` por posible legado antes de fusionar. |
| `Utils` | Copiar utilidades necesarias | `ExportJsInterop`, `ArticlesMapper`, `StorageJsInterop` segun dependencias reales. |
| `Configuration/ServiceCollectionExtensions.cs` | No copiar completo | Extraer solo registro de clientes del modulo. |
| `wwwroot` | Adaptar | Revisar scripts, css, assets y dependencias antes de mezclar con host. |

### Shared

| Carpeta / archivo | Accion recomendada | Motivo |
| --- | --- | --- |
| `DTOs/Articles` | Copiar | Contratos centrales del modulo. |
| `DTOs/Imports` | Copiar como contratos de importacion de articulos | Riesgo medio por nombre generico. Ideal: subnamespace de produccion cientifica en fusion futura. |
| `DTOs/MassRegistration` | Copiar | Matriz de articulos. |
| `DTOs/Workflow` | Copiar/adaptar | Workflow de articulos. Evitar choque con workflow del host. |
| `DTOs/Reports` | Copiar/adaptar | Reporterias de articulos. Ideal: namespace `ScientificProduction.Reports`. |
| `DTOs/Intelligence` | Copiar/adaptar | IA de articulos. |
| `DTOs/ExternalApis` | Copiar/adaptar | APIs externas para articulos. |
| `DTOs/Configuration` | Adaptar | Formularios dinamicos de articulos; puede chocar con configuracion del host. |
| `DTOs/Catalogs` | Adaptar | Comparar con catálogos del host. |
| `DTOs/Auth` | No copiar completo | Usar contratos auth del host. |
| `DTOs/Audit` | Adaptar | Usar auditoria del host si existe. |
| `Abstractions`, `Wrappers` | Evaluar | Si el host tiene `Result`/`ApiResult`, no duplicar. |
| `Validation/DynamicFieldValidationEngine.cs` | Copiar | Es clave para campos dinamicos de articulos. |

## Orden recomendado para migrar

1. Copiar DTOs propios del modulo: `Articles`, `Imports`, `MassRegistration`, `Workflow`, `Reports`, `Intelligence`, `ExternalApis`, `Venues`.
2. Copiar entidades de articulos, staging, workflow y catalogos propios al backend del host.
3. Registrar servicios de modulo sin tocar servicios existentes de proyectos.
4. Agregar controllers con rutas `api/scientific-production/*`.
5. Agregar vistas frontend bajo `/scientific-production/*`.
6. Conectar al layout/sidebar del host.
7. Mapear roles del host a capacidades del modulo.
8. Probar registro individual, matriz, workflow, ingesta, reportería e IA.
9. Solo despues integrar `Article -> Project` real del host.
10. Solo despues evaluar repositorios/unit of work.

## Puntos que no deben hacerse en la primera fusion

- Reemplazar Identity del host.
- Mover el sistema de proyectos.
- Fusionar ambos `AppDbContext` copiando archivos completos.
- Unificar DW de proyectos y articulos sin fase intermedia.
- Convertir todo articulos a repositorios antes de validar la migracion.
- Usar `Project` de articulos como si fuera el `Project` real del host.

## Paquete tecnico de integracion

Esta seccion resume lo que el sistema anfitrion de proyectos necesitara recibir para montar el modulo de articulos como `scientific-production`. La intencion es migrar capacidades del modulo, no reemplazar infraestructura global del host.

### Dependencias NuGet backend

| Paquete | Uso en articulos | Accion en el host |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore.SqlServer` | Persistencia OLTP y lectura DW/reporteria. | Agregar solo si el host no lo tiene o si requiere alinear version. |
| `Microsoft.EntityFrameworkCore.Design` | Soporte de migraciones/desarrollo. | Mantener como dependencia de desarrollo si se generan migraciones. |
| `Microsoft.EntityFrameworkCore.Tools` | Herramientas EF. | Mantener como dependencia de desarrollo. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API actual con JWT. | No duplicar si el host ya gobierna autenticacion. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity actual de articulos. | No copiar como autoridad nueva; usar Identity del host. |
| `System.IdentityModel.Tokens.Jwt` | Lectura/emision de tokens. | Usar version del host si ya existe. |
| `Swashbuckle.AspNetCore` | Swagger en desarrollo. | Usar configuracion global del host. |
| `Microsoft.AspNetCore.OpenApi` | OpenAPI. | Usar configuracion global del host. |
| `ClosedXML` | Exportacion Excel. | Necesario para exportes de reportería/datasets si el host no tiene alternativa. |
| `QuestPDF` | Generacion de reportes PDF. | Necesario para PDF institucional y reportes de autores. |
| `ScottPlot` | Soporte de graficos renderizados/exportables. | Mantener si se conserva exportacion de graficos desde backend. |
| `Microsoft.ML` | Motor base de IA. | Necesario para entrenamiento/prediccion. |
| `Microsoft.ML.TimeSeries` | Forecast temporal SSA. | Necesario para prediccion de produccion cientifica. |

### Dependencias NuGet frontend

| Paquete | Uso en articulos | Accion en el host |
| --- | --- | --- |
| `Microsoft.AspNetCore.Components.WebAssembly` | Frontend Blazor WASM actual. | No duplicar si el host ya es Blazor compatible. |
| `Microsoft.AspNetCore.Components.Authorization` | Autorizacion en UI. | Usar mecanismo del host. |
| `Microsoft.Extensions.Http` | Clientes HTTP tipados. | Mantener si el host no lo registra. |
| `Blazored.LocalStorage` | Token/local state actual. | No duplicar si el host ya maneja sesion/token. |
| `Blazored.Toast` | Mensajes de UI. | Mantener o mapear al sistema de notificaciones del host. |
| `System.IdentityModel.Tokens.Jwt` | Lectura de claims/token en cliente. | Usar solo si el host mantiene JWT en cliente. |

### Registros DI backend que deben migrarse

Servicios propios del modulo que si deben registrarse:

```text
IArticlesService -> ArticlesService
IVenuesService -> VenuesService
IConfigurationFormsService -> ConfigurationFormsService
IArticleRegistrationService -> ArticleRegistrationService
IArticleAggregatePersistenceService -> ArticleAggregatePersistenceService
IBulkImportService -> BulkImportService
IWorkflowService -> WorkflowService
IRegistrationWorkflowSettingsService -> RegistrationWorkflowSettingsService
IRegistrationMatrixService -> RegistrationMatrixService
IExternalApiExplorerService -> ExternalApiExplorerService
IInstitutionalReportingService -> InstitutionalReportingService
IReportingPerformanceMetricsService -> ReportingPerformanceMetricsService
IInstitutionalIntelligenceService -> InstitutionalIntelligenceService
IInstitutionAuthorDirectoryService -> LocalInstitutionAuthorDirectoryService
```

Servicios de plataforma que deben adaptarse al host, no copiarse automaticamente:

```text
IAuthService -> IdentityAuthService
IIdentityAdministrationService -> IdentityAdministrationService
ITokenService -> TokenService
IAuditLogger -> AuditLogger
```

El host debe conservar su autenticacion, usuarios, roles y auditoria si ya existen. El modulo de articulos solo debe aportar capacidades y llamadas operativas.

Tambien se requiere:

```text
AddMemoryCache()
HttpClient "external-api-explorer" con timeout controlado
ExternalApiExplorerOptions desde "ExternalApis"
InstitutionIdentityOptions desde "InstitutionIdentity"
ReportingRefreshQueue como singleton + IReportingRefreshQueue + hosted service
```

`ReportingRefreshQueue` es importante porque permite refrescar reportería automaticamente despues de procesamiento o ingesta. Si no se registra, el sistema seguiria guardando datos, pero el usuario tendria que refrescar reportería manualmente.

### DbContexts y bases de datos

El sistema actual usa dos contextos principales:

| Contexto | Funcion | Ruta de integracion |
| --- | --- | --- |
| `AppDbContext` | OLTP: articulos, autores, catalogos, campos dinamicos, staging, workflow, seguridad actual. | No copiar completo. Agregar entidades del modulo al contexto del host o crear configuraciones EF por modulo. |
| `ReportingDbContext` | Lectura del DW mediante vistas `dw.*` y ejecucion ETL. | Registrar separado contra `ReportingConnection` o `DwConnection`. |

Existe tambien `DwDbContext` con entidades dimensionales y migraciones DW. En la app actual no esta registrado en DI de runtime; debe tratarse como contexto de migracion/modelado del DW. En la fusion conviene mantener DW de articulos separado inicialmente y validar sus scripts antes de unificarlo con cualquier DW de proyectos.

Connection strings requeridas:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...OLTP del host...",
    "ReportingConnection": "...DW de produccion cientifica...",
    "DwConnection": "...fallback DW si no existe ReportingConnection..."
  }
}
```

### Configuracion requerida

Backend:

```json
{
  "ExternalApis": {
    "Scopus": { "ApiKey": "", "InstToken": "" },
    "Crossref": { "ApiKey": "", "InstToken": "" },
    "OpenAlex": { "ApiKey": "", "InstToken": "" },
    "SemanticScholar": { "ApiKey": "", "InstToken": "" }
  },
  "InstitutionIdentity": {
    "Enabled": false,
    "SourceType": "None",
    "BaseUrl": "",
    "ApiKey": "",
    "AuthorsEndpoint": ""
  }
}
```

Frontend:

```json
{
  "ApiBaseUrl": "https://host-institucional"
}
```

`Jwt`, CORS, HSTS, HTTPS, Swagger y DataProtection deben quedar gobernados por el sistema de proyectos. No conviene copiar el `Program.cs` de articulos completo.

### Endpoints a montar

El host debe montar controllers del modulo bajo:

```text
api/scientific-production/*
```

Las rutas antiguas pueden conservarse temporalmente si se necesita compatibilidad, pero la fusion deberia consumir las rutas preparadas:

```text
api/scientific-production/articles
api/scientific-production/articles/aggregate
api/scientific-production/import-batches
api/scientific-production/workflows/import-batches
api/scientific-production/registration-matrices
api/scientific-production/registration-workflow-settings
api/scientific-production/reporting
api/scientific-production/intelligence
api/scientific-production/external-api-explorer
api/scientific-production/catalogs
api/scientific-production/config
api/scientific-production/venues
```

### Clientes frontend a registrar

Clientes propios del modulo:

```text
IArticlesClient -> ArticlesClient
IVenuesClient -> VenuesClient
ICatalogsService -> CatalogsService
IFormConfigurationClient -> FormConfigurationClient
IBulkImportClient -> BulkImportClient
IWorkflowClient -> WorkflowClient
IRegistrationWorkflowSettingsClient -> RegistrationWorkflowSettingsClient
IRegistrationMatrixClient -> RegistrationMatrixClient
IExternalApiExplorerClient -> ExternalApiExplorerClient
IInstitutionalReportingClient -> InstitutionalReportingClient
IInstitutionalIntelligenceClient -> InstitutionalIntelligenceClient
InstitutionalReportingPageStateStore
ExportJsInterop
IApiClient -> ApiClient
```

Clientes/servicios que deben adaptarse al host:

```text
ITokenStore
IAuthClient
IIdentityAdministrationClient
AuthMessageHandler
JwtAuthStateProvider
AuthenticationStateProvider
```

Si el sistema de proyectos ya tiene autenticacion, el modulo de articulos debe usar su handler/token provider en vez de registrar los propios.

### Archivos estaticos frontend

Archivos del modulo que deben revisarse al migrar:

```text
wwwroot/js/registerArticle.js
wwwroot/js/export.js
wwwroot/js/echartsInterop.js
wwwroot/css/app.css
wwwroot/css/site.css
wwwroot/tailwind.css
wwwroot/images/logo-uta.png
```

No copiar `index.html`, `appsettings.json` ni layout completo si el host ya los tiene. Hay que incorporar solo scripts, estilos y assets que las vistas de articulos usen realmente.

### Scripts SQL y seeds

Scripts propios detectados:

```text
tesisproject.backend/Services/Modules/Reporting/Sql/CreateReportingPerformanceMetrics.sql
tesisproject.backend/Services/Modules/Intelligence/Sql/CreateIntelligenceTrainingHistory.sql
tesisproject.backend/Services/Modules/Intelligence/Sql/SeedIntelligenceTrainingSample.sql
```

Uso recomendado:

- `CreateReportingPerformanceMetrics.sql`: migrar si se mantiene medicion de tiempos/reporteria para tesis y auditoria.
- `CreateIntelligenceTrainingHistory.sql`: migrar si se conserva modulo IA.
- `SeedIntelligenceTrainingSample.sql`: solo para demo/pruebas, no para produccion.
- Scripts/migraciones DW: validar aparte. No copiar migraciones DW a ciegas dentro del host.

### Startup y semillas

`ValidateConfiguredDatabaseAsync` contiene mezcla de validacion, creacion incremental de tablas, roles demo y usuarios demo. En la fusion no debe copiarse entero.

Extraer solo:

```text
EnsureInstitutionalSettingsSchemaAsync
EnsureRegistrationMatrixModuleTablesAsync
EnsureWorkflowModuleSchemaAsync
workflowService.EnsureSeedDataAsync()
roles/capacidades del modulo scientific-production
```

No migrar a produccion:

```text
admin@local.test
autor.demo@uta.edu.ec
uodide.demo@uta.edu.ec
tecnica.demo@uta.edu.ec
passwords demo
```

### Orden tecnico de integracion dentro del host

1. Crear carpeta/namespace de modulo `ScientificProduction`.
2. Copiar DTOs propios del modulo.
3. Copiar entidades propias y agregarlas al contexto del host mediante configuraciones controladas.
4. Registrar servicios de dominio del modulo.
5. Registrar opciones `ExternalApis` e `InstitutionIdentity`.
6. Registrar `ReportingDbContext` contra el DW.
7. Registrar `ReportingRefreshQueue` si se mantiene sincronizacion automatica.
8. Montar controllers con rutas `api/scientific-production/*`.
9. Copiar vistas frontend bajo `/scientific-production/*`.
10. Conectar vistas al layout/sidebar del host.
11. Mapear roles del host a capacidades del modulo.
12. Ejecutar scripts/migraciones OLTP necesarias.
13. Ejecutar scripts DW y validar ETL.
14. Probar flujos completos.

### Checklist minimo de pruebas de fusion

| Flujo | Validacion esperada |
| --- | --- |
| Autenticacion | Usuario del host accede a vistas de articulos segun permisos. |
| Sidebar/menu | El host muestra `scientific-production` sin romper navegacion de proyectos. |
| Registro individual | Guarda/envia articulo a workflow. |
| Matriz | Valida fila por fila y envia lote. |
| Workflow autor | Permite seguimiento, correccion y reenvio. |
| Workflow UODIDE | Permite tomar/no tomar caso, devolver o avanzar. |
| Workflow area tecnica | Permite validar y procesar definitivamente. |
| Ingesta externa | Scopus crea/procesa lotes sin duplicar DOI. |
| Reporteria | ETL refresca y los graficos reflejan registros nuevos. |
| Exportes | Excel/PDF/dataset funcionan. |
| IA | Dashboard carga, reentrena y registra historial. |
| Seguridad | Un usuario sin permiso no ve ni ejecuta acciones del modulo. |

## Analisis AppDbContext y entidades OLTP

El `AppDbContext` actual de articulos hereda de:

```text
IdentityDbContext<ApplicationUser, ApplicationRole, string>
```

Esto es correcto para el sistema aislado de articulos, pero en la fusion no debe copiarse completo porque el sistema de proyectos debe conservar su propio contexto, usuarios y roles. La ruta segura es migrar entidades y configuraciones de articulos como bloque modular, no el `AppDbContext` entero.

### Clasificacion de entidades

| Grupo | Entidades | Decision recomendada |
| --- | --- | --- |
| Dominio cientifico central | `Article`, `ArticleParticipant`, `ArticleFile`, `ArticleIndexing` | Migrar. Son el nucleo del modulo de produccion cientifica. |
| Publicaciones / revistas | `Venue`, `VenueMetric`, `IndexingSource`, `PublicationStatus` | Migrar como subdominio de articulos. |
| Catalogos academicos | `AcademicTerm`, `ResearchLine`, `Faculty`, `BroadField`, `SpecificField`, `DetailedField` | Migrar o mapear a catalogos institucionales del host si ya existen. Inicialmente es mas seguro migrarlos aislados. |
| Formularios dinamicos | `FieldCatalogEntry`, `DynamicFieldOption`, `FormDefinition`, `FormFieldDefinition`, `DynamicFieldValue`, `ArticleParticipantDynamicFieldValue` | Migrar. Son necesarios para mantener formularios configurables y carga flexible. |
| Staging/importacion | `ImportBatch`, `ImportBatchRow`, `ImportBatchRowValue`, `ImportBatchError` | Migrar como staging de articulos. Riesgo de nombre medio si el host tambien maneja importaciones. |
| Workflow de articulos | `WorkflowDefinition`, `WorkflowStageDefinition`, `WorkflowInstance`, `WorkflowStageInstance`, `WorkflowActionLog` | Migrar con aislamiento semantico. En el host debe entenderse como workflow de produccion cientifica, no workflow global. |
| Matriz de registro | `RegistrationMatrix`, `RegistrationMatrixColumn`, `RegistrationMatrixRow`, `RegistrationMatrixCell` | Migrar. Pertenece al registro masivo/manual de articulos. |
| IA | `IntelligenceTrainingRun`, `IntelligenceTrainingAlgorithmMetric` | Migrar si el modulo IA de articulos se conserva. |
| Metricas tesis/reporteria | `ReportingPerformanceMetric` | Migrar si se conserva medicion cuantitativa de tiempos de reportería. |
| Auditoria | `AuditLog` | Adaptar. Si el host tiene auditoria, mapear hacia la auditoria global. |
| Proyecto temporal | `Project` | No migrar como entidad central. Mapear a proyecto real del host o conservar solo como referencia temporal con otro nombre. |
| Plataforma/seguridad | `ApplicationUser`, `ApplicationRole`, tablas Identity | No migrar. Usar usuarios/roles del sistema de proyectos. |

### Riesgos por choque de nombres

| Nombre actual en articulos | Riesgo al fusionar | Accion segura |
| --- | --- | --- |
| `Project` / tabla `Projects` | Choca directamente con el dominio principal del sistema de proyectos. | No copiar como `Project`. Usar relacion hacia proyecto real del host o renombrar a referencia temporal si se necesita conservar. |
| `Workflow*` | El host puede tener su propio workflow de proyectos. | Mantener bajo namespace/carpeta `ScientificProduction.Workflow` y rutas `scientific-production`. |
| `ImportBatch*` | El host puede tener importaciones de proyectos. | Mantener como staging de articulos; idealmente tabla o namespace claramente asociado a produccion cientifica. |
| `FieldCatalog`, `FormDefinitions`, `FormFields` | El host puede tener configuracion/formularios propios. | Mantener como configuracion de articulos, no configuracion global del host. |
| `AuditLog` | Auditoria suele ser transversal. | Preferir auditoria global del host si existe. |
| `ApplicationUser` / `ApplicationRole` | Doble Identity rompe autenticacion y permisos. | Usar Identity del host. |

### Dependencias a Identity dentro de entidades

Algunas entidades de articulos apuntan a usuarios actuales:

```text
ImportBatch.CreatedByUserId -> ApplicationUser
WorkflowInstance.SubmittedByUserId -> ApplicationUser
WorkflowStageInstance.AssignedToUserId -> ApplicationUser
WorkflowStageInstance.ApprovedByUserId -> ApplicationUser
WorkflowActionLog.PerformedByUserId -> ApplicationUser
RegistrationMatrix.CreatedByUserId -> string
IntelligenceTrainingRun.CreatedByUserId -> string
ReportingPerformanceMetric.UserId -> string
AuditLog.UserId -> string
```

En la fusion, esas columnas pueden mantenerse como `string` y relacionarse contra el usuario del host. Lo que no debe hacerse es crear otra tabla de usuarios paralela. Para las navegaciones que hoy usan `ApplicationUser`, hay dos rutas seguras:

1. Cambiar la navegacion hacia la entidad de usuario del host.
2. Mantener solo `UserId` como columna y resolver nombre/usuario desde servicios del host.

La segunda opcion es menos invasiva para una primera fusion.

### Configuracion EF que conviene extraer

Hoy todo el modelado vive dentro de `OnModelCreating` de `AppDbContext`. Para fusionar con menor riesgo, conviene extraerlo a configuraciones por entidad:

```text
ScientificProduction/Data/Configurations/ArticleConfiguration.cs
ScientificProduction/Data/Configurations/ArticleParticipantConfiguration.cs
ScientificProduction/Data/Configurations/VenueConfiguration.cs
ScientificProduction/Data/Configurations/ImportBatchConfiguration.cs
ScientificProduction/Data/Configurations/WorkflowConfiguration.cs
ScientificProduction/Data/Configurations/DynamicFormsConfiguration.cs
ScientificProduction/Data/Configurations/RegistrationMatrixConfiguration.cs
ScientificProduction/Data/Configurations/IntelligenceConfiguration.cs
ScientificProduction/Data/Configurations/ReportingPerformanceMetricConfiguration.cs
```

Esto permite que el `AppDbContext` del sistema de proyectos aplique:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArticleConfiguration).Assembly);
```

sin copiar el contexto completo de articulos.

### Recomendacion sobre tablas y esquemas

Para la primera fusion hay dos opciones:

| Opcion | Ventaja | Riesgo |
| --- | --- | --- |
| Mantener nombres actuales de tablas | Menos cambios y menor riesgo funcional. | Nombres genericos como `WorkflowDefinition` o `ImportBatch` pueden confundirse con el host. |
| Mover a esquema SQL `scientific` o prefijar tablas | Aislamiento claro para produccion cientifica. | Requiere migracion SQL y pruebas mas amplias. |

Ruta recomendada:

1. Primera fase: mantener nombres actuales para no romper funcionalidad.
2. Segunda fase: si el host ya tiene tablas con nombres iguales, crear esquema `scientific` o prefijos controlados.
3. No cambiar nombres de tablas antes de tener el mapa real de tablas del sistema de proyectos.

### Adaptacion con repositorios y UnitOfWork del host

El sistema de articulos no usa repositorios ni UnitOfWork, pero eso no bloquea la fusion. Lo importante es no intentar convertir todo antes de migrar.

Ruta segura:

1. Migrar servicios actuales usando `DbContext` directamente.
2. Compilar y validar flujo completo.
3. Crear interfaces de repositorio solo para puntos de alto acoplamiento:
   - articulos
   - staging/importacion
   - workflow
   - reportería/ETL si aplica
4. Adaptar esos repositorios al UnitOfWork del host por fases.

Intentar convertir todo el modulo a repositorios antes de fusionar elevaria mucho el riesgo y mezclaria dos cambios grandes al mismo tiempo.

### Primer corte recomendado

Para preparar articulos sin romperlo ahora:

1. No modificar tablas ni entidades todavia.
2. Documentar `Project` como choque confirmado.
3. Extraer configuraciones EF por entidad sin cambiar comportamiento.
4. Compilar y probar.
5. Solo despues preparar una rama de migracion real hacia el host.

Estado aplicado:

- Configuraciones EF del nucleo de articulos extraidas a `Data/Configurations`.
- Configuraciones EF de catalogos academicos, campos OCDE y venues extraidas.
- Configuraciones EF de formularios dinamicos extraidas.
- Configuraciones EF de staging/importacion, workflow y matriz extraidas.
- Configuraciones EF de IA, metricas de reportería, auditoria y `Project` extraidas.
- `AppDbContext` conserva los mismos `DbSet` y aplica las configuraciones externas con `ApplyConfigurationsFromAssembly`, filtrando por el namespace del marcador `ScientificProductionModelConfigurationMarker`.
- No se cambiaron nombres de tablas, claves, indices ni relaciones.

Este cambio deja el contexto mas preparado para que el host de proyectos pueda aplicar configuraciones del modulo sin copiar el `AppDbContext` completo.

Patron listo para reutilizar en el host:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(ScientificProductionModelConfigurationMarker).Assembly,
    type => type.Namespace == typeof(ScientificProductionModelConfigurationMarker).Namespace);
```
