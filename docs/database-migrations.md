# Migraciones locales

Desarrollo utiliza SQL Server `.\DINNOVA`, base `tesis`, con autenticación de Windows.
Las fábricas de diseño permiten ejecutar EF sin iniciar la autenticación JWT ni las tareas de arranque.

| Contexto | Conexión | Tablas | Historial |
| --- | --- | --- | --- |
| AppDbContext | DefaultConnection | dbo | dbo.__EFMigrationsHistory |
| DwContext | DefaultConnection | DW | DW.__EFMigrationsHistory |
| ArticlesDbContext | ArticlesOltpConnection | dbo | dbo.__EFMigrationsHistoryArticles |

`IndexingSources` pertenece a AppDbContext y se comparte con artículos. Artículos mapea su propiedad `IndexingSourceId` a la columna `Id` y no migra ese catálogo. Al crear una base nueva, ejecutar AppDbContext antes de ArticlesDbContext y apuntar ambos a la misma base.

Desde `tesisproject.backend`, generar una migración únicamente para los contextos cuyos modelos cambiaron, utilizando un nombre nuevo:

```powershell
dotnet ef migrations add NombreDelCambio --context AppDbContext --output-dir Migrations -- --environment Development
dotnet ef migrations add NombreDelCambioDW --context DwContext --output-dir Migrations/Dw -- --environment Development
dotnet ef migrations add NombreDelCambioArticulos --context ArticlesDbContext --output-dir Migrations/Articles -- --environment Development
```

Aplicar todas las pendientes:

```powershell
dotnet ef database update --context AppDbContext -- --environment Development
dotnet ef database update --context DwContext -- --environment Development
dotnet ef database update --context ArticlesDbContext -- --environment Development
```

Para consultar las tablas de la base:

```sql
SELECT s.name AS Esquema, t.name AS Tabla
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name;
```

## Reconciliación realizada el 2026-09-05

La base local ya había aplicado `UnifyScientificProductionStorage` y `SeparateAnalyticsMigrationHistory`, ausentes del árbol actual. Por eso ya existían las tablas iniciales de artículos, AppConfigurations y el historial de DW. Ejecutar nuevamente las migraciones de creación habría fallado.

Se realizó un respaldo COPY_ONLY con CHECKSUM y se probó la reconciliación en una copia restaurada. Se crearon las cuatro tablas RegistrationMatrix, RegistrationMatrixColumn, RegistrationMatrixRow y RegistrationMatrixCell, y se ajustaron 15 claves foráneas al comportamiento de borrado configurado en los modelos actuales (CASCADE o SET NULL).

Antes de registrar ArticlesFusionSync y ActualizarArticulos como aplicadas, un script transaccional comprobó la igualdad de 988 elementos de esquema (columnas, valores predeterminados, índices y claves foráneas) con una base creada desde cero mediante las migraciones. Se conservaron los historiales anteriores y los conteos de filas de las 90 tablas preexistentes. EF aplicó después ActualizarModelo, que no contiene cambios de esquema.

El script de auditoría local está en `artifacts/database/reconcile-tesis-20260905.sql` (directorio ignorado por Git). Es de una sola ejecución para el estado histórico verificado; no es parte del flujo normal ni debe ejecutarse otra vez sobre la base actualizada.

Respaldo local: `C:\Program Files\Microsoft SQL Server\MSSQL16.DINNOVA\MSSQL\Backup\tesis_before_context_reconcile_20260905.bak`.
