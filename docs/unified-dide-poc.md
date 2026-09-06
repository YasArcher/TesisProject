# POC de unificación DIDE

> Informe histórico. El estado final funcional y la nueva base `tesis_unified` se documentan en [unified-dide-final.md](unified-dide-final.md).

## Actualización: migración aplicada por solicitud posterior

Se creó la base independiente **`tesis_unified_poc`** en **`.\DINNOVA`** y se aplicó `20260906023613_InitialUnifiedDide` mediante el SQL generado por EF Core, dentro de una transacción. La aplicación conserva su conexión a `tesis`; no se cambió DI ni se retiró el bloqueo offline del contexto experimental.

Verificación posterior al commit: **74 tablas del modelo + historial, 101 FK, 131 índices secundarios y 3 CHECK**. Todas las FK y CHECK están habilitadas y son confiables; no hay cascadas. El historial registra EF Core 9.0.8. La base original `tesis` tiene **0 tablas Unified**. No se trasladaron datos ni se ejecutaron seeds.

Evidencia: `artifacts/unified-database-validation.json`. Script utilizado: `artifacts/apply-unified-poc.ps1` (destino fijo; rechaza bases no vacías). Las referencias a validación offline en el informe siguiente describen la fase inicial, anterior a esta autorización.

---

La POC compila y EF Core 9.0.8 genera correctamente el modelo operacional unificado: **74 tablas, 101 FK y 131 índices**. Se agregaron únicamente archivos nuevos. Los cambios locales preexistentes en `Program.cs`, configuración y migraciones se conservaron. No se ejecutó `database update`, ni sincronización, seeds o traslado de datos.

## Aislamiento y entidades

- Entidades independientes en `tesisproject.backend/Data/UnifiedEntities/`, con namespaces propios; no heredan de entidades actuales. Los enums e interfaces compartidos existentes se reutilizan sin modificarlos.
- Nueva entidad `Authors/Author.cs`. `ProductAuthorDynamicFieldValue` reemplaza conceptualmente la extensión dinámica de participantes.
- Copias ajustadas: `Product`, `ProductAuthor`, `Article`, `Faculty`, `AcademicTerm`, `AppUser` (constraints), `ProductAttribute` (navegación redundante). Relaciones adicionales configuradas para `Project`, `Visit`, `FacultyScopeFaculty` y `ObjectiveActivityUser`.
- Se incluyen las dependencias operacionales de ambos contextos: presupuestos/transacciones, documentos, objetivos/actividades/visitas, grupos, convocatorias/reglas, catálogos, exportaciones, configuración, publicaciones/indexaciones/archivos, formularios/campos dinámicos y matrices de registro.
- `UnifiedDideDbContext` es un `DbContext` independiente, sin registro en DI. Sus configuraciones se seleccionan exclusivamente por namespace. No incluye Identity, tokens, DW ni el código archivado/excluido de `ArticlesMigration`.
- La factory no lee appsettings, secretos ni arranque de la aplicación. Usa una conexión ficticia y un interceptor que rechaza aperturas síncronas y asíncronas. Permite construir metadatos y generar SQL; bloquea el uso accidental de la POC contra una BD.

## Relaciones y restricciones

```text
Project 1 ── N Product 1 ── 0..1 Article
AppUser 0..1 ── Author ── 0..1 ExternalResearcher  [exactamente una fuente]
Author 1 ── N ProductAuthor N ── 1 Product
Faculty 1 ── N Projects / Articles / FacultyScopeFaculties
AcademicTerm 1 ── N Visits / Articles
Project ── Group ── GroupMembers ── AppUsers
Project ── ExternalResearcherProjects ── ExternalResearcher
ObjectiveActivityUsers.UserId ── AppUsers.IdUser
```

`Article.ProductId` es obligatorio, FK y UNIQUE; no existe `Article.ProjectId`. `Title` y `CreatedAt` quedan en `Product`, junto con descripción, tipo y ciclo de vida. DOI, año/fecha de publicación, páginas, venue, indexaciones, acceso abierto, procedencia de importación y clasificaciones científicas quedan en `Article`. `IsProjectResult` resulta redundante en esta POC: todo artículo pertenece a un producto con proyecto obligatorio. `GroupName` y `Filiacion` permanecen como información declarada de la publicación; no se presume que coincidan con el grupo actual del proyecto.

`Author` exige XOR de fuentes mediante CHECK y UNIQUE filtrados por cada FK nullable. ORCID está en el perfil académico, con UNIQUE para valores no vacíos. `ProductAuthors` impide duplicar `(ProductId, AuthorId)` y, cuando se informa, `(ProductId, AuthorOrder)`; el orden debe ser positivo. Es nullable para productos que antes no almacenaban orden. No se impone un único autor principal: el modelo previo no demuestra esa regla.

Todas las FK usan **NoAction**, incluidas las dependencias de artículos que antes tenían Cascade/SetNull. Se comprobaron PK, nulabilidad, orden de creación de tablas, ausencia de cascadas y de columnas ocultas, cobertura de la migración y rollback limitado a `Unified`. La autorreferencia de categorías se conserva sin borrado en cascada.

## Destino de ArticleParticipants

| Campo actual | Destino Unified | Motivo |
|---|---|---|
| `Id` | `ProductAuthor.Id` | Identidad de participación nueva; una futura carga deberá conservar una tabla de equivalencias. |
| `ArticleId` | `ProductAuthor.ProductId` vía `Article.ProductId` | Autoría común a todos los productos. |
| `Index` | `ProductAuthor.AuthorOrder` | Orden de firma en ese producto. |
| `Participacion` | `ProductAuthor.Participation` | Rol Autor/Coautor, como valida `ArticleRegistrationCommandService`. |
| `ParticipantType` | `ProductAuthor.ParticipantTypeSnapshot` | Docente/Estudiante/Administrativo/Externo/Otro en el momento del registro; no equivale a la fuente XOR. |
| `IsPrimaryAuthor` | `ProductAuthor.IsPrimaryAuthor` | Condición específica de esa autoría. |
| `Affiliation` | `ProductAuthor.AffiliationSnapshot` | Afiliación declarada en esa publicación, no necesariamente institución actual. |
| `Nombre` | `ProductAuthor.NameSnapshot` | El nombre actual externo está en `ExternalResearcher.FullName`; el institucional se obtiene por el puente AppUser desde Identity/directorio, no está almacenado en AppUser. |
| `Email` | `ProductAuthor.EmailSnapshot` | Correo histórico; el externo actual está en `ExternalResearcher.Email` y el institucional en su fuente. |
| `Identificacion` | `ProductAuthor.IdentificationSnapshot` | Identidad histórica; `ExternalUserProfileModel.Document` contiene el dato institucional, pero AppUser y ExternalResearcher actuales no lo almacenan. |
| `InstitutionalPersonId` | `ProductAuthor.InstitutionalPersonIdSnapshot` | El código conserva un entero sin resolver su equivalencia a IdUser/IdAsp. No se crea una FK especulativa; el vínculo real pasa por Author.AppUserId. |
| `Orcid` | `Author.Orcid` | Identificador académico reutilizable de la persona. |
| `ExternalAuthorId` | `ProductAuthor.ExternalAuthorIdSnapshot` | No hay proveedor ni unicidad global documentados; se conserva el valor importado sin convertirlo en identidad maestra. |
| `CreatedAt`, `UpdatedAt` | `ProductAuthor` | Auditoría de participación; no de la persona. |
| `DynamicFieldValues` | `ProductAuthorDynamicFieldValue` | Conserva todos los tipos de valores, auditoría y FK al catálogo de campos. |

`ArticleParticipant` original continúa intacto y presente en `ArticlesDbContext`; no existe en Unified. Los snapshots no sustituyen las fuentes actuales ni disparan sincronización.

## Catálogos y evidencia

| Concepto | Decisión y referencia inspeccionada |
|---|---|
| Facultades | `ExternalFacultyCareerFlatModel`: Id, ParentId, Name, Acronym; `ExternalAcademicsService.IsFaculty` selecciona ParentId null. `Faculty` conserva PK local y agrega ExternalFacultyId UNIQUE nullable, Acronym y LastSyncedAt. Name proviene de la fuente. IsActive y CreatedAt son metadata local; no se interpreta la actividad de una asignación docente como actividad de una facultad. No se inventa un Code externo. |
| Períodos | `ExternalAcademicPeriodModel`: PeriodId, Name, StartDate, EndDate. `AcademicTerm` conserva PK local y agrega ExternalPeriodId UNIQUE nullable, fechas y LastSyncedAt. Fechas ambas ausentes o ambas presentes en orden correcto mediante CHECK, para permitir registros locales aún no conciliados. Visits.AcademicPeriodId y Articles.AcademicTermId apuntan a este mismo catálogo. |
| AppUsers | Conserva únicamente IdUser/IdLocal/IdAsp. `AppUserService.EnsureSingleInternalAsync` busca por IdAsp y luego IdLocal antes de crear: respalda UNIQUE filtrados. IdLocal queda como referencia opaca a autenticación fuera de la POC, sin FK a tablas actuales ni copia de AspNetUsers. |
| Countries / Institutions | Catálogos locales con su relación existente; no reciben IDs externos ni metadata de sincronización. |
| ResearchCategoryGroup / Type / Category | Jerarquía local configurable utilizada por proyectos; conserva árbol padre/hijos y ProjectResearchCategories. No se encontró contrato externo de sincronización. |
| ResearchLines | Catálogo local de artículos distinto de la jerarquía anterior. No existe correspondencia demostrada para fusionarlos. |
| BroadFields / SpecificFields / DetailedFields | Taxonomía jerárquica OECD según las configuraciones actuales; se conservan relaciones y unicidad por padre. No se presume equivalencia con ResearchCategories ni un proveedor de sincronización. |
| IndexingSources | Una copia compartida del catálogo de proyectos. La configuración actual de artículos ya mapea IndexingSourceId a IndexingSources.Id y excluye esa tabla de sus migraciones. Unified utiliza ese Id como principal y sí crea la tabla propia del esquema. |

## Límites y asuntos pendientes

- No se adaptaron servicios, repositorios, UoW, controladores, DTOs, clientes, frontend, autenticación, seeds ni DW. Las pruebas también construyen ambos contextos actuales y verifican que no incorporen entidades Unified.
- Artículos actuales sin proyecto, sin título, identidades sin conciliar y ORCID duplicados necesitan decisiones/limpieza antes de una futura migración de datos. Esta POC comprueba el contrato solicitado con proyecto obligatorio; no afirma que todos los registros existentes se puedan cargar directamente.
- La FK nueva de ObjectiveActivityUsers usa la identidad DIDE coherente con GroupMembers y las otras relaciones de usuario; el servicio actual no valida la existencia de AppUser y el comentario de la entidad menciona Identity. Los datos existentes deberán auditarse antes de convertirlos; no se reinterpretan IDs automáticamente.
- Faculty.Code anterior no se equipara automáticamente a siglas; la POC usa Acronym según el contrato externo. No hay servicio de conciliación de IDs locales/externos. Nombre de facultad/período deja de ser clave única: la identidad externa, cuando existe, es el criterio de unicidad.
- Al mover Title fuera de Article, el índice anterior UNIQUE `(Title, Year, VenueId)` condicionado a ausencia de DOI no puede mantenerse como índice simple entre tablas. Se conserva UNIQUE de DOI; la deduplicación sin DOI queda pendiente de decisión. No se duplica Title para simular ese índice.
- ProductValues y campos dinámicos se conservan para extensibilidad. Una futura carga deberá evitar almacenar de nuevo DOI/título u otros campos canónicos allí; no se cambiaron definiciones ni seeds para imponer esa política.
- Se corrigió solo en Unified la navegación directa `ProductAttribute.Values`, que producía una FK oculta adicional. Se accede por `TypeDefinitions.ProductValues`. También se quitó la longitud 64 configurada sobre FacultyScopeFaculty.FacultyId entero y se hizo explícita la precisión decimal, conservando precisiones específicas existentes.
- Las referencias heredadas de matrices (`LastImportBatchId`, `CreatedByUserId` string) se preservan sin inventar relaciones con modelos de importación o autenticación excluidos. No se añadieron reglas especulativas de coherencia entre facultad del proyecto/artículo o entre las clasificaciones OECD elegidas.

## Migración y validación

- Nombre: **20260906023613_InitialUnifiedDide**.
- Carpeta: `tesisproject.backend/Migrations/Unified/`, con archivo principal, Designer y `UnifiedDideDbContextModelSnapshot.cs`.
- Contexto: `UnifiedDideDbContext`; schema: `Unified`; historial: `Unified.__EFMigrationsHistoryUnifiedDide`.
- `dotnet build TesisProject.sln --no-restore --verbosity quiet`: correcto, 0 errores en la comprobación final incremental. La primera compilación del backend mostró advertencias existentes de nulabilidad; la POC no agregó advertencias propias al finalizar.
- `dotnet run --project tests/tesisproject.unifiedmodeltests/tesisproject.unifiedmodeltests.csproj --no-restore --verbosity quiet`: **117 comprobaciones correctas**, incluyendo coincidencia del snapshot, fuentes institucional/externa en un grafo EF, aislamiento de los contextos originales y bloqueo de conexiones.
- `dotnet ef migrations script --idempotent --context UnifiedDideDbContext --project tesisproject.backend/tesisproject.backend.csproj --no-build --output artifacts/unified-dide.sql`: correcto. SQL generado e inspeccionado, **no ejecutado**. No se probó la ejecución de constraints en un servidor SQL: su definición y SQL emitido se validaron offline.

El listado exacto de archivos nuevos está en [unified-dide-files.txt](unified-dide-files.txt). Los logs y el SQL son artefactos regenerables en `artifacts/`; los archivos de código y documentación de la POC se pueden retirar sin cambiar los contextos actuales.
