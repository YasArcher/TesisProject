# Pruebas de carga funcional

Plan complementario:

1. `C:\Users\Personal\Source\Repos\TesisProject\docs\plan-pruebas-oltp-extensible.md`

Esta carpeta deja una base reproducible para probar el sistema nuevo en dos flujos:

1. `registro manual agregado`
2. `carga masiva con staging`

La idea no es hacer benchmarking sintético puro, sino validar:

1. tiempos de respuesta razonables
2. comportamiento con lotes medianos y grandes
3. manejo de errores intencionales
4. estabilidad del staging durante validación, corrección y procesamiento

## Scripts incluidos

### 1. Generar CSV masivo

Archivo:

- `C:\Users\Personal\Source\Repos\TesisProject\tests\load\generate-bulk-import-csv.ps1`

Ejemplos:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\load\generate-bulk-import-csv.ps1 -RowCount 50
powershell -ExecutionPolicy Bypass -File .\tests\load\generate-bulk-import-csv.ps1 -RowCount 250 -ErrorEvery 10
powershell -ExecutionPolicy Bypass -File .\tests\load\generate-bulk-import-csv.ps1 -RowCount 1000 -ErrorEvery 25
```

Qué genera:

1. CSV con columnas estables del modelo nuevo
2. filas válidas
3. filas con error intencional cada `ErrorEvery`

Errores que introduce:

1. `Title` vacío
2. `Year` inválido
3. `Nombre` de participante vacío

Eso sirve para revisar:

1. upload al staging
2. validación
3. cola de corrección
4. proceso posterior

### 2. Ejecutar flujo de carga masiva

Archivo:

- `C:\Users\Personal\Source\Repos\TesisProject\tests\load\invoke-bulk-import-scenario.ps1`

Ejemplos:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-bulk-import-scenario.ps1 -CsvPath .\tests\load\generated-bulk-import.csv -Validate
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-bulk-import-scenario.ps1 -CsvPath .\tests\load\generated-bulk-import.csv -Validate -Process
```

Qué mide:

1. tiempo de upload
2. tiempo de validación
3. tiempo de procesamiento
4. resumen del lote por etapa

Requisito:

1. backend de pruebas levantado en `http://localhost:5041`

### 3. Ejecutar carga repetida del registro manual

Archivo:

- `C:\Users\Personal\Source\Repos\TesisProject\tests\load\invoke-registration-load.ps1`

Ejemplos:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 10
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 50
powershell -ExecutionPolicy Bypass -File .\tests\load\invoke-registration-load.ps1 -Count 100 -PauseMs 100
```

Qué mide:

1. total de éxitos/fallos
2. tiempo promedio por registro
3. tiempo mínimo y máximo
4. errores devueltos por el endpoint

## Escenarios recomendados

### Escenario A. Smoke

1. registro manual: `10`
2. carga masiva: `50` filas sin error

Objetivo:

1. confirmar que el sistema responde bien de punta a punta

### Escenario B. Errores controlados

1. carga masiva: `100` filas
2. `ErrorEvery 10`

Objetivo:

1. revisar staging
2. revisar validación
3. revisar bandeja de corrección
4. corregir algunas filas manualmente

### Escenario C. Volumen medio

1. registro manual: `50`
2. carga masiva: `250`

Objetivo:

1. ver tiempos reales
2. revisar respuesta de UI
3. confirmar que el lote sigue navegable

### Escenario D. Volumen alto prudente

1. carga masiva: `1000`
2. con y sin errores

Objetivo:

1. revisar cuánto tarda upload
2. cuánto tarda validate
3. cuánto tarda process
4. si la pantalla se vuelve pesada

## Qué observar

### Backend

1. tiempos de upload
2. tiempos de validate
3. tiempos de process
4. excepciones o timeouts

### Frontend

1. si la tabla del lote sigue siendo utilizable
2. si la bandeja de corrección sigue respondiendo
3. si el modal de fila sigue siendo cómodo
4. si hay bloqueos visuales o scroll excesivo

### Datos

1. filas válidas vs error
2. artículos creados
3. venues creados o resueltos
4. participantes creados
5. consistencia entre staging y proceso final

## Siguiente paso sugerido

Correr en este orden:

1. `Smoke`
2. `Errores controlados`
3. `Volumen medio`
4. `Volumen alto prudente`

Cuando tengamos esos resultados, el siguiente paso natural es endurecer:

1. rendimiento del preview
2. paginación del lote
3. procesamiento por bloques
4. carga masiva externa desde APIs
