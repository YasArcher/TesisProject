# Migraciones activas

El backend usa una sola base física, `tesis_unified`, con tres `DbContext` y tres streams de migración independientes:

| Contexto | Conexión | Esquema | Historial |
| --- | --- | --- | --- |
| `UnifiedDideDbContext` | `UnifiedDideConnection` | `dbo` | `dbo.__EFMigrationsHistory` |
| `ProjectsDwContext` | `ProjectsDwConnection` | `ProjectsDW` | `ProjectsDW.__EFMigrationsHistory` |
| `ArticlesDwContext` | `ArticlesDwConnection` | `ArticlesDW` | `ArticlesDW.__EFMigrationsHistory` |

Las tres conexiones deben resolver al mismo servidor, base y usuario. En Docker, `DB_NAME=tesis_unified` es la única fuente del nombre físico.

El job `migrate-database` ejecuta `--migrate-database` y aplica, en orden:

1. `UnifiedDideDbContext.Database.MigrateAsync()`
2. `ProjectsDwContext.Database.MigrateAsync()`
3. `ArticlesDwContext.Database.MigrateAsync()`

El job falla ante cualquier error. La API solo arranca cuando el job termina con éxito. `MigrateAsync` crea `tesis_unified` si no existe; no se requiere crear bases o schemas manualmente.

El bootstrap normaliza dos nombres históricos antes de migrar una instalación existente: `dbo.__EFMigrationsHistoryUnifiedDide` pasa a `dbo.__EFMigrationsHistory`, y `DW.__EFMigrationsHistory` pasa a `ProjectsDW.__EFMigrationsHistory`. Si existen ambos nombres simultáneamente, el proceso se detiene para evitar mezclar streams.

Desde `tesisproject.backend`, los comandos locales son:

```powershell
dotnet ef migrations add NombreUnified --context UnifiedDideDbContext --output-dir Migrations/Unified -- --environment Development
dotnet ef migrations add NombreProjectsDw --context ProjectsDwContext --output-dir Migrations/ProjectsDw -- --environment Development
dotnet ef migrations add NombreArticlesDw --context ArticlesDwContext --output-dir Migrations/ArticlesDw -- --environment Development
```

Las migraciones históricas de `AppDbContext` están en
`LegacyHistory/AppDbContextMigrations` y se excluyen de compilación. No son un
stream activo y no deben aplicarse al runtime Unified. Su conservación no altera
las tablas ni `__EFMigrationsHistory` existentes.
