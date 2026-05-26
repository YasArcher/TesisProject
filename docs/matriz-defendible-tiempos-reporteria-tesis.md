# Matriz defendible de tiempos de reportería

## 1. Proposito

Este documento presenta una matriz de tiempos de reportería basada en datos reales registrados por el sistema. El objetivo es usar evidencia cuantitativa para la tesis sin asumir conclusiones excesivas o riesgosas en defensa.

La lectura recomendada es:

```text
El sistema reduce significativamente el esfuerzo manual de reportería y presenta tiempos promedio adecuados; sin embargo, existen picos de carga que deben considerarse como oportunidades de optimizacion en una fase de produccion.
```

## 2. Fuente de datos

Tabla consultada:

```text
dbo.ReportingPerformanceMetrics
```

Base de datos:

```text
TesisDB_Extensible
```

Periodo medido:

```text
2026-05-22 05:28:11 UTC a 2026-05-24 23:36:59 UTC
```

Cantidad de mediciones:

```text
80 operaciones de reportería
```

Tasa de exito:

```text
80 exitosas / 80 totales = 100%
```

## 3. Matriz general de resultados

| Indicador | Valor observado | Lectura para tesis |
| --- | ---: | --- |
| Operaciones medidas | 80 | Muestra inicial suficiente para describir comportamiento operativo preliminar. |
| Operaciones exitosas | 80 | No se observaron fallos en las operaciones registradas. |
| Operaciones fallidas | 0 | La reportería se mantuvo estable durante la muestra. |
| Tasa de exito | 100% | Indicador favorable de estabilidad funcional. |
| Tiempo promedio real | 10.53 segundos | Tiempo promedio adecuado para consultas institucionales. |
| Tiempo real acumulado | 14.04 minutos | Las 80 operaciones se resolvieron en menos de 15 minutos acumulados. |
| Tiempo manual estimado acumulado | 68.17 horas | Linea base estimada para tareas manuales equivalentes. |
| Ahorro estimado acumulado | 67.93 horas | Evidencia del potencial de reduccion de tiempos. |
| Reduccion estimada | 99.66% | Resultado favorable, debe presentarse como estimacion basada en baseline configurado. |
| Elementos consultados acumulados | 95629 | La muestra incluye operaciones con volumen relevante de datos. |

## 4. Matriz por tipo de reporte

| Operacion | Ejecuciones | Promedio | Mediana | P90 | Maximo | Lectura defendible |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Dashboard institucional | 32 | 16.37 s | 0.44 s | 96.01 s | 96.40 s | La mayoria de consultas fueron rapidas, pero existen picos altos cercanos a 1.6 minutos. |
| Panel autores/coautoria | 29 | 8.94 s | 0.38 s | 15.17 s | 99.16 s | Comportamiento promedio favorable; un valor maximo aislado evidencia necesidad de monitoreo. |
| ETL reportería | 12 | 2.63 s | 2.71 s | 3.41 s | 3.43 s | Tiempo estable y bajo en la muestra medida. |
| Exportacion Excel | 3 | 6.71 s | 7.07 s | 7.12 s | 7.14 s | Generacion de Excel consistente y menor a 10 segundos. |
| Exportacion PDF | 3 | 2.12 s | 0.55 s | 4.41 s | 5.38 s | Generacion de PDF rapida en la muestra. |
| PDF compuesto | 1 | 0.83 s | 0.82 s | 0.82 s | 0.83 s | Evidencia puntual; requiere mas muestras para generalizar. |

## 5. Clasificacion de tiempos

Para interpretar los tiempos sin exagerar, se propone la siguiente escala:

| Rango | Clasificacion | Interpretacion |
| ---: | --- | --- |
| 0 a 5 segundos | Rapido | Respuesta muy favorable para uso operativo. |
| 5 a 15 segundos | Adecuado | Aceptable para reportería con datos consolidados. |
| 15 a 60 segundos | Moderado | Puede ser aceptable si el reporte es pesado o tiene filtros amplios. |
| 60 a 120 segundos | Alto | Debe monitorearse y optimizarse antes de produccion intensiva. |
| Mayor a 120 segundos | Critico | Requiere optimizacion prioritaria. |

Aplicacion a la muestra:

| Operacion | Clasificacion promedio | Clasificacion peor caso |
| --- | --- | --- |
| Dashboard institucional | Moderado bajo | Alto |
| Panel autores/coautoria | Adecuado | Alto |
| ETL reportería | Rapido | Rapido |
| Exportacion Excel | Adecuado | Adecuado |
| Exportacion PDF | Rapido | Adecuado |
| PDF compuesto | Rapido | Rapido |

## 6. Matriz de valor para tesis

| Pregunta de defensa | Respuesta sustentada |
| --- | --- |
| El sistema tarda demasiado en generar reportes? | No en promedio. El promedio general fue de 10.53 segundos. |
| Existen casos lentos? | Si. Se observaron picos cercanos a 96-99 segundos en dashboard y autores. |
| Eso invalida el sistema? | No. Los picos siguen siendo menores al tiempo manual estimado, pero son oportunidades de optimizacion. |
| La reduccion de tiempo es real o estimada? | Es estimada frente a una linea base manual configurada por operacion. |
| Como se evita exagerar el resultado? | Presentando promedio, mediana, P90 y maximo, no solo el porcentaje de reduccion. |
| Que modulo se comporta mejor? | ETL, PDF y Excel muestran tiempos bajos y consistentes. |
| Que modulo requiere monitoreo? | Dashboard institucional y panel de autores, por picos altos. |
| Que evidencia cuantitativa existe? | 80 mediciones registradas, 100% exitosas, 95629 elementos consultados acumulados. |

## 7. Interpretacion para tesis

Los datos muestran que el sistema automatiza actividades que manualmente requeririan consolidar informacion, aplicar filtros, calcular indicadores, revisar autores/coautorias y generar archivos PDF o Excel. En la muestra obtenida, el promedio general de respuesta fue de 10.53 segundos, con una tasa de exito del 100%.

No obstante, la defensa debe reconocer que existen picos de carga en el dashboard institucional y el panel de autores. Estos picos alcanzan aproximadamente 96 a 99 segundos. Por ello, no conviene afirmar que todos los reportes son instantaneos. La afirmacion defendible es que:

```text
El sistema reduce significativamente el tiempo de elaboracion de reportes frente a un proceso manual estimado, aunque existen escenarios de carga pesada que deben monitorearse y optimizarse en una fase de produccion.
```

## 8. Indicadores recomendados para presentar

Los indicadores mas seguros para tesis son:

| Indicador | Valor |
| --- | ---: |
| Operaciones medidas | 80 |
| Tasa de exito | 100% |
| Tiempo promedio general | 10.53 s |
| Tiempo acumulado del sistema | 14.04 min |
| Tiempo manual estimado | 68.17 h |
| Ahorro estimado | 67.93 h |
| Reduccion estimada | 99.66% |
| Elementos consultados | 95629 |
| Picos maximos observados | 96-99 s |

## 9. Indicadores que conviene matizar

| Indicador | Riesgo si se presenta mal | Forma segura de presentarlo |
| --- | --- | --- |
| Reduccion de 99.66% | Puede parecer demasiado alta si no se explica la linea base. | Decir que es una reduccion estimada frente a tiempos manuales configurados. |
| Mediana menor a 1 segundo en dashboards | Puede sugerir que todo carga instantaneamente. | Presentarla junto al P90 y maximo. |
| 100% de exito | La muestra no garantiza ausencia futura de errores. | Decir: no se observaron fallos en las 80 mediciones registradas. |
| ETL de 2.63 segundos | Puede variar con volumen real de produccion. | Decir: en la muestra medida, el ETL tuvo bajo tiempo promedio. |

## 10. Redaccion recomendada

Para la tesis se recomienda usar una redaccion equilibrada:

```text
Durante la evaluacion del modulo de reportería se registraron 80 operaciones en la tabla ReportingPerformanceMetrics, correspondientes a carga del dashboard institucional, analitica de autores, ejecucion del ETL y generacion de reportes PDF/Excel. Todas las operaciones registradas finalizaron exitosamente, obteniendo una tasa de exito del 100% en la muestra observada.

El tiempo promedio general de respuesta fue de 10.53 segundos, mientras que el tiempo acumulado para las 80 operaciones fue de 14.04 minutos. Al compararlo con la linea base manual estimada configurada en el sistema, equivalente a 68.17 horas, se obtuvo un ahorro estimado de 67.93 horas, lo que representa una reduccion aproximada del 99.66%.

Sin embargo, tambien se identificaron picos de carga en el dashboard institucional y el panel de autores, con valores maximos cercanos a 96-99 segundos. Estos resultados no invalidan el sistema, pero evidencian oportunidades de optimizacion para escenarios de mayor carga en produccion. Por tanto, se concluye que el sistema presenta una reduccion significativa del tiempo de reportería frente al proceso manual estimado, manteniendo como mejora futura la optimizacion de consultas pesadas.
```

## 11. Matriz de conclusion

| Dimension evaluada | Resultado | Conclusion |
| --- | --- | --- |
| Estabilidad | 80/80 operaciones exitosas | Favorable |
| Velocidad promedio | 10.53 s | Favorable |
| Exportes | PDF y Excel menores a 10 s promedio | Favorable |
| ETL | 2.63 s promedio | Favorable en muestra |
| Dashboard general | Promedio aceptable, picos altos | Requiere monitoreo |
| Autores/coautoria | Promedio aceptable, un pico alto | Requiere monitoreo |
| Reduccion de tiempo | 99.66% estimado | Muy favorable, con aclaracion metodologica |

## 12. Recomendacion para defensa

Si preguntan si los tiempos son altos, la respuesta recomendada es:

```text
No en promedio. El sistema presento un tiempo promedio general de 10.53 segundos y una tasa de exito del 100% en las 80 operaciones registradas. Sin embargo, si se observaron picos cercanos a 96-99 segundos en consultas pesadas del dashboard y autores. Por eso, el resultado se presenta como favorable, pero con una mejora futura orientada a optimizar cargas maximas.
```

Si preguntan si el 99.66% es absoluto:

```text
No. Es una reduccion estimada frente a una linea base manual configurada por operacion. Para fortalecer la validacion, se recomienda complementar con pruebas cronometradas de usuarios reales.
```

Si preguntan por el valor academico:

```text
La medicion demuestra que el sistema incorpora mecanismos cuantitativos para evaluar desempeno, permitiendo comparar tiempo real del sistema contra una linea base manual y detectar oportunidades de optimizacion.
```
