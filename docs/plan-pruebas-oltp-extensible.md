# Plan de Pruebas OLTP Extensible

## Objetivo

Validar el sistema actual sobre `TesisDB_Extensible` con un enfoque de mejora segura:

1. confirmar que los modulos nuevos funcionan end-to-end
2. detectar regresiones antes de introducir cambios adicionales
3. medir rendimiento funcional en escenarios reales y prudentes
4. dejar evidencia suficiente para decidir mejoras con bajo riesgo

## Contexto del modelo actual

El sistema trabaja sobre un modelo hibrido en SQL Server:

1. capa fisica relacional del dominio:
   - `Articles`
   - `ArticleParticipants`
   - `Venues`
   - `VenueMetrics`
   - catalogos fisicos como `AcademicTerms`, `PublicationStatuses`, `ResearchLines`, `BroadFields`, `SpecificFields`, `DetailedFields`
2. capa dinamica de metadata y formularios:
   - `FieldCatalog`
   - `FormDefinitions`
   - `FormFields`
   - `DynamicFieldOptions`
   - `DynamicFieldValues`
   - `ArticleParticipantDynamicFieldValues`
3. capa de staging e integracion:
   - `ImportBatch`
   - `ImportBatchRow`
   - `ImportBatchRowValue`
   - `ImportBatchError`
   - `ApiFieldMappings`
   - `ExternalIntegrationLog`

La consecuencia practica es que toda mejora debe respetar tanto:

1. las columnas fisicas y relaciones del agregado principal
2. la resolucion dinamica de formularios y campos
3. el pipeline de staging, validacion y procesamiento

## Principios para cambios a partir de ahora

En esta fase el sistema ya no debe tratarse como un modulo en construccion libre, sino como una base operativa en validacion.

Reglas de trabajo:

1. no cambiar contratos de entrada o salida sin una prueba que capture el comportamiento actual
2. no tocar validaciones de negocio sin un caso de prueba previo y posterior
3. no rehacer modulos completos cuando el problema pueda aislarse
4. primero reproducir, despues corregir, despues volver a medir
5. cualquier optimizacion de rendimiento debe acompañarse con comparacion antes/despues
6. si una mejora afecta formularios, revisar tambien carga masiva y APIs externas

## Alcance de pruebas

### Modulo 1. Configuracion dinamica

Objetivo:

1. confirmar que el sistema resuelve y administra formularios mutables sin romper el registro

Cobertura minima:

1. listar formularios
2. crear formulario
3. editar formulario
4. activar o desactivar formulario
5. listar campos por entidad
6. crear campo dinamico
7. actualizar campo
8. agregar campo a formulario
9. editar asignacion de campo
10. eliminar asignacion
11. administrar opciones dinamicas
12. administrar catalogos base
13. obtener formulario resuelto
14. verificar que los cambios se reflejan en la UI de registro

Riesgos vigilados:

1. formularios activos inconsistentes
2. requireds mal resueltos
3. orden visual incorrecto
4. opciones dinamicas que no cargan
5. dependencias OCDE rotas

### Modulo 2. Registro manual dinamico

Objetivo:

1. confirmar que el agregado `Article` se registra correctamente desde formularios mutables

Cobertura minima:

1. carga del formulario activo de articulo
2. carga del formulario activo de participante
3. carga de catalogos fisicos y dependientes
4. registro exitoso de articulo simple
5. registro con multiples participantes
6. validacion de campos requeridos fisicos
7. validacion de campos requeridos dinamicos
8. validacion de DOI duplicado
9. validacion de indices de participante duplicados
10. guardado de campos dinamicos de articulo
11. guardado de campos dinamicos de participante
12. resolucion o creacion de venue y metricas
13. consistencia transaccional del agregado

Riesgos vigilados:

1. datos guardados a medias
2. venue duplicado o mal resuelto
3. dynamic fields no persistidos
4. errores de validacion poco claros
5. divergencia entre formulario resuelto y payload aceptado

### Modulo 3. Carga masiva con staging

Objetivo:

1. confirmar que el pipeline `upload -> staging -> validate -> correct -> process` funciona y escala razonablemente

Cobertura minima:

1. generar plantilla
2. subir CSV o Excel valido
3. crear lote en staging
4. ver preview del lote
5. validar lote
6. detectar errores por fila y por campo
7. corregir fila y revalidar
8. procesar lote valido
9. revisar resumen final
10. confirmar creacion real de articulos, venues y participantes
11. confirmar trazabilidad de errores en `ImportBatchError`
12. revisar lote creado desde fuente externa

Riesgos vigilados:

1. columnas no mapeadas o mapeadas incorrectamente
2. normalizacion erronea de catalogos
3. errores no visibles para el usuario
4. preview lento con lotes medianos o grandes
5. proceso final inconsistente respecto al staging

### Modulo 4. Explorador de APIs externas

Objetivo:

1. confirmar que una consulta externa puede producir un lote util para el flujo nuevo

Cobertura minima:

1. listar proveedores
2. ejecutar consulta por texto o DOI
3. revisar articulo enriquecido
4. crear lote externo en staging
5. validar lote externo
6. procesar lote externo si la validacion es correcta
7. revisar trazabilidad en `ExternalIntegrationLog`

Riesgos vigilados:

1. mappings incompletos
2. payloads externos con campos parciales
3. errores de enriquecimiento poco claros
4. lote creado pero inutilizable

## Tipos de prueba

### 1. Pruebas de humo

Sirven para confirmar que el sistema sigue vivo luego de cambios puntuales.

Minimo por corrida:

1. abrir configuracion
2. abrir registro dinamico
3. registrar un articulo simple
4. generar plantilla
5. subir un lote pequeno
6. validar lote

### 2. Pruebas funcionales por modulo

Sirven para cubrir reglas de negocio y consistencia.

Se ejecutan cuando:

1. se modifica backend de validacion
2. se modifica metadata de formularios
3. se toca el pipeline de carga masiva
4. se cambia persistencia de venues o participantes

### 3. Pruebas end-to-end manuales

Sirven para confirmar comportamiento real desde UI.

Se debe recorrer:

1. configuracion -> cambio visible en formulario
2. registro manual -> persistencia correcta
3. carga masiva -> staging -> validacion -> correccion -> proceso
4. API externa -> staging -> validacion -> proceso

### 4. Pruebas de carga funcional

Sirven para medir tiempos y usabilidad bajo volumen prudente.

Base actual:

1. `tests/load/generate-bulk-import-csv.ps1`
2. `tests/load/invoke-bulk-import-scenario.ps1`
3. `tests/load/invoke-registration-load.ps1`

## Ambientes y precondiciones

Antes de probar:

1. backend levantado en `http://localhost:5040`
2. frontend compilando correctamente
3. base `TesisDB_Extensible` accesible
4. formularios activos para `Article` y `ArticleParticipant`
5. catalogos base cargados
6. si se prueba API externa, proveedor configurado cuando aplique

## Plan de ejecucion por fases

### Fase A. Linea base de seguridad

Objetivo:

1. capturar el comportamiento actual antes de tocar logica sensible

Casos:

1. consultar formulario resuelto de articulo
2. consultar formulario resuelto de participante
3. registrar un articulo minimo valido
4. generar una plantilla
5. subir y validar un lote pequeno

Salida esperada:

1. tener una referencia clara de comportamiento actual

### Fase B. Validacion funcional por modulo

Objetivo:

1. comprobar cobertura minima de negocio

Casos:

1. configuracion dinamica
2. registro manual
3. carga masiva
4. APIs externas

Salida esperada:

1. lista de hallazgos clasificados por severidad

### Fase C. Pruebas de carga funcional

Objetivo:

1. medir tiempos y estabilidad sin llegar aun a benchmarking agresivo

Escenarios iniciales:

1. `Smoke`
   - registro manual: `10`
   - carga masiva: `50` filas sin error
2. `Errores controlados`
   - carga masiva: `100`
   - `ErrorEvery 10`
3. `Volumen medio`
   - registro manual: `50`
   - carga masiva: `250`
4. `Volumen alto prudente`
   - carga masiva: `1000`
   - con y sin errores

Salida esperada:

1. tiempos de upload
2. tiempos de validate
3. tiempos de process
4. tiempo promedio de registro manual
5. sintomas de lentitud de UI

### Fase D. Endurecimiento guiado por evidencia

Solo despues de ejecutar las fases A, B y C.

Mejoras candidatas:

1. rendimiento del preview de lotes
2. paginacion o segmentacion del lote
3. procesamiento por bloques
4. mensajes de error mas utiles
5. ajustes de normalizacion en carga externa

## Matriz base de casos

| ID | Modulo | Caso | Resultado esperado | Estado |
|---|---|---|---|---|
| CFG-01 | Configuracion | Obtener formulario resuelto activo | JSON consistente y renderizable | Pendiente |
| CFG-02 | Configuracion | Crear campo dinamico | Campo visible para asignacion | Pendiente |
| CFG-03 | Configuracion | Agregar campo a formulario | Campo aparece en UI de registro | Pendiente |
| REG-01 | Registro | Registrar articulo minimo valido | Articulo y participante creados | Pendiente |
| REG-02 | Registro | Registrar con DOI duplicado | Error claro y sin persistencia parcial | Pendiente |
| REG-03 | Registro | Registrar con dinamicos | Valores guardados en tablas dinamicas | Pendiente |
| REG-04 | Registro | Registrar con multiples participantes | Participantes persistidos con indice correcto | Pendiente |
| IMP-01 | Importacion | Generar plantilla | Archivo descargable y coherente con metadata | Pendiente |
| IMP-02 | Importacion | Upload de lote valido | Staging creado con preview correcto | Pendiente |
| IMP-03 | Importacion | Validar lote con errores | Errores visibles por fila y campo | Pendiente |
| IMP-04 | Importacion | Corregir fila y revalidar | Error resuelto y lote actualizado | Pendiente |
| IMP-05 | Importacion | Procesar lote valido | Articulos persistidos y lote actualizado | Pendiente |
| EXT-01 | API externa | Consultar proveedor | Respuesta mapeable y visible | Pendiente |
| EXT-02 | API externa | Crear lote desde articulo externo | Lote en staging usable | Pendiente |

## Evidencia a registrar por corrida

Por cada prueba relevante guardar:

1. fecha y hora
2. modulo
3. caso
4. insumo usado
5. resultado obtenido
6. tiempos si aplica
7. errores observados
8. decision:
   - pasa
   - pasa con observaciones
   - falla
9. accion posterior requerida

## Criterios de salida para permitir mejoras de bajo riesgo

Antes de entrar a optimizaciones o refactors mas delicados, deberiamos tener:

1. `Smoke` en verde
2. `Errores controlados` ejecutado y entendido
3. registro manual validado con casos felices y de error
4. un lote procesado correctamente desde UI
5. un lote externo creado y validado al menos una vez
6. lista priorizada de problemas reales, no supuestos

## Secuencia inmediata recomendada

### Paso 1. Linea base funcional

1. confirmar formularios activos
2. registrar un articulo simple desde UI
3. generar plantilla y subir lote pequeno

### Paso 2. Escenario Smoke

1. correr `invoke-registration-load.ps1 -Count 10`
2. generar CSV de `50`
3. correr `invoke-bulk-import-scenario.ps1 -Validate`

### Paso 3. Escenario con errores controlados

1. generar CSV de `100` con `ErrorEvery 10`
2. subir lote
3. validar
4. revisar errores
5. corregir un subconjunto
6. revalidar y procesar si corresponde

### Paso 4. Consolidar hallazgos

1. clasificar por:
   - bloqueo funcional
   - inconsistencia de datos
   - UX
   - rendimiento
2. corregir primero solo lo que tenga evidencia

## Relacion con artefactos actuales

Artefactos existentes que debemos reutilizar antes de crear nuevos:

1. `tests/load/README.md`
2. `tests/load/generate-bulk-import-csv.ps1`
3. `tests/load/invoke-bulk-import-scenario.ps1`
4. `tests/load/invoke-registration-load.ps1`
5. `docs/roadmap-oltp-extensible.md`

## Nota operativa

Desde este punto, cualquier cambio sensible en:

1. `ArticleRegistrationService`
2. `BulkImportService`
3. `ConfigurationController` o su servicio
4. formularios activos en base de datos

debe venir acompanado por:

1. caso de prueba objetivo
2. reproduccion antes del cambio
3. validacion despues del cambio
