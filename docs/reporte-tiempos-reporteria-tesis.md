# Reporte de tiempos de reportería para tesis

## 1. Objetivo

Este reporte documenta las mediciones registradas por el sistema para evaluar el desempeño del modulo de reportería. La finalidad es aportar evidencia cuantitativa para el objetivo de tesis relacionado con la reduccion de tiempos de reportería, eficiencia operativa y soporte a la toma de decisiones institucionales.

Los datos fueron obtenidos desde la tabla:

```text
dbo.ReportingPerformanceMetrics
```

Base consultada:

```text
TesisDB_Extensible
```

Periodo de medicion registrado:

```text
2026-05-22 05:28:11 UTC a 2026-05-24 23:36:59 UTC
```

## 2. Funcionamiento de la medicion

El sistema registra una metrica cada vez que se ejecuta una operacion relevante de reportería.

Campos principales:

| Campo | Descripcion |
| --- | --- |
| `Operation` | Tipo de operacion ejecutada. |
| `StartedAtUtc` | Fecha/hora de inicio. |
| `FinishedAtUtc` | Fecha/hora de fin. |
| `DurationMs` | Duracion real en milisegundos. |
| `Succeeded` | Indica si la operacion fue exitosa. |
| `ResultCount` | Cantidad de registros o elementos devueltos. |
| `PayloadBytes` | Peso aproximado del archivo generado, si aplica. |
| `ManualBaselineMs` | Tiempo manual estimado para realizar la misma tarea. |
| `EstimatedTimeSavedMs` | Tiempo estimado ahorrado por el sistema. |

## 3. Formulas utilizadas

### 3.1 Duracion real

```text
DurationMs = FinishedAtUtc - StartedAtUtc
```

En segundos:

```text
DurationSeconds = DurationMs / 1000
```

### 3.2 Tiempo manual estimado

El sistema asigna una linea base manual por operacion.

| Operacion | Baseline manual configurado |
| --- | ---: |
| `DashboardLoad` sin filtros | 20 minutos |
| `DashboardLoad` con filtros | 25 minutos |
| `AuthorDashboardLoad` sin filtros | 25 minutos |
| `AuthorDashboardLoad` con filtros | 30 minutos |
| `DashboardPdf` | 180 minutos |
| `DashboardPdfComposed` | 210 minutos |
| `AuthorPdf` | 180 minutos |
| `DashboardExcel` | 150 minutos |
| `RawDatasetExcel` | 240 minutos |
| `RawDatasetCsv` | 240 minutos |
| `ReportingFullEtl` | 120 minutos |
| Otros reportes sin filtros | 15 minutos |
| Otros reportes con filtros | 20 minutos |

### 3.3 Tiempo ahorrado

```text
EstimatedTimeSavedMs =
max(0, ManualBaselineMs - DurationMs)
```

### 3.4 Porcentaje de reduccion

```text
ReductionPercent =
(EstimatedTimeSavedMs / ManualBaselineMs) * 100
```

Para totales:

```text
TotalReductionPercent =
(sum(EstimatedTimeSavedMs) / sum(ManualBaselineMs)) * 100
```

### 3.5 Tasa de exito

```text
SuccessRate =
(SuccessfulOperations / TotalOperations) * 100
```

## 4. Resumen general obtenido

| Indicador | Valor |
| --- | ---: |
| Operaciones medidas | 80 |
| Operaciones exitosas | 80 |
| Operaciones fallidas | 0 |
| Tasa de exito | 100% |
| Duracion promedio real | 10.53 segundos |
| Tiempo real total acumulado | 842.18 segundos |
| Tiempo real total acumulado en minutos | 14.04 minutos |
| Tiempo manual estimado total | 4090.00 minutos |
| Tiempo manual estimado total en horas | 68.17 horas |
| Tiempo estimado ahorrado | 4075.96 minutos |
| Tiempo estimado ahorrado en horas | 67.93 horas |
| Reduccion estimada total | 99.66% |
| Registros/elementos consultados acumulados | 95629 |
| Bytes generados en exportes | 1655676 bytes |

## 5. Resumen por operacion

| Operacion | Ejecuciones | Exitosas | Fallidas | Promedio real | Minimo | Maximo | Baseline manual promedio | Ahorro promedio | Reduccion |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `DashboardLoad` | 32 | 32 | 0 | 16.37 s | 0.00 s | 96.40 s | 21.56 min | 21.29 min | 98.73% |
| `AuthorDashboardLoad` | 29 | 29 | 0 | 8.94 s | 0.00 s | 99.16 s | 26.21 min | 26.06 min | 99.43% |
| `ReportingFullEtl` | 12 | 12 | 0 | 2.63 s | 1.73 s | 3.43 s | 120.00 min | 119.96 min | 99.96% |
| `DashboardExcel` | 3 | 3 | 0 | 6.71 s | 5.94 s | 7.14 s | 150.00 min | 149.89 min | 99.93% |
| `DashboardPdf` | 3 | 3 | 0 | 2.12 s | 0.42 s | 5.38 s | 180.00 min | 179.96 min | 99.98% |
| `DashboardPdfComposed` | 1 | 1 | 0 | 0.83 s | 0.83 s | 0.83 s | 210.00 min | 209.99 min | 99.99% |

## 6. Uso diario registrado

| Fecha UTC | Operaciones | Exitosas | Duracion promedio | Ahorro estimado |
| --- | ---: | ---: | ---: | ---: |
| 2026-05-22 | 63 | 63 | 3.49 s | 2841.33 min |
| 2026-05-24 | 17 | 17 | 36.60 s | 1234.63 min |

## 7. Operaciones mas lentas observadas

| Operacion | Fecha UTC | Duracion real | Resultado | Baseline manual | Ahorro estimado |
| --- | --- | ---: | ---: | ---: | ---: |
| `AuthorDashboardLoad` | 2026-05-24 23:05:55 | 99.16 s | 1408 | 25.00 min | 23.35 min |
| `DashboardLoad` | 2026-05-24 16:01:10 | 96.40 s | 1972 | 20.00 min | 18.39 min |
| `DashboardLoad` | 2026-05-22 11:02:56 | 96.15 s | 1971 | 20.00 min | 18.40 min |
| `DashboardLoad` | 2026-05-24 23:04:17 | 96.08 s | 1972 | 20.00 min | 18.40 min |
| `DashboardLoad` | 2026-05-24 16:25:08 | 96.02 s | 1972 | 20.00 min | 18.40 min |
| `DashboardLoad` | 2026-05-24 23:33:42 | 95.92 s | 1972 | 20.00 min | 18.40 min |
| `AuthorDashboardLoad` | 2026-05-24 23:35:19 | 95.90 s | 1408 | 25.00 min | 23.40 min |
| `AuthorDashboardLoad` | 2026-05-22 11:04:45 | 43.10 s | 1407 | 25.00 min | 24.28 min |
| `DashboardLoad` | 2026-05-22 08:40:23 | 23.87 s | 1968 | 20.00 min | 19.60 min |
| `DashboardLoad` | 2026-05-22 12:05:34 | 9.83 s | 1972 | 20.00 min | 19.84 min |

## 8. Lectura tecnica de resultados

Los resultados muestran que el modulo de reportería automatiza tareas que manualmente requeririan consolidacion de datos, aplicacion de filtros, conteos, preparacion de graficos, exportacion de documentos y generacion de archivos institucionales.

El tiempo manual total estimado para las 80 operaciones medidas es:

```text
4090.00 minutos = 68.17 horas
```

El tiempo real acumulado del sistema fue:

```text
842.18 segundos = 14.04 minutos
```

Por tanto, el ahorro estimado fue:

```text
4075.96 minutos = 67.93 horas
```

La reduccion total estimada:

```text
99.66%
```

Esto significa que, bajo las lineas base configuradas, el sistema reduce significativamente el tiempo requerido para obtener reportería institucional.

## 9. Consideraciones metodologicas para tesis

Aunque los resultados son favorables, deben interpretarse correctamente:

1. `ManualBaselineMs` es una linea base estimada configurada por operacion, no una medicion cronometro de usuarios reales.
2. El resultado sirve como evidencia cuantitativa inicial del potencial de reduccion de tiempo.
3. Para una validacion institucional final, conviene complementar con mediciones reales de usuarios antes y despues del sistema.
4. Algunas operaciones tienen duraciones cercanas a cero, posiblemente por cache, datos ya cargados o respuesta rapida del backend.
5. Las operaciones mas lentas siguen estando por debajo del tiempo manual estimado, pero deben monitorearse si la base de datos crece.

## 10. Interpretacion por modulo

### Dashboard institucional

El dashboard general tuvo 32 ejecuciones con promedio de 16.37 segundos. Aunque existen casos cercanos a 96 segundos, el tiempo sigue siendo menor al baseline manual de 20 a 25 minutos.

Interpretacion:

```text
El usuario obtiene indicadores institucionales consolidados en segundos o pocos minutos,
frente a una tarea manual estimada en aproximadamente 20 minutos.
```

### Panel de autores y coautoria

El panel de autores tuvo 29 ejecuciones con promedio de 8.94 segundos. El resultado acumulado incluye consultas de alta cantidad de autores, publicaciones y coautorias.

Interpretacion:

```text
La analitica de autores permite consultar relaciones autorales y publicaciones
sin reconstruir manualmente matrices de coautoria.
```

### ETL de reportería

El ETL completo tuvo 12 ejecuciones con promedio de 2.63 segundos. La linea base manual configurada es de 120 minutos.

Interpretacion:

```text
La actualizacion del modelo analitico automatiza una tarea que manualmente
implicaria extraccion, transformacion, carga y verificacion de datos.
```

### Exportes PDF y Excel

Los exportes institucionales generaron archivos con tiempos promedio bajos:

```text
PDF: 2.12 segundos
Excel: 6.71 segundos
PDF compuesto: 0.83 segundos
```

Interpretacion:

```text
El sistema convierte informacion filtrada en documentos reutilizables,
reduciendo el trabajo manual de construir reportes en hojas de calculo o documentos.
```

## 11. Indicadores para usar en el documento de tesis

| Indicador | Valor para tesis |
| --- | ---: |
| Operaciones de reportería medidas | 80 |
| Tasa de exito | 100% |
| Tiempo promedio del sistema | 10.53 segundos |
| Tiempo manual estimado promedio | 51.13 minutos |
| Ahorro promedio estimado | 50.95 minutos |
| Reduccion promedio estimada total | 99.66% |
| Tiempo total del sistema | 14.04 minutos |
| Tiempo manual estimado total | 68.17 horas |
| Ahorro total estimado | 67.93 horas |
| Elementos consultados acumulados | 95629 |

## 12. Redaccion sugerida para la tesis

Para evaluar el desempeno del sistema en terminos de reduccion de tiempos de reportería, se implemento un mecanismo de medicion automatica en la tabla `ReportingPerformanceMetrics`. Este mecanismo registra la duracion real de operaciones como carga del dashboard institucional, analitica de autores, ejecucion del ETL y generacion de reportes PDF/Excel. Adicionalmente, cada operacion se compara contra una linea base manual estimada, definida segun la complejidad de la tarea.

Durante el periodo registrado entre el 22 y el 24 de mayo de 2026 se obtuvieron 80 mediciones de reportería, todas ejecutadas exitosamente. El tiempo real acumulado del sistema fue de 14.04 minutos, frente a una linea base manual estimada de 68.17 horas. Esto representa un ahorro estimado de 67.93 horas y una reduccion aproximada del 99.66% en el tiempo requerido para generar informacion de reportería.

Estos resultados evidencian que la automatizacion de reportería permite transformar tareas manuales de consolidacion, filtrado, conteo, generacion de graficos y exportacion documental en procesos ejecutados desde el sistema en segundos o pocos minutos. No obstante, al tratarse de una linea base manual estimada, se recomienda complementar esta evidencia con pruebas de usuario en un entorno institucional real.

## 13. Recomendacion para validacion final

Para fortalecer la tesis, se recomienda ejecutar una evaluacion adicional con usuarios reales:

1. Pedir a un usuario generar manualmente un reporte equivalente.
2. Medir el tiempo real manual con cronometro.
3. Ejecutar el mismo reporte desde el sistema.
4. Comparar ambos tiempos.
5. Registrar percepcion de utilidad mediante encuesta.

Formula final:

```text
ReduccionReal =
((TiempoManualReal - TiempoSistema) / TiempoManualReal) * 100
```

Con esto se podra complementar la medicion automatica actual con evidencia empirica institucional.
