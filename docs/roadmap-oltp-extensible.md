# Roadmap Oltp Extensible

## Objetivo

Completar tres capacidades principales sobre el nuevo modelo `TesisDB_Extensible`:

1. Modulo de configuracion
2. Modulo de registro manual
3. Modulo de carga masiva

El modulo de registro manual no se implementara como un formulario fijo. Se implementara como un motor de formularios mutables, generado desde configuracion y capaz de cambiar sin rehacer la pantalla cada vez.

La implementacion se hara manteniendo la estructura actual de proyectos:

- `tesisproject.backend`
- `tesisproject.frontend`
- `tesisproject.shared`

Dentro de `tesisproject.backend` se recomienda organizar la evolucion con subcarpetas tipo Clean Architecture:

- `Domain`
- `Application`
- `Infrastructure`
- `Api`

## Fuente de verdad por capa

- Base de datos SQL Server: fuente de verdad fisica del modelo extensible
- `tesisproject.backend`: fuente de verdad de persistencia, reglas y casos de uso
- `tesisproject.shared`: contratos compartidos entre frontend y backend
- `tesisproject.frontend`: renderizado, estado y experiencia de usuario

## Alcance funcional cerrado

### 1. Modulo de configuracion

Debe permitir:

- listar catalogos base
- crear y editar catalogos
- listar campos de `FieldCatalog`
- activar o desactivar campos
- marcar campos requeridos
- cambiar orden de visualizacion
- resolver formularios desde `FormDefinitions` y `FormFields`
- listar campos dinamicos por entidad
- listar opciones de `DynamicFieldOptions`
- listar mapeos externos de `ApiFieldMappings`

Resultado esperado:

- el sistema sabe que campos renderizar
- el sistema sabe que catalogos cargar
- el sistema sabe que campos son requeridos
- el sistema sabe que campos son dinamicos
- el sistema sabe que reglas de UI y guardado aplicar

### 2. Modulo de registro manual

Debe permitir:

- generar formularios automaticamente segun la configuracion activa
- obtener un formulario resuelto para `Article`
- obtener un formulario resuelto para `ArticleParticipant`
- permitir que el formulario cambie cuando cambien `FieldCatalog`, `FormDefinitions` o `FormFields`
- guardar un articulo completo con:
  - datos fijos de `Articles`
  - datos de `ArticleParticipants`
  - datos de `DynamicFieldValues`
  - datos de `ArticleParticipantDynamicFieldValues`

Resultado esperado:

- el formulario puede mutar sin recodificar la UI
- el usuario puede cambiar configuracion y ver reflejado el cambio en el registro manual
- poder registrar un articulo completo sin SQL manual
- manejar multiples participantes
- guardar todo en una transaccion

### 3. Modulo de carga masiva

Debe permitir:

- generar plantilla de carga basada en `FieldCatalog` y `FormFields`
- recibir archivo o payload de filas
- poblar staging:
  - `ImportBatch`
  - `ImportBatchRow`
  - `ImportBatchRowValue`
- validar lote
- procesar lote
- devolver errores y resumen de resultados

Resultado esperado:

- pipeline completo de staging -> validacion -> procesamiento
- errores legibles para el usuario

## Mapa del nuevo modelo SQL a backend

### Nucleo de articulo

Tablas:

- `Articles`
- `ArticleParticipants`
- `ArticleFiles`
- `Venues`
- `VenueMetrics`
- `AcademicTerms`
- `PublicationStatuses`
- `ResearchLines`
- `BroadFields`
- `SpecificFields`
- `DetailedFields`

Backend:

- entidades de dominio del agregado `Article`
- repositorios de lectura y escritura
- servicio transaccional `RegisterArticleAggregate`

Nota:

- el backend no solo guarda datos; tambien debe resolver formularios renderizables a partir de la metadata activa

Shared:

- DTOs de lectura y escritura de articulo
- DTOs de catalogos

### Capa dinamica

Tablas:

- `FieldCatalog`
- `DynamicFieldOptions`
- `DynamicFieldValues`
- `ArticleParticipantDynamicFieldValues`
- `FormDefinitions`
- `FormFields`

Backend:

- casos de uso para resolver formularios
- validaciones de campos dinamicos
- adaptadores para persistencia de valores tipados

Shared:

- `DynamicFormDefinitionDto`
- `DynamicFieldDefinitionDto`
- `DynamicFieldOptionDto`
- `DynamicFieldValueDto`

### Capa de integracion y carga

Tablas:

- `ApiFieldMappings`
- `ExternalIntegrationLog`
- `ImportBatch`
- `ImportBatchRow`
- `ImportBatchRowValue`

Procedimientos almacenados ya validados:

- `sp_ValidateImportBatch_Article`
- `sp_ValidateImportBatch_ArticleParticipant`
- `sp_ProcessImportBatch_Article`
- `sp_RegisterExternalIntegrationResponse`
- `sp_TransformExternalArticleJsonToStaging_Debug`

Backend:

- servicios de staging
- servicios de validacion
- servicios de procesamiento

Shared:

- DTOs de plantilla
- DTOs de lote
- DTOs de errores de validacion
- DTOs de resultado de procesamiento

## Plan tecnico por fases

### Fase 0. Preparacion y alineacion

Objetivo:

- apuntar el backend a la nueva base
- dejar claro que el modelo nuevo vive primero en backend

Tareas:

- actualizar connection strings
- crear `AppDbContextExtensible` o adaptar `AppDbContext` cuidadosamente
- decidir si la migracion sera por reemplazo o por coexistencia temporal
- documentar entidades que quedan obsoletas o deben adaptarse

Entregable:

- backend conectando a `TesisDB_Extensible`

### Fase 1. Backend de configuracion

Objetivo:

- exponer toda la metadata necesaria para formularios mutables y catalogos

Casos de uso:

- `GetCatalogs`
- `GetFieldCatalogByEntity`
- `UpdateFieldConfiguration`
- `GetFormDefinition`
- `GetResolvedForm`
- `GetDynamicFieldOptions`
- `GetApiFieldMappings`

Endpoints sugeridos:

- `GET /api/config/catalogs/{catalogName}`
- `POST /api/config/catalogs/{catalogName}`
- `PUT /api/config/catalogs/{catalogName}/{id}`
- `GET /api/config/fields?entityName=Article`
- `PUT /api/config/fields/{fieldId}`
- `GET /api/config/forms/{formKey}`
- `GET /api/config/forms/{formKey}/resolved`
- `GET /api/config/options/{fieldId}`
- `GET /api/config/api-mappings?entityName=Article`

DTOs shared minimos:

- `CatalogItemDto`
- `FieldCatalogItemDto`
- `UpdateFieldCatalogRequest`
- `DynamicFormDefinitionDto`
- `DynamicFormSectionDto`
- `DynamicFieldDefinitionDto`
- `DynamicFieldOptionDto`

Contrato importante del formulario resuelto:

- debe venir listo para renderizar
- debe incluir entidad objetivo
- debe incluir grupos o secciones
- debe incluir orden
- debe incluir tipo de dato
- debe incluir si el campo es fijo o dinamico
- debe incluir si es visible, editable y requerido
- debe incluir placeholder, ayuda y validaciones configuradas
- debe incluir la fuente de catalogo cuando aplique
- debe incluir dependencias de campos cuando aplique
- debe incluir metadatos suficientes para repetir bloques como participantes

Entregable:

- backend capaz de decirle al frontend que debe renderizar y como debe renderizarlo sin hardcodear el formulario

### Fase 2. Backend de registro manual

Objetivo:

- registrar un articulo completo con una sola operacion transaccional usando un formulario resuelto dinamicamente

Caso de uso central:

- `RegisterArticleAggregate`

Subcapacidad obligatoria:

- el backend debe aceptar payloads generados por formularios mutables, no asumir una pantalla fija

Entrada del caso de uso:

- articulo fijo
- valores dinamicos de articulo
- participantes
- valores dinamicos de cada participante

Persistencia esperada:

- `Articles`
- `ArticleParticipants`
- `DynamicFieldValues`
- `ArticleParticipantDynamicFieldValues`

Servicios auxiliares:

- `DynamicFieldValidator`
- `CatalogReferenceValidator`
- `FormDefinitionResolver`

Endpoint sugerido:

- `POST /api/articles/aggregate`

DTOs shared minimos:

- `RegisterArticleAggregateRequest`
- `ArticleCoreInputDto`
- `ParticipantInputDto`
- `DynamicFieldValueInputDto`
- `RegisterArticleAggregateResponse`

Reglas clave:

- una sola transaccion
- el contrato de entrada debe ser estable aunque la composicion del formulario cambie
- validacion de campos requeridos resuelta desde metadata
- validacion de tipos de datos
- validacion de claves foraneas
- respuesta con errores de negocio claros

Entregable:

- registro manual funcional desde backend con soporte real para formularios mutables

### Fase 3. Pagina de registro manual

Objetivo:

- pantalla minima funcional basada en configuracion dinamica y mutabilidad de formularios

Flujo:

1. consultar formulario resuelto
2. cargar catalogos necesarios
3. renderizar campos por definicion
4. manejar dependencias entre catalogos
5. agregar y quitar participantes
6. guardar con `POST /api/articles/aggregate`

Frontend minimo:

- formulario dinamico de articulo
- seccion repetible de participantes
- mensajes de error por campo y por bloque
- soporte para `BroadField -> SpecificField -> DetailedField`
- capacidad de renderizar campos nuevos o desactivados sin cambiar el codigo de la pagina
- capacidad de reordenar visualmente segun metadata
- soporte para grupos o secciones del formulario
- soporte para tipos de campo configurables

Entregable:

- modulo de registro manual usable

### Fase 4. Backend de carga masiva

Objetivo:

- exponer desde API el flujo que ya probaste en SQL

Casos de uso:

- `GenerateImportTemplate`
- `UploadImportBatchToStaging`
- `ValidateImportBatch`
- `ProcessImportBatch`
- `GetImportBatchStatus`
- `GetImportBatchErrors`

Endpoints sugeridos:

- `GET /api/import/articles/template`
- `POST /api/import/articles/staging`
- `POST /api/import/articles/{batchId}/validate`
- `POST /api/import/articles/{batchId}/process`
- `GET /api/import/articles/{batchId}`
- `GET /api/import/articles/{batchId}/errors`

Implementacion recomendada:

- primero soportar carga por JSON o CSV simple
- despues agregar parsing de Excel real
- reutilizar SPs existentes para validacion y proceso

DTOs shared minimos:

- `ImportTemplateDto`
- `CreateImportBatchRequest`
- `ImportBatchDto`
- `ImportBatchRowDto`
- `ImportBatchErrorDto`
- `ProcessImportBatchResultDto`

Entregable:

- backend de carga masiva funcional sobre staging y SPs

### Fase 5. Pagina de carga masiva

Objetivo:

- dar una UI guiada para plantillas, validacion y proceso

Flujo:

1. descargar plantilla
2. subir archivo
3. validar lote
4. mostrar errores
5. procesar lote
6. mostrar resumen final

Entregable:

- modulo de carga masiva usable por usuario final

## Distribucion por proyecto

### Backend

Agregar o reorganizar carpetas:

- `Application/Configuration`
- `Application/Articles`
- `Application/Imports`
- `Domain/Articles`
- `Domain/Configuration`
- `Infrastructure/Persistence`
- `Infrastructure/SqlServer`
- `Api/Controllers`

Prioridades de implementacion:

1. configuracion
2. registro manual
3. carga masiva

### Shared

Agregar contratos minimos:

- DTOs de configuracion dinamica
- DTOs de registro agregado
- DTOs de importacion y lotes

No mover:

- entidades EF
- esquema SQL
- logica de persistencia

### Frontend

Agregar servicios:

- `IConfigurationClient`
- `IArticleAggregateClient`
- `IImportBatchClient`

Agregar paginas:

- `Configuration`
- `DynamicArticleRegister`
- `ArticleImport`

## Backlog inmediato recomendado

### Sprint 1

- cambiar conexion a nueva base
- crear modelo de configuracion en backend
- exponer `GET /api/config/forms/{formKey}/resolved`
- exponer catalogos base

Meta adicional:

- dejar establecido el contrato del motor de formularios mutables

Meta:

- frontend ya puede preguntar que campos debe mostrar y en que estructura debe construir el formulario

### Sprint 2

- implementar `RegisterArticleAggregate`
- guardar articulo, participantes y dinamicos
- agregar validaciones de negocio

Meta:

- backend de registro manual completo

### Sprint 3

- construir pagina dinamica de registro manual
- cargar formularios y catalogos
- guardar agregado completo

Meta:

- registro manual usable desde UI con formularios que mutan por configuracion

### Sprint 4

- exponer endpoints de carga masiva
- integrar validacion y procesamiento de lotes

Meta:

- backend de carga masiva completo

### Sprint 5

- construir pagina de carga masiva
- mostrar errores y resultados

Meta:

- carga masiva usable desde UI

## Riesgos principales

1. Intentar migrar todo el modelo a `shared`

Impacto:

- mezcla de contratos con persistencia

Mitigacion:

- dejar `shared` solo para DTOs y requests

2. Construir frontend antes del backend de configuracion

Impacto:

- formularios hardcodeados y retrabajo

Mitigacion:

- comenzar por `GET resolved form`

5. Implementar el registro manual como CRUD fijo

Impacto:

- el usuario no podra mutar formularios sin volver a desarrollar la pantalla

Mitigacion:

- tratar el registro manual como motor de formularios configurables desde el inicio

3. Guardar articulo y dinamicos en varios pasos sin transaccion

Impacto:

- inconsistencia de datos

Mitigacion:

- usar un caso de uso agregado con transaccion unica

4. Rehacer importacion desde cero ignorando los SPs validados

Impacto:

- perder la ventaja del trabajo SQL ya probado

Mitigacion:

- encapsular los SPs desde backend antes de reemplazarlos

## Decision recomendada

El orden oficial de trabajo sera:

1. Modulo de configuracion
2. Modulo de registro manual
3. Modulo de carga masiva

Y cada modulo se implementara primero en backend y luego en frontend.
