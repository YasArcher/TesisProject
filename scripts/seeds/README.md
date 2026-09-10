# Semilla Projects Unified

Adaptada de `9.txt` proporcionado por el usuario. La huella SHA256, cantidades y excepciones están en `projects-unified.manifest.json`. La semilla autocontenida es `projects-unified.sql`; no ejecuta instrucciones ni jobs del archivo original.

## Ejecutar

Desde la raíz del repositorio, PowerShell:

```powershell
# Prevalidación: solo temporales SQL; no inserta/actualiza tablas de aplicación.
./scripts/seeds/Invoke-ProjectsSeed.ps1

# Carga explícita y transaccional.
./scripts/seeds/Invoke-ProjectsSeed.ps1 -Apply
```

La conexión procede de `ConnectionStrings__UnifiedDideConnection` o del archivo local ignorado `tesisproject.backend/appsettings.Local.json`. No hay credenciales en esta semilla. Por defecto exige `tesis_unified`, IdAsp institucional 1 y email `admin@uta.edu.ec`. Para otro despliegue, configurar conexión y parámetros `-ExpectedDatabase`, `-OwnerAspId`, `-OwnerEmail` explícitamente. Nunca admite `tesis` ni bases de sistema; exige las migrations Unified hasta HardenArticleReadModel.

Resuelve al operador mediante **IdAsp → AppUser.IdLocal → Identity con superadmin**; en la aplicación actual es AppUser.IdUser **15**, Identity.Id **13**. No inserta usuarios ni cambia roles. Las 23 tablas importadas no tienen campos de creador, por lo que no se añade una atribución ficticia ni se modifica ownership de proyectos existentes. El resultado SQL identifica ActorAppUserId como referencia de la operación, no como una nueva columna persistida.

## Datos incluidos

462 filas en 23 tablas:

| Grupo | Filas |
| --- | ---: |
| DocumentTypes / GroupTypes / Convocations | 6 / 2 / 47 |
| ProjectStates / ProjectTypes / ProjectOriginTypes / ProjectExtensionTypes | 6 / 2 / 3 / 2 |
| TransactionTypes / VisitStates / ObjectiveTypes / FundingTypes / MemberRoleTypes | 3 / 4 / 2 / 4 / 4 |
| ResearchCategoryGroups / ResearchCategoryTypes / ResearchCategories | 4 / 8 / 163 |
| Countries / Institutions | 40 / 36 |
| ExportFields / ExportTemplates / ExportTemplateColumns | 30 / 1 / 30 |
| ProductTypes / ProductAttributes / ProductAttributeDefinitions | 7 / 19 / 39 |

El archivo no contiene inserciones de proyectos registrados: es configuración inicial del módulo Projects. Países e instituciones son catálogos locales de este modelo; no se confunden con facultades/periodos sincronizados. Las categorías jerárquicas Projects se conservan en ResearchCategories, sin poblar otro catálogo Articles.

Se conservan IDs canónicos, required, orden, flags, jerarquías y metadatos de exportación. Todas las cadenas se emiten como Unicode y se usan los nombres/tipos físicos actuales de Unified. No hay DELETE de columnas de plantilla: inserción idempotente y rechazo de diferencias desconocidas.

## Exclusiones y particularidades

- Los 12 INSERT de `usuario` y el JSON de credenciales del documento no se copian ni ejecutan. Son datos de identidad/directorio externo.
- Faculties y AcademicTerms no se rellenan manualmente. Los datos sincronizados existentes se conservan.
- Se excluyen Country 41 e Institution 37 (`NO VALIDO`) y ExternalResearcher 1 (`AGREGAR MANUALMENTE`): el propio archivo los identifica como placeholders/falsos.
- No se crean Forms/Fields Articles. El tipo regional tiene **seis** definitions en la fuente, sin DOI/Year/SJR/Quartile propios; no se inventaron definitions para completar otro flujo.
- No se ejecutan el procedimiento de transición de estados ni instrucciones de SQL Agent/MSDB. Son comportamiento/scheduling, no datos iniciales, y apuntaban al esquema/BD anterior.
- Se conserva `ProjectOriginTypes[2].Name = Extern0` tal como está en la fuente.
- La plantilla repite `BUDGET_CERTIFIED` en las posiciones 25 y 26 (ExportFieldId 22). Se conserva esa decisión literal; no se sustituye silenciosamente una columna por otro concepto financiero.

## Conflictos y seguridad de la carga

Las filas ya idénticas no se cambian. Un ID con contenido diferente provoca error y rollback. Solo se permite corregir las siete filas con firmas exactas de fixtures documentados en `tests/tesisproject.cutoversmoketests/HttpSmoke.cs` y `AcademicSmoke.cs`:

- Convocations 1: `Cutover smoke`/`CT` → `SIN CONVOCATORIA`/NULL.
- ProductTypes 1: `Producto smoke` → `PRODUCCIÓN CIENTÍFICA`.
- ProjectTypes 1: `Investigación` → `Aplicada`.
- ProjectStates 3: `En ejecución` → `EN EJECUCION`.
- GroupTypes 1: IsLocked 1 → 0, conforme al archivo.
- MemberRoleTypes 1: `Coordinador` → `Coordinador Principal`.
- MemberRoleTypes 3: IsLocked 1 → 0, conforme al archivo.

Se valida que las referencias afectadas sigan siendo registros smoke `CA-*`/`CT-*`, con las demás restricciones codificadas en SQL; si aparecen referencias ajenas, se detiene. No se borran proyectos, productos, autores ni grupos. No existe una autorización genérica para sobrescribir cualquier catálogo parecido.

La prevalidación termina todos los chequeos de contenido antes de mutar. La carga usa una transacción SERIALIZABLE con XACT_ABORT; las FK/índices únicos SQL también deben cumplirse. Ante error, se cierra la conexión y se revierte la transacción. No se deshabilitan constraints.

## Resultado de la carga local

Aplicada a `tesis_unified`: **454 inserciones, 7 correcciones de fixtures, 1 fila ya idéntica**. Total: 462 filas. Prevalidación posterior: **0 inserciones / 0 correcciones**.

Se conservaron 2 Projects, 3 Products, 3 ProductAuthors, 11 AppUsers, 11 AspNetUsers, 53 Faculties, 23 AcademicTerms y 0 FormDefinitions. No se reasignaron sus propietarios.

33 pruebas SQL PASS sobre BD temporal creada desde las migrations Unified: esquema real, prevalidación sin escritura, superadmin requerido, conflictos, protección de referencias reales, fallo al final con rollback total, carga de los 23 catálogos, idempotencia y exclusiones. Comando:

```powershell
dotnet run --project tests/tesisproject.articleruntimetests -c Release -- project-seed-test
```

Evidencia local no versionada: `artifacts/projects-seed-before.json` (snapshot previo de catálogos), `projects-seed-protected-counts.json`, `projects-seed-applied.log`, `projects-seed-final-preview.log` y `projects-seed-tests.log`. El documento original con credenciales personales no se incorporó al repositorio.
