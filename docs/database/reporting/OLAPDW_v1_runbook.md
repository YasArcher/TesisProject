# OLAPDW v1 - Runbook de ejecucion controlada

## Objetivo

Crear y validar el nuevo DW analitico en una base separada llamada `TesisDW_Extensible`, sin tocar la base legacy `TesisDW` que aun usa el backend actual.

## Archivos

- `OLAPDW_v1_bootstrap.sql`: crea la base `TesisDW_Extensible` si no existe.
- `OLAPDW_v1_reviewed.sql`: crea schemas, tablas, vistas, FKs y procedimientos ETL del modelo revisado.
- `OLAPDW_v1_validation.sql`: valida conteos, llaves criticas y vistas principales despues de la carga.
- `OLAPDW_v1_original.sql`: referencia intacta del script recibido.
- `OLAPDW_v1_notes.md`: decisiones y observaciones del modelo.

## Orden recomendado

1. Crear base destino:

```powershell
sqlcmd -S "PERSONAL\DINNOVA" -U "<usuario>" -P "<password>" -i "docs\database\reporting\OLAPDW_v1_bootstrap.sql"
```

2. Crear modelo DW:

```powershell
sqlcmd -S "PERSONAL\DINNOVA" -U "<usuario>" -P "<password>" -i "docs\database\reporting\OLAPDW_v1_reviewed.sql"
```

3. Ejecutar carga completa:

```sql
USE [TesisDW_Extensible];
EXEC etl.sp_RunFullLoad;
```

4. Ejecutar validaciones:

```powershell
sqlcmd -S "PERSONAL\DINNOVA" -U "<usuario>" -P "<password>" -i "docs\database\reporting\OLAPDW_v1_validation.sql"
```

## Validaciones esperadas

- El ultimo registro de `etl.EtlRun` debe quedar en `Success`.
- `dw.DimArticle` debe aproximarse al conteo de `TesisDB_Extensible.dbo.Articles`.
- `dw.DimAuthor` debe aproximarse a `TesisDB_Extensible.dbo.ArticleParticipants`.
- `dw.FactRegistrationBatch` debe aproximarse a `TesisDB_Extensible.dbo.ImportBatch`.
- `dw.FactWorkflowStage` debe aproximarse a `TesisDB_Extensible.dbo.WorkflowStageInstance`.
- `dw.FactWorkflowAction` debe aproximarse a `TesisDB_Extensible.dbo.WorkflowActionLog`.
- Las vistas `dw.vw_KPI_ProduccionCientifica`, `dw.vw_KPI_CalidadCarga` y `dw.vw_KPI_Workflow` deben devolver filas sin error.

## Conexion con backend

El backend actual tiene `DwConnection` apuntando a `TesisDW`. Para no romper la reportería legacy, la recomendacion es agregar una conexion nueva en una fase posterior:

```json
"ReportingConnection": "Server=PERSONAL\\DINNOVA;Database=TesisDW_Extensible;..."
```

Despues se puede crear un nuevo `ReportingDbContext` o un servicio de lectura basado en vistas `dw.vw_*`.

## Endpoints backend agregados

El backend ya expone un canal paralelo para el nuevo DW, sin modificar `api/reports` legacy:

```http
GET /api/reporting/health
GET /api/reporting/dashboard
```

Ambos usan `ReportingConnection` y la politica `ReportingAccess`.

## Riesgos conocidos

- Si `TesisDB_Extensible` no existe con ese nombre exacto, los procedimientos ETL fallaran.
- Si el usuario SQL no tiene permisos de creacion de base, el bootstrap fallara.
- Si ya existen objetos `dw` o `etl` en `TesisDW_Extensible`, el script de modelo puede fallar por objetos duplicados.
- Si existen fechas fuera del rango 2000-2050, ampliar los parametros de `etl.sp_PopulateDimDate`.
