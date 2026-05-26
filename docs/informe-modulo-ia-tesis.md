# Informe del modulo de inteligencia artificial aplicado al sistema

## 1. Proposito del modulo IA

El modulo de inteligencia artificial del sistema tiene como finalidad apoyar la toma de decisiones institucionales sobre la produccion cientifica registrada. Su enfoque no es reemplazar el criterio humano, sino convertir los datos historicos de articulos, autores, indexacion, facultades, lineas de investigacion y workflow en senales utiles para planificacion, seguimiento y mejora continua.

El modulo se encuentra implementado principalmente en:

```text
tesisproject.backend/Services/Modules/Intelligence/InstitutionalIntelligenceService.cs
tesisproject.backend/Controllers/Modules/Intelligence/IntelligenceController.cs
tesisproject.shared/DTOs/Intelligence/InstitutionalIntelligenceDto.cs
tesisproject.frontend/Features/Management/Pages/IntelligenceInsights.razor
```

## 2. Que predice el sistema

Actualmente el sistema realiza predicciones relacionadas con la produccion cientifica institucional.

### 2.1 Prediccion de produccion cientifica mensual

Variable objetivo:

```text
Total de articulos por mes
```

Fuente de datos:

```text
Dashboard de reportería institucional / Data Warehouse
```

Horizonte de prediccion:

```text
6 meses futuros
```

Salida generada:

- mes proyectado
- valor esperado de articulos
- limite inferior de confianza
- limite superior de confianza
- algoritmo usado
- porcentaje de confianza
- interpretacion del resultado

Uso institucional:

- anticipar si la produccion cientifica esperada sube, baja o se mantiene
- comparar mensualmente la produccion real contra la prediccion
- detectar caidas tempranas en el ritmo de registro o publicacion
- apoyar decisiones de seguimiento por parte de la DIDE

### 2.2 Prediccion por facultad

El modulo tambien construye predicciones segmentadas por facultad.

Para cada facultad se calcula:

- articulos historicos
- meses disponibles
- total esperado para los proximos 6 meses
- tendencia: creciente, descendente o estable
- confianza estimada
- recomendacion asociada

Uso institucional:

- identificar facultades con mayor o menor ritmo de produccion
- detectar unidades academicas que requieren acompanamiento
- priorizar seguimiento cuando la tendencia sea descendente
- usar la prediccion como linea base de comparacion mensual

### 2.3 Prediccion por linea de investigacion

El modulo genera predicciones por linea de investigacion, usando la misma logica temporal aplicada a segmentos.

Para cada linea se calcula:

- produccion historica
- produccion esperada a 6 meses
- tendencia
- confianza
- senal interpretativa
- recomendacion

Uso institucional:

- detectar lineas activas, estables o en descenso
- contrastar la produccion real contra areas estrategicas
- identificar saturacion o brechas tematicas
- apoyar planificacion academica e investigativa

## 3. Que recomienda el sistema

El modulo de IA genera recomendaciones explicables basadas en reglas, indicadores y resultados predictivos. Estas recomendaciones son importantes para tesis porque muestran que el sistema no solo predice, sino que traduce los resultados en acciones comprensibles para el usuario.

### 3.1 Recomendaciones de entrenamiento y calidad del dataset

El sistema recomienda ampliar o mejorar la base historica cuando:

- existen pocos articulos disponibles
- hay pocos meses historicos para entrenar
- faltan datos de autores
- falta trazabilidad de filiacion u ORCID
- falta indexacion o cuartil editorial

Ejemplos de recomendacion:

- ampliar la base historica para mejorar modelos segmentados
- completar autores, filiacion, ORCID e identificacion
- normalizar fuentes de indexacion y cuartiles
- esperar mas meses antes de usar predicciones como referencia fuerte

### 3.2 Recomendaciones de prediccion

Cuando el sistema detecta baja confianza o alta volatilidad mensual, recomienda:

- usar el pronostico como alerta temprana
- no tomarlo como meta cerrada
- revisar meses pico o cambios bruscos
- comparar la produccion real contra el rango esperado

Esto evita que el usuario interprete una prediccion incierta como una verdad absoluta.

### 3.3 Recomendaciones editoriales

El modulo genera recomendaciones basadas en:

- bases de indexacion con mayor presencia
- cuartiles predominantes
- articulos sin cuartil
- porcentaje de Open Access
- distribucion editorial por revista/fuente

Ejemplos de senales:

- base de indexacion prioritaria
- cuartil editorial relevante
- estrategia Open Access
- necesidad de normalizar metricas editoriales

Uso institucional:

- orientar rutas editoriales
- mejorar visibilidad cientifica
- priorizar normalizacion de revistas y cuartiles
- apoyar decisiones sobre donde publicar o que mejorar en la carga de datos

### 3.4 Recomendaciones de autores y colaboracion

El sistema analiza informacion de autores y coautoria para generar recomendaciones sobre:

- autores con alta produccion
- autores principales vs coautores
- redes de colaboracion
- autores sin ORCID
- autores sin filiacion

Ejemplos de recomendacion:

- usar autores con alta produccion como nodos de colaboracion
- completar ORCID para mejorar trazabilidad
- normalizar filiacion para analizar redes por unidad academica
- revisar coautorias para ampliar impacto institucional

### 3.5 Recomendaciones de workflow

Cuando existen devoluciones o etapas repetidas en el flujo, el modulo recomienda preparar un futuro modelo de riesgo de demora.

Senales usadas:

- etapas devueltas
- duracion de revision
- estado final
- numero de devoluciones

Uso futuro:

- predecir lotes con riesgo de retraso
- priorizar revision de casos complejos
- mejorar tiempos operativos

## 4. Algoritmos utilizados

El sistema compara varios algoritmos o modelos candidatos. No depende de un unico modelo, sino que evalua alternativas y promueve el modelo con mejor desempeno.

### 4.1 Persistencia del ultimo valor

Tipo:

```text
Baseline
```

Funcionamiento:

Usa el ultimo mes observado como prediccion para los meses de validacion.

Uso en tesis:

Sirve como linea base minima. Cualquier modelo mas avanzado deberia igualar o superar este comportamiento.

### 4.2 Promedio movil de 3 meses

Tipo:

```text
Baseline interpretable
```

Funcionamiento:

Calcula el promedio de los ultimos tres meses para suavizar fluctuaciones.

Uso:

- util cuando la serie historica aun es pequena
- facil de explicar al usuario
- sirve como modelo competitivo en datos con ruido

### 4.3 Regresion lineal temporal

Tipo:

```text
Regresion
```

Funcionamiento:

Ajusta una tendencia lineal sobre la serie mensual de articulos.

Uso:

- detectar tendencia creciente
- detectar tendencia descendente
- estimar continuidad del comportamiento historico

### 4.4 ML.NET ForecastBySsa

Tipo:

```text
Forecasting / series temporales
```

Tecnologia:

```text
Microsoft.ML
Microsoft.ML.TimeSeries
```

Funcionamiento:

Usa el algoritmo `ForecastBySsa` de ML.NET, basado en Singular Spectrum Analysis, para generar pronosticos de series temporales.

Configuracion aplicada:

- ventana dinamica segun tamano de la serie
- horizonte de prediccion: 6 meses
- nivel de confianza: 95%
- limites inferior y superior de confianza
- semilla fija `MLContext(seed: 7)` para reproducibilidad

Uso:

Es el modelo principal propuesto para prediccion temporal cuando la serie historica tiene suficientes meses.

## 5. Como entrena el modelo

El entrenamiento se realiza desde el backend mediante:

```text
InstitutionalIntelligenceService.RunTrainingAsync(...)
```

El endpoint expuesto es:

```text
POST api/intelligence/training/run
```

### 5.1 Dataset de entrenamiento

Dataset usado:

```text
Produccion cientifica mensual
```

Variable objetivo:

```text
Total de articulos por mes
```

Caracteristicas usadas o derivadas:

| Caracteristica | Fuente | Uso |
| --- | --- | --- |
| Periodo | Dimension temporal / reportería | Ordenar la serie mensual. |
| Total articulos | ReporterIA institucional | Variable objetivo. |
| Tendencia | Serie historica | Medir crecimiento o descenso. |
| Volatilidad | Serie historica | Estimar confianza y rangos. |
| Ventana movil | Serie historica | Representar ritmo reciente de publicacion. |

### 5.2 Separacion entrenamiento-validacion

El sistema usa validacion temporal, no mezcla aleatoria.

Regla aplicada:

| Meses disponibles | Meses para validacion |
| ---: | ---: |
| 18 o mas | 6 |
| 12 a 17 | 4 |
| 8 a 11 | 3 |
| Menos de 8 | 0 |

Cuando hay validacion:

```text
Primeros meses -> entrenamiento
Ultimos meses -> validacion
```

Esto es correcto para series temporales porque respeta el orden cronologico de los datos.

### 5.3 Evaluacion de algoritmos

El sistema evalua cada algoritmo contra los meses de validacion.

Metricas calculadas:

| Metrica | Significado |
| --- | --- |
| MAE | Error absoluto medio. Es la metrica principal para escoger el modelo. |
| RMSE | Penaliza errores grandes. Sirve como metrica complementaria. |
| MAPE | Error porcentual medio. Se usa como senal complementaria, especialmente delicada cuando hay meses con pocos articulos. |
| Score | Aproximacion de desempeno basada en `100 - MAPE`. |

La metrica principal de seleccion es:

```text
MAE
```

## 6. Como escoge el modelo

El sistema ordena los modelos evaluados por:

1. menor MAE
2. menor RMSE en caso de empate

El primer modelo evaluado con menor error es marcado como:

```text
IsBest = true
```

Luego se registra como:

```text
BestAlgorithm
BestMetric = MAE
BestMetricValue
```

Cuando se ejecuta el entrenamiento manual, si existe un modelo evaluado, el sistema lo promueve y genera una version activa.

Ejemplos de version:

```text
mlnet-ssa-20260514023535
baseline-moving-average-20260514023535
baseline-regression-20260514023535
baseline-persistence-20260514023535
```

Si no hay datos suficientes:

```text
Status = InsufficientData
ActiveModelVersion = sin-version
PromotedAlgorithm = Sin modelo promovido
```

## 7. Como se reentrena

El reentrenamiento se ejecuta desde el modulo IA mediante el boton de reentrenar o mediante el endpoint:

```text
POST api/intelligence/training/run
```

Politica definida en el sistema:

```text
Reentrenar despues de cada ETL completo o cuando ingresen nuevos meses con produccion confirmada.
```

Flujo de reentrenamiento:

1. Se consulta nuevamente la reportería institucional.
2. Se reconstruye la serie mensual de articulos.
3. Se separa entrenamiento y validacion.
4. Se evaluan los algoritmos candidatos.
5. Se selecciona el modelo con menor MAE.
6. Se genera version de modelo.
7. Se guarda historial del entrenamiento.
8. Se invalida cache del dashboard IA para mostrar el nuevo resultado.

## 8. Historial y auditoria del entrenamiento

El sistema guarda cada corrida de entrenamiento en base de datos.

Tablas usadas:

```text
IntelligenceTrainingRuns
IntelligenceTrainingAlgorithmMetrics
```

Informacion registrada:

- RunId
- fecha de inicio
- fecha de finalizacion
- estado
- si el modelo fue promovido
- algoritmo promovido
- version activa
- razon de seleccion
- resumen del entrenamiento
- usuario que ejecuto
- dataset usado
- filas de entrenamiento
- filas de validacion
- metrica ganadora
- metricas por algoritmo

Esto permite sustentar en la tesis que el modulo tiene trazabilidad y auditoria del proceso de IA.

## 9. Preparacion de datos y calidad

Antes de recomendar o entrenar, el sistema calcula un diagnostico de preparacion.

Indicadores de preparacion:

| Indicador | Que mide |
| --- | --- |
| DatasetVolumeScore | Volumen de articulos disponibles. |
| AuthorTraceScore | Trazabilidad de autores. |
| LoadQualityScore | Calidad de la carga de datos. |
| IndexingCoverageScore | Cobertura de indexacion. |
| WorkflowSignalScore | Senales disponibles del flujo operativo. |
| OverallScore | Preparacion general para IA. |

Tambien calcula calidad temporal:

- meses disponibles
- meses con articulos
- meses vacios
- primer mes
- ultimo mes
- mes pico
- concentracion porcentual del mes pico
- meses faltantes para entrenar

Esto ayuda a explicar al usuario si la IA esta lista para entrenar o si aun necesita mas datos.

## 10. Escenarios IA contemplados

El sistema define escenarios de IA actuales y futuros.

| Escenario | Estado | Algoritmo recomendado |
| --- | --- | --- |
| Prediccion de produccion | Implementado parcialmente | ML.NET ForecastBySsa / regresion |
| Recomendacion editorial | Implementado por reglas explicables | Ranking por reglas + clasificacion futura |
| Riesgo de demora | Planificado | Clasificacion binaria |
| Calidad de datos | Implementado parcialmente por reglas | Reglas explicables + clasificacion |

## 11. Arquitectura funcional del modulo IA

Flujo general:

```text
Datos transaccionales
        ↓
ETL / Data Warehouse
        ↓
ReporterIA institucional
        ↓
Servicio de IA
        ↓
Predicciones + recomendaciones + diagnostico
        ↓
Dashboard IA
        ↓
Reentrenamiento e historial
```

Endpoints principales:

| Endpoint | Funcion |
| --- | --- |
| `GET api/intelligence/dashboard` | Devuelve predicciones, recomendaciones, diagnostico y escenarios. |
| `POST api/intelligence/training/run` | Ejecuta reentrenamiento manual. |
| `GET api/intelligence/training/history` | Consulta historial de entrenamientos. |

## 12. Que aporta el modulo IA a la tesis

El modulo IA aporta:

1. prediccion institucional de produccion cientifica mensual
2. prediccion por facultad
3. prediccion por linea de investigacion
4. recomendaciones editoriales
5. recomendaciones de calidad de datos
6. recomendaciones de autores y colaboracion
7. diagnostico de preparacion de datos
8. comparacion de algoritmos
9. seleccion automatica del mejor modelo por MAE
10. historial auditable de entrenamientos
11. integracion con el DW y reportería

## 13. Limitaciones actuales

El modulo ya tiene una base funcional, pero todavia existen limites importantes:

- la prediccion principal trabaja con serie mensual agregada
- no existe aun un modelo predictivo supervisado para riesgo de demora
- la recomendacion editorial es explicable por reglas, no por aprendizaje de ranking avanzado
- no se guarda fisicamente un archivo de modelo ML.NET, se registra la version y metricas del entrenamiento
- la calidad del resultado depende directamente de la cantidad de meses y completitud de datos
- MAPE puede ser inestable cuando hay meses con muy pocos articulos

## 14. Mejoras futuras recomendadas

Para una siguiente version del modulo IA:

1. Guardar modelos ML.NET entrenados como artefactos versionados.
2. Agregar entrenamiento programado posterior al ETL.
3. Implementar prediccion de riesgo de demora del workflow.
4. Implementar recomendador editorial con ranking por revista, facultad, linea y cuartil.
5. Agregar modelo de calidad de datos para predecir registros con probabilidad de error.
6. Permitir comparar el modelo activo contra uno nuevo antes de promoverlo.
7. Agregar metricas de adopcion y utilidad percibida por usuarios.
8. Agregar explicabilidad por variable o senal usada en la recomendacion.

## 15. Redaccion sugerida para la tesis

El modulo de inteligencia artificial del sistema se diseno para apoyar la toma de decisiones institucionales a partir de la informacion historica de produccion cientifica. Para ello, el sistema transforma los datos registrados y consolidados en el modelo analitico en una serie temporal mensual de articulos, sobre la cual se aplican modelos de prediccion y reglas de recomendacion explicables.

La prediccion principal estima la produccion cientifica esperada para los siguientes seis meses, utilizando como modelo principal `ForecastBySsa` de ML.NET cuando existe suficiente historico. Adicionalmente, el sistema compara modelos base como persistencia del ultimo valor, promedio movil de tres meses y regresion lineal temporal. La seleccion del modelo se realiza mediante validacion temporal, separando los primeros meses para entrenamiento y los ultimos para validacion. El criterio principal de seleccion es el menor MAE, apoyado por RMSE y MAPE como metricas complementarias.

El sistema tambien genera recomendaciones institucionales relacionadas con calidad de datos, trazabilidad de autores, cobertura de indexacion, estrategia Open Access, colaboracion autoral y uso de predicciones como alertas tempranas. Cada entrenamiento queda registrado en un historial auditable, incluyendo algoritmo promovido, version activa, metricas obtenidas, cantidad de filas de entrenamiento y validacion, y usuario que ejecuto el proceso.

De esta forma, el modulo IA permite pasar de una reportería descriptiva a una analitica predictiva y prescriptiva inicial, manteniendo trazabilidad, explicabilidad y una ruta clara de mejora futura.
