# OLAPDW v1 - Notas de revision

## Estado

`OLAPDW_v1_reviewed.sql` es la copia de trabajo del modelo analitico. `OLAPDW_v1_original.sql` se conserva como referencia intacta del script recibido.

`OLAPDW_v1_validation.sql` contiene consultas de comprobacion para ejecutar despues de `EXEC etl.sp_RunFullLoad;`.

`OLAPDW_v1_bootstrap.sql` y `run_olapdw_v1.ps1` preparan una ejecucion controlada sobre `TesisDW_Extensible`.

`OLAPDW_v1_incremental_existing.sql` actualiza una base `TesisDW_Extensible` que ya contiene una version previa del modelo.

## Estado local de ejecucion

- `TesisDW_Extensible` ya existia localmente con una version previa del modelo.
- Se aplico `OLAPDW_v1_incremental_existing.sql` correctamente.
- Se ejecuto `EXEC etl.sp_RunFullLoad;` correctamente despues del ajuste incremental.
- `TesisDB_Extensible` local no contiene todavia las tablas fisicas `IndexingSources`, `ArticleIndexings` ni `Projects`; la carga de indexacion queda tolerante y devuelve cero filas hasta que esas tablas existan.
- En la base local validada, los hechos principales estan en cero porque el OLTP consultado no tenia registros cargados para articulos/lotes/workflow al momento de la prueba.

## Ajustes aplicados

- Se agrego `dw.DimIndexingSource` para catalogar fuentes de indexacion.
- Se agrego `dw.FactArticleIndexing` para medir articulos por fuente de indexacion.
- Se agrego `dw.vw_Articles_ByIndexingSource`.
- Se agregaron `etl.sp_Load_DimIndexingSource` y `etl.sp_Load_FactArticleIndexing`.
- Se agregaron `etl.sp_Load_DimRegistrationSource` y `etl.sp_Load_DimBatchStatus` como cargas semilla/dinamicas.
- `etl.sp_RunFullLoad` ahora carga `DimDate`, fuentes, estados e indexacion.
- `etl.sp_PopulateDimDate` ahora tiene rango por defecto `2000-01-01` a `2050-12-31`.
- Se ajustaron procedimientos que esperaban columnas no existentes en el OLTP actual, usando constantes o `NULL` controlado.
- `FactWorkflowStage` y `FactWorkflowAction` ahora incluyen `BatchId_OLTP`.
- Se agregaron vistas de pipeline: `dw.vw_Workflow_CurrentPipeline` y `dw.vw_Workflow_Batches_ByCurrentStage`.
- Se agrego `dw.vw_Authors_UniqueProduction` para reportes de autores consolidados sin cambiar aun la granularidad de `DimAuthor`.
- Se agrego `OLAPDW_v1_validation.sql` para validar conteos, llaves criticas y vistas principales despues de la carga.
- Se agrego `OLAPDW_v1_bootstrap.sql` para crear la base destino separada si no existe.
- Se agrego `run_olapdw_v1.ps1` como runner controlado con modo dry-run por defecto.
- Se agrego `OLAPDW_v1_incremental_existing.sql` porque la base local `TesisDW_Extensible` ya contiene una version previa del modelo.

## Decisiones pendientes

- `Project` existe en OLTP, pero `Article` no tiene relacion directa con `Project`. Por ahora no se conecta al hecho de articulos para no crear una dimension sin uso real.
- `DimAuthor` conserva granularidad por participacion (`ArticleParticipantId_OLTP`). Para reportes institucionales se usa la vista `vw_Authors_UniqueProduction`.
- La fuente de registro se infiere por `ImportBatch.SourceType` cuando existe un lote asociado al articulo, y por `ExternalSource` como respaldo.
- El modelo sigue usando carga completa para varios hechos. Antes de produccion conviene decidir si se mantiene full load o se implementa carga incremental.

## Antes de ejecutar en SQL Server

- Confirmar nombres reales de bases: `TesisDB_Extensible` y `TesisDW_Extensible`.
- Confirmar que se usara `TesisDW_Extensible` para no afectar `TesisDW`, que aun esta configurada como `DwConnection` legacy.
- Confirmar que las tablas nuevas del OLTP existan en la base local, especialmente `IndexingSources`, `Projects`, `RegistrationMatrix`, `Workflow*` e `ImportBatch*`.
- Ejecutar primero en una base DW limpia o restaurable.
- Validar que `sp_RunFullLoad` complete sin errores.
- Ejecutar `OLAPDW_v1_validation.sql` despues de la carga.
- Comparar conteos OLTP vs DW para articulos, participantes, lotes, errores y etapas de workflow.
- Si existen metricas de revistas con `Year` fuera del rango 2000-2050, ampliar el rango de `etl.sp_PopulateDimDate`.
