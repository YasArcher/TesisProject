# Catálogo inicial de Projects

Fuente runtime canónica: data migration C# `PROJECTS_INITIAL_CATALOG_V1`.
Contiene 465 filas en 24 secciones tipadas. Conserva IDs, flags, relaciones,
jerarquías, configuración de exportación y textos Unicode.

Rutas:

```text
GET  /api/admin/data-migrations
POST /api/admin/data-migrations/PROJECTS_INITIAL_CATALOG_V1/apply
```

Ambas requieren JWT con rol `superadmin`. `POST` también requiere header
`X-Admin-Operation-Secret`, configurado mediante:

```text
AdministrativeOperations__Enabled=true
AdministrativeOperations__Secret=<secret independiente>
```

Bootstrap de Identity/superadmin debe terminar antes. Data migration no crea
usuarios, Faculties, AcademicTerms ni configuración de Articles.

Ejecución idempotente. IDs existentes idénticos quedan sin cambios. Conflicto
ambiguo cancela y revierte toda operación. Siete correcciones de fixtures smoke
históricas siguen permitidas solo para firmas exactas y sin referencias reales.

Cada intento queda en `dbo.OperationExecutionHistory`. Solo puede existir un
`SUCCEEDED` para este código. `ResultJson` contiene conteos por sección.

## Validación

Equivalencia verificada en BD temporal creada desde migrations Unified: 24
secciones, 465 filas, fixture conocido, exclusiones, idempotencia, constraint
JSON y Unicode.

```powershell
dotnet run --project tests/tesisproject.articleruntimetests -c Release -- data-migration-test
```

`projects-unified.manifest.json` queda como evidencia histórica. No participa
en runtime.
