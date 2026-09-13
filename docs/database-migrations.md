# Migraciones activas

El backend tiene tres streams de migración independientes:

| Contexto | Conexión | Esquema | Historial |
| --- | --- | --- | --- |
| `UnifiedDideDbContext` | `UnifiedDideConnection` | `dbo` | `dbo.__EFMigrationsHistoryUnifiedDide` |
| `ProjectsDwContext` | `ProjectsDwConnection` | `DW` | `DW.__EFMigrationsHistory` |
| `ArticlesDwContext` | `ArticlesDwConnection` | `ArticlesDW` | `ArticlesDW.__EFMigrationsHistory` |

Las migraciones Unified de despliegue se ejecutan con `--migrate-unified`. Las
migraciones DW solo se aplican automáticamente cuando
`DatabaseBootstrap:ApplyDwMigrations=true`; el valor predeterminado es `false`.

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
