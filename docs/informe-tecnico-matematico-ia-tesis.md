# Informe tecnico-matematico del modulo de inteligencia artificial

## 1. Objetivo del informe

Este informe documenta la base matematica y tecnica del modulo de inteligencia artificial aplicado al sistema de gestion de produccion cientifica. Su finalidad es servir como soporte metodologico para la tesis, explicando los indicadores, porcentajes, formulas, metricas de evaluacion, predicciones y reglas de recomendacion utilizadas.

El modulo IA combina:

- prediccion de series temporales
- algoritmos baseline
- ML.NET ForecastBySsa
- regresion lineal temporal
- promedio movil
- metricas de evaluacion predictiva
- reglas explicables de recomendacion
- diagnostico de preparacion de datos

## 2. Preparacion general para IA

El sistema calcula un indicador global llamado `OverallScore`, que representa el nivel de preparacion del sistema para ejecutar predicciones y recomendaciones confiables.

Formula:

```text
OverallScore =
(DatasetVolumeScore * 0.30)
+ (AuthorTraceScore * 0.22)
+ (IndexingCoverageScore * 0.20)
+ (max(LoadQualityScore, 55) * 0.14)
+ (max(WorkflowSignalScore, 45) * 0.14)
```

Pesos utilizados:

| Indicador | Peso |
| --- | ---: |
| Volumen de datos | 30% |
| Trazabilidad de autores | 22% |
| Cobertura de indexacion | 20% |
| Calidad de carga | 14% |
| Senales de workflow | 14% |
| Total | 100% |

Interpretacion:

| OverallScore | Nivel |
| ---: | --- |
| 80% a 100% | Preparacion alta |
| 60% a 79% | Preparacion media |
| 40% a 59% | Preparacion inicial |
| Menor a 40% | Preparacion baja |

## 3. Puntaje por volumen de datos

El volumen de datos se calcula en funcion del numero total de articulos disponibles.

Regla:

```text
Si totalArticulos >= 500 -> 100%
Si totalArticulos >= 250 -> 85%
Si totalArticulos >= 100 -> 70%
Si totalArticulos >= 50  -> 55%
Si totalArticulos > 0    -> 35%
Si totalArticulos = 0    -> 0%
```

Importancia:

Este puntaje indica si la cantidad de registros es suficiente para entrenar modelos con mejor capacidad predictiva. A mayor volumen, mayor estabilidad estadistica.

## 4. Trazabilidad de autores

La trazabilidad de autores mide que porcentaje de articulos tiene informacion asociada a sus autores.

Formula:

```text
AuthorTraceScore =
(ArticulosConTrazabilidadDeAutor / TotalArticulos) * 100
```

Ejemplo:

```text
Articulos con autor = 151
Total de articulos = 151

AuthorTraceScore = (151 / 151) * 100 = 100%
```

Importancia:

Una alta trazabilidad permite generar recomendaciones por productividad, coautoria, redes colaborativas, filiacion y ORCID.

## 5. Calidad de carga

La calidad de carga mide la proporcion de filas procesadas correctamente durante procesos de importacion, carga masiva o staging.

Formula:

```text
LoadQualityScore =
(FilasExitosas / TotalFilasProcesadas) * 100
```

Ejemplo:

```text
Filas exitosas = 950
Total filas procesadas = 1000

LoadQualityScore = (950 / 1000) * 100 = 95%
```

Importancia:

Este indicador permite estimar si la informacion que alimenta la reportería y la IA tiene suficiente calidad operativa.

## 6. Cobertura de indexacion

La cobertura de indexacion mide el porcentaje de articulos que tienen informacion analitica relacionada con indexacion, fuentes o bases.

Formula:

```text
IndexingCoverageScore =
(ArticulosConInformacionDeIndexacion / TotalArticulos) * 100
```

Importancia:

Este indicador es clave para recomendaciones editoriales, analisis de cuartiles, bases de indexacion y visibilidad cientifica.

## 7. Senales de workflow

El sistema calcula una senal operativa del workflow considerando ejecuciones de etapas y devoluciones.

Formula:

```text
WorkflowSignalScore =
min(100, TotalStageExecutions) - min(30, ReturnedStages)
```

Luego el valor se limita entre:

```text
0% y 100%
```

Interpretacion:

- mas ejecuciones de etapas generan mayor evidencia operativa
- mas devoluciones reducen la calidad de la senal
- el sistema usa esta informacion para preparar modelos futuros de riesgo de demora

## 8. Prediccion temporal de produccion cientifica

El modulo predice la produccion cientifica mensual esperada.

Variable objetivo:

```text
Total de articulos por mes
```

Horizonte actual:

```text
6 meses futuros
```

Cada punto de prediccion contiene:

```text
Periodo
Valor esperado
Limite inferior
Limite superior
Indicador de si es pronostico
```

Representacion:

```text
Forecast(t + h) = articulos esperados para el mes futuro h
```

Donde:

```text
h = 1, 2, 3, 4, 5, 6
```

## 9. Modelo baseline: persistencia del ultimo valor

Este modelo usa el ultimo valor observado como prediccion futura.

Formula:

```text
Prediccion(t + 1) = Valor(t)
```

Ejemplo:

```text
Ultimo mes observado = 44 articulos

Prediccion siguiente mes = 44 articulos
```

Uso:

Sirve como linea base minima para comparar modelos mas avanzados.

## 10. Modelo baseline: promedio movil

El sistema usa promedio movil, principalmente de 3 meses, para suavizar fluctuaciones recientes.

Formula:

```text
Prediccion(t + 1) =
(Valor(t) + Valor(t - 1) + Valor(t - 2)) / 3
```

Ejemplo:

```text
Mes 1 = 20 articulos
Mes 2 = 30 articulos
Mes 3 = 40 articulos

Prediccion siguiente =
(20 + 30 + 40) / 3 = 30 articulos
```

Uso:

- reduce ruido mensual
- es interpretable para usuarios no tecnicos
- funciona bien cuando hay pocos datos historicos

## 11. Regresion lineal temporal

La regresion lineal temporal estima una tendencia de crecimiento o descenso.

Formula de la pendiente:

```text
slope =
(n * sum(xy) - sum(x) * sum(y)) /
(n * sum(x^2) - (sum(x))^2)
```

Donde:

```text
x = indice del mes
y = total de articulos del mes
n = numero de meses
```

Intercepto:

```text
intercepto = promedio(y) - slope * promedio(x)
```

Prediccion:

```text
Prediccion = intercepto + slope * mesFuturo
```

Interpretacion de tendencia:

```text
Si slope > 0.25  -> tendencia creciente
Si slope < -0.25 -> tendencia descendente
Caso contrario   -> tendencia estable
```

Uso:

Permite explicar si la produccion cientifica tiende a crecer, reducirse o mantenerse.

## 12. Modelo ML.NET ForecastBySsa

El modelo principal de series temporales usa:

```text
Microsoft.ML
Microsoft.ML.TimeSeries
ForecastBySsa
```

Tipo:

```text
Forecasting basado en Singular Spectrum Analysis
```

Parametros principales:

```text
horizon = 6
confidenceLevel = 0.95
seed = 7
```

Parametros dinamicos:

```text
windowSize = clamp(totalMeses / 3, 2, 6)
seriesLength = clamp(windowSize * 2, windowSize + 1, totalMeses)
trainSize = totalMeses
```

Nivel de confianza:

```text
95%
```

Salida del modelo:

```text
ForecastedValues
LowerBoundValues
UpperBoundValues
```

Interpretacion:

El sistema entrega un valor esperado y un rango de confianza. El rango permite comunicar incertidumbre al usuario.

## 13. Calculo de confianza de la prediccion

Primero se asigna un puntaje base segun cantidad de meses historicos.

Regla:

```text
Si meses >= 24 -> 85%
Si meses >= 18 -> 72%
Si meses >= 12 -> 60%
Si meses >= 6  -> 45%
Si meses < 6   -> 25%
```

Luego se penaliza por volatilidad:

```text
VolatilityPenalty =
(Volatilidad / max(1, Promedio)) * 45
```

Confianza final:

```text
ConfidenceScore =
VolumeScore - VolatilityPenalty
```

Limite:

```text
15% <= ConfidenceScore <= 95%
```

Interpretacion:

- una serie larga aumenta confianza
- una serie muy volatil reduce confianza
- el sistema evita mostrar confianza absoluta

## 14. Volatilidad

La volatilidad mide la dispersion de la produccion mensual respecto al promedio.

Promedio:

```text
promedio = sum(x) / n
```

Varianza:

```text
varianza =
sum((x - promedio)^2) / n
```

Volatilidad:

```text
volatilidad = sqrt(varianza)
```

Uso:

La volatilidad se usa para:

- calcular confianza
- definir margenes de prediccion
- detectar si el pronostico debe tratarse como alerta temprana

## 15. Rangos de prediccion en modelo base

Cuando se usa el modelo base con promedio y tendencia, el sistema calcula un margen.

Formula:

```text
margen = max(1, volatilidad * 0.85)
```

Limite inferior:

```text
LowerBound = max(0, prediccion - margen)
```

Limite superior:

```text
UpperBound = prediccion + margen
```

Esto evita valores negativos y comunica incertidumbre.

## 16. Separacion entrenamiento-validacion

El sistema usa validacion temporal, manteniendo el orden cronologico.

Regla:

| Meses disponibles | Meses de validacion |
| ---: | ---: |
| 18 o mas | 6 |
| 12 a 17 | 4 |
| 8 a 11 | 3 |
| Menos de 8 | 0 |

Calculo:

```text
trainingRows = totalMeses - validationRows
```

Ejemplo:

```text
totalMeses = 29
validationRows = 6
trainingRows = 29 - 6 = 23
```

Justificacion:

En series temporales no se recomienda mezclar aleatoriamente los datos porque se pierde el orden historico.

## 17. Metricas de evaluacion

El sistema evalua los modelos con:

- MAE
- RMSE
- MAPE
- Score complementario

## 18. MAE: error absoluto medio

Formula:

```text
MAE =
sum(|Real - Predicho|) / n
```

Ejemplo:

```text
Real:      10, 20, 30
Predicho: 12, 18, 33

Errores:
|10 - 12| = 2
|20 - 18| = 2
|30 - 33| = 3

MAE = (2 + 2 + 3) / 3 = 2.33
```

Interpretacion:

El MAE indica el error promedio en unidades reales, es decir, articulos.

Uso en el sistema:

```text
Metrica principal para seleccionar el mejor modelo.
```

## 19. RMSE: raiz del error cuadratico medio

Formula:

```text
RMSE =
sqrt(sum((Real - Predicho)^2) / n)
```

Interpretacion:

Penaliza errores grandes mas que el MAE.

Uso:

Se utiliza como metrica complementaria y criterio de desempate.

## 20. MAPE: error porcentual absoluto medio

Formula:

```text
MAPE =
(sum(|(Real - Predicho) / Real|) * 100) / n
```

Consideracion:

Cuando `Real` es igual o cercano a cero, el MAPE puede distorsionarse. Por eso el sistema usa MAE como metrica principal.

Uso:

Se utiliza como senal complementaria para interpretar error relativo.

## 21. Score complementario

Formula:

```text
Score = max(0, 100 - MAPE)
```

Ejemplo:

```text
MAPE = 20%
Score = 80%
```

Uso:

Permite mostrar un indicador simple de desempeno relativo, aunque no reemplaza al MAE.

## 22. Seleccion del mejor modelo

El sistema selecciona el modelo ganador ordenando por:

```text
1. Menor MAE
2. Menor RMSE
```

Formalmente:

```text
BestModel = argmin(MAE, RMSE)
```

Luego el modelo se marca como:

```text
IsBest = true
```

Y se registra:

```text
BestAlgorithm
BestMetric = MAE
BestMetricValue
```

## 23. Promocion del modelo

Si existe al menos un modelo evaluado, se promueve el mejor.

Estados:

```text
Completed
InsufficientData
```

Si hay modelo evaluado:

```text
Status = Completed
Promoted = true
ActiveModelVersion = version generada
```

Si no hay suficientes datos:

```text
Status = InsufficientData
Promoted = false
ActiveModelVersion = sin-version
PromotedAlgorithm = Sin modelo promovido
```

## 24. Versionado del modelo

El sistema genera versiones segun el algoritmo promovido.

Reglas:

```text
Si algoritmo contiene ML.NET:
    mlnet-ssa-yyyyMMddHHmmss

Si algoritmo contiene Regresion:
    baseline-regression-yyyyMMddHHmmss

Si algoritmo contiene Promedio:
    baseline-moving-average-yyyyMMddHHmmss

Caso contrario:
    baseline-persistence-yyyyMMddHHmmss
```

Ejemplo:

```text
baseline-moving-average-20260514023535
mlnet-ssa-20260514023535
```

## 25. Reentrenamiento

Politica:

```text
Reentrenar despues de cada ETL completo
o cuando ingresen nuevos meses con produccion confirmada.
```

Flujo:

```text
1. Obtener datos actualizados desde reportería
2. Construir serie mensual
3. Separar entrenamiento y validacion
4. Evaluar algoritmos candidatos
5. Calcular MAE, RMSE, MAPE y Score
6. Seleccionar menor MAE
7. Promover modelo si aplica
8. Guardar historial de entrenamiento
9. Invalidar cache del dashboard IA
```

## 26. Porcentaje de participacion

El sistema usa porcentajes de participacion para recomendaciones de facultades, lineas, indexacion, cuartiles y Open Access.

Formula:

```text
SharePercent =
(ValorSegmento / TotalArticulos) * 100
```

Ejemplo:

```text
Articulos Open Access = 97
Total articulos = 187

SharePercent = (97 / 187) * 100 = 51.87%
```

## 27. Reglas de prioridad en recomendaciones

El sistema asigna prioridad usando reglas explicables.

Reglas principales:

```text
Si TotalArticles < 250:
    prioridad = Alta
    recomendacion = ampliar base historica

Si ForecastConfidence < 40:
    prioridad = Alta
    recomendacion = revisar volatilidad antes de decidir

Si AuthorTraceScore < 90:
    prioridad = Alta
    recomendacion = fortalecer trazabilidad autoral

Si IndexingCoverageScore < 80:
    prioridad = Media
    recomendacion = normalizar fuentes y cuartiles

Si OpenAccessShare < 60:
    prioridad = Alta
    recomendacion = mejorar estrategia Open Access
```

## 28. Reglas de tendencia por segmento

Para facultades y lineas de investigacion se calcula tendencia por pendiente.

Regla:

```text
Si slope > 0.25:
    Tendencia = Creciente

Si slope < -0.25:
    Tendencia = Descendente

Caso contrario:
    Tendencia = Estable
```

Recomendaciones asociadas:

```text
Si tendencia = Creciente:
    sostener seguimiento y capacidad editorial

Si tendencia = Descendente:
    priorizar acompanamiento institucional

Si tendencia = Estable:
    usar como linea base de seguimiento
```

## 29. Preparacion temporal del dataset

El sistema evalua la serie mensual disponible.

Indicadores:

```text
MonthsAvailable
NonEmptyMonths
EmptyMonths
MinimumMonthsRequired = 8
RecommendedMinimumMonths = 12
MissingMonthsToTrain
FirstMonth
LastMonth
PeakMonth
PeakMonthArticles
ConcentrationPercent
```

Concentracion del mes pico:

```text
ConcentrationPercent =
(PeakMonthArticles / TotalArticlesSerie) * 100
```

Uso:

Detectar si la produccion esta demasiado concentrada en un solo mes, lo cual reduce estabilidad predictiva.

## 30. Valores clave actuales del modulo

| Elemento | Valor |
| --- | --- |
| Horizonte de prediccion | 6 meses |
| Nivel de confianza SSA | 95% |
| Meses minimos para validar | 8 |
| Meses recomendados | 12 |
| Semilla ML.NET | 7 |
| Metrica principal | MAE |
| Metrica de desempate | RMSE |
| Penalizacion por volatilidad | hasta 45 puntos |
| Confianza minima | 15% |
| Confianza maxima | 95% |
| Umbral Open Access | 60% |
| Umbral trazabilidad autores | 90% |
| Umbral cobertura indexacion | 80% |
| Umbral baja confianza forecast | 40% |
| Umbral volumen bajo para recomendaciones | 250 articulos |

## 31. Resumen tecnico para tesis

El modulo IA del sistema combina modelos predictivos y reglas explicables. La prediccion principal se basa en una serie temporal mensual de articulos cientificos, sobre la cual se evaluan modelos baseline, regresion lineal y ML.NET ForecastBySsa. La seleccion del mejor modelo se realiza con validacion temporal, usando MAE como metrica principal y RMSE como desempate.

Adicionalmente, el sistema calcula un porcentaje de preparacion IA ponderado con cinco dimensiones: volumen de datos, trazabilidad de autores, cobertura de indexacion, calidad de carga y senales del workflow. Este indicador permite determinar si la base de datos es suficientemente confiable para generar predicciones y recomendaciones.

Las recomendaciones se generan mediante reglas explicables que analizan confianza predictiva, volatilidad, calidad de datos, trazabilidad autoral, cobertura editorial, acceso abierto y senales operativas. Este enfoque permite que el usuario entienda por que el sistema recomienda una accion y que evidencia respalda la decision.

## 32. Formulas principales consolidadas

```text
OverallScore =
(DatasetVolumeScore * 0.30)
+ (AuthorTraceScore * 0.22)
+ (IndexingCoverageScore * 0.20)
+ (max(LoadQualityScore, 55) * 0.14)
+ (max(WorkflowSignalScore, 45) * 0.14)
```

```text
AuthorTraceScore =
(ArticulosConTrazabilidadDeAutor / TotalArticulos) * 100
```

```text
LoadQualityScore =
(FilasExitosas / TotalFilasProcesadas) * 100
```

```text
IndexingCoverageScore =
(ArticulosConIndexacion / TotalArticulos) * 100
```

```text
WorkflowSignalScore =
min(100, TotalStageExecutions) - min(30, ReturnedStages)
```

```text
PromedioMovil3 =
(Valor(t) + Valor(t - 1) + Valor(t - 2)) / 3
```

```text
slope =
(n * sum(xy) - sum(x) * sum(y)) /
(n * sum(x^2) - (sum(x))^2)
```

```text
MAE =
sum(|Real - Predicho|) / n
```

```text
RMSE =
sqrt(sum((Real - Predicho)^2) / n)
```

```text
MAPE =
(sum(|(Real - Predicho) / Real|) * 100) / n
```

```text
Score =
max(0, 100 - MAPE)
```

```text
SharePercent =
(ValorSegmento / TotalArticulos) * 100
```

```text
ConfidenceScore =
VolumeScore - ((Volatilidad / max(1, Promedio)) * 45)
```
