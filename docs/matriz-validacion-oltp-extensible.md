# Matriz de Validacion OLTP Extensible

Usar esta matriz para registrar resultados reales antes y despues de cambios sensibles.

## Corridas planificadas

| Orden | Fase | Caso | Herramienta | Estado | Observaciones |
|---|---|---|---|---|---|
| 1 | Linea base | CFG-01 Obtener formulario activo de `Article` | UI o API | Completado | Responde `ArticleManualForm` con secciones y campos coherentes. |
| 2 | Linea base | CFG-02 Obtener formulario activo de `ArticleParticipant` | UI o API | Completado | Responde `ArticleParticipantForm` con campos requeridos esperados. |
| 3 | Linea base | REG-01 Registro minimo valido | UI | Completado | API devolvio `articleId=1020` y `participantIds=[1017]`. |
| 4 | Linea base | IMP-01 Generar plantilla | UI | Completado | Descarga correcta, HTTP 200, archivo Excel de 10673 bytes. |
| 5 | Linea base | IMP-02 Upload y preview de lote pequeno | UI | Completado | Tras corregir parser CSV y script de prueba, el lote `BATCH-UI-20260325085434` quedo interpretable y validable. |
| 6 | Smoke | REG-LD-10 Registro manual x10 | `invoke-registration-load.ps1` | Completado | 10/10 exitosos, promedio 40.2 ms, max 239 ms, min 14 ms. |
| 7 | Smoke | IMP-LD-50 Carga masiva x50 sin errores | `invoke-bulk-import-scenario.ps1` | Completado | Lote `BATCH-UI-20260325090357`: 50/50 validas y 50/50 procesadas en `5041`. |
| 8 | Errores controlados | IMP-ERR-100 Carga x100 con errores | UI + script | Completado | Lote `BATCH-UI-20260325090427`: 90 validas y 10 con error, consistente con `ErrorEvery 10`. |
| 9 | Funcional | REG-02 DOI duplicado | UI o API | Pendiente | |
| 10 | Funcional | REG-03 Dynamicos de articulo y participante | UI o API | Pendiente | |
| 11 | Funcional | IMP-03 Correccion de fila y revalidacion | UI | Pendiente | |
| 12 | Funcional | IMP-04 Process final de lote valido | UI o API | Pendiente | |
| 13 | Integracion | EXT-01 Consulta a proveedor externo | UI | Pendiente | |
| 14 | Integracion | EXT-02 Crear lote externo | UI | Pendiente | |
| 15 | Volumen medio | REG-LD-50 Registro manual x50 | `invoke-registration-load.ps1` | Completado | 50/50 exitosos, promedio 71.88 ms, max 2151 ms. |
| 16 | Volumen medio | IMP-LD-250 Carga masiva x250 | `invoke-bulk-import-scenario.ps1` | Completado | Lote `BATCH-UI-20260325224811`: 250/250 validas y 250/250 procesadas. |
| 17 | Volumen alto prudente | IMP-LD-1000 Carga masiva x1000 | `invoke-bulk-import-scenario.ps1` | Completado | Variante limpia `1000/1000` procesadas y variante con errores `960` validas, `40` con error. |

## Registro de resultados

| Fecha | Caso | Resultado | Tiempo | Hallazgo | Severidad | Accion siguiente |
|---|---|---|---|---|---|---|
| 2026-03-25 | CFG-01 | Pasa | n/d | `GET /api/config/forms/resolved-active?entityName=Article&preferredFormKey=ArticleManualForm` devolvio formulario activo utilizable. | Baja | Mantener como linea base para cambios de configuracion. |
| 2026-03-25 | CFG-02 | Pasa | n/d | `GET /api/config/forms/resolved-active?entityName=ArticleParticipant&preferredFormKey=ArticleParticipantForm` devolvio formulario activo utilizable. | Baja | Mantener como linea base para cambios de configuracion. |
| 2026-03-25 | REG-01 | Pasa | n/d | Registro minimo por API persistio articulo y participante en la base extensible. | Baja | Usar este payload como caso base de regresion. |
| 2026-03-25 | IMP-01 | Pasa | n/d | `POST /api/import-batches/template` devolvio Excel correctamente. | Baja | Reutilizar para smoke y validacion manual de columnas. |
| 2026-03-25 | IMP-02 | Pasa con observaciones | upload 1758 ms, validate 77 ms | El lote se creo en staging, pero el CSV generado usa comas y el parser actual separa por `;`, por lo que toda la fila termina en `Title`. Tambien el script `invoke-bulk-import-scenario.ps1` falla en Windows PowerShell por usar `ConvertFrom-Json -Depth`. | Alta | Corregir primero el artefacto de prueba o el parser CSV, luego repetir la linea base y el smoke. |
| 2026-03-25 | REG-LD-10 | Pasa | total 519 ms, promedio 40.2 ms | El escenario smoke de registro manual repetido fue estable y sin fallos. | Baja | Mantener como prueba rapida antes de cambios en registro agregado. |
| 2026-03-25 | CAT-PROJ-01 | Pasa | n/d | `/api/catalogs/projects` ahora devuelve lista vacia cuando la tabla `Projects` no existe en esta base, sin romper el backend. | Media | Mantener este comportamiento mientras el modelo actual no incluya proyectos. |
| 2026-03-25 | IMP-02-R1 | Pasa | upload 2995 ms, validate 1169 ms, process n/d | En backend de pruebas sobre `http://localhost:5041`, el lote `BATCH-UI-20260325085434` subio y valido correctamente con 4/4 filas validas. | Baja | Usar este flujo como nueva linea base de carga CSV. |
| 2026-03-25 | IMP-04 | Pasa | n/d | El lote `BATCH-UI-20260325085434` se proceso correctamente con 4/4 filas procesadas y trazabilidad completa en staging. | Baja | Continuar con `Smoke` de 50 filas y luego `Errores controlados`. |
| 2026-03-25 | IMP-LD-50 | Pasa | upload 2156 ms, validate 1253 ms, process 1831 ms | El smoke de carga masiva ya procesa exactamente 50 filas; se corrigio la perdida silenciosa de la primera fila CSV. | Baja | Mantener este escenario como regresion de importacion CSV. |
| 2026-03-25 | IMP-ERR-100 | Pasa | upload 1348 ms, validate 1480 ms | La validacion del lote con errores intencionales devolvio 90 filas validas y 10 con error, alineado con el generador. | Baja | Siguiente paso: revisar cola de correccion y luego pasar a volumen medio. |
| 2026-03-25 | REG-LD-50 | Pasa | total 3880 ms, promedio 71.88 ms | El registro manual repetido x50 se mantuvo estable; solo la primera insercion fue sensiblemente mas lenta. | Baja | Monitorear si ese primer costo crece con carga alta. |
| 2026-03-25 | IMP-LD-250 | Pasa | upload 2878 ms, validate 3834 ms, process 6770 ms | La carga masiva x250 se proceso completa sin errores y sin señales tempranas de degradacion funcional. | Baja | Siguiente paso: volumen alto prudente x1000. |
| 2026-03-25 | IMP-LD-1000 | Pasa | upload 24410 ms, validate 47075 ms, process 39294 ms | La corrida limpia de 1000 filas completo upload, validacion y proceso sin errores funcionales. | Media | Vigilar experiencia de UI y conveniencia de procesamiento por bloques. |
| 2026-03-25 | IMP-ERR-1000 | Pasa | upload 37284 ms, validate 30349 ms | La corrida de 1000 filas con errores intencionales devolvio 960 validas y 40 con error, alineado con `ErrorEvery 25`. | Media | Siguiente paso: revisar correccion parcial y UX de la cola de errores a gran escala. |

## Clasificacion sugerida de hallazgos

### Bloqueo funcional

1. no permite completar el flujo
2. rompe persistencia o procesamiento

### Inconsistencia de datos

1. guarda datos parciales
2. crea relaciones incorrectas
3. deja staging y resultado final desalineados

### UX operativa

1. error poco claro
2. pantalla pesada
3. correccion manual incomoda

### Rendimiento

1. tiempos altos pero funcionales
2. degradacion visible con volumen medio o alto

## Regla de uso

Antes de cambiar comportamiento en registro, configuracion o importacion:

1. marcar el caso afectado
2. correr la linea base si no existe evidencia reciente
3. registrar resultado actual
4. aplicar cambio
5. repetir el caso y comparar
