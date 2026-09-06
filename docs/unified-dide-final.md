# Modelo DIDE unificado final

**Resultado:** `UnifiedDideDbContext` completo, migración inicial limpia y base nueva **`tesis_unified`**, creada y validada físicamente. Servidor configurado: **`.\DINNOVA`** (SQL Server informa `Archer\DINNOVA`). Esquema: **`dbo`**. Las bases `tesis` y `tesis_unified_poc` no se modificaron en esta fase.

## Contexto y configuración

- `tesisproject.backend/Data/UnifiedDideDbContext.cs`: hereda de `IdentityDbContext<IdentityUser<int>, IdentityRole<int>, int>`, constructor normal y llamada a `base.OnModelCreating`. Selecciona únicamente `UnifiedConfigurations` por namespace y aplica `NoAction` a todas las FK al finalizar.
- `UnifiedDideDbContext.DbSets.cs`: todos los DbSets operacionales; Identity aporta los suyos. No incorpora DW.
- Las configuraciones de proyectos, autoría e integración se extrajeron de los antiguos parciales a `IEntityTypeConfiguration<T>` en `UnifiedConfigurations`. Se conservaron las relaciones, constraints e índices de la POC, con los ajustes de esta fase.
- `UnifiedDideDbContextFactory.cs`: utiliza `ConnectionStrings:UnifiedDideConnection` desde Development/local; no ejecuta el arranque de la aplicación. Se eliminó el interceptor offline y la conexión ficticia. La factory rechaza otros entornos, credenciales SQL/servidores remotos, bases de sistema, la POC aplicada y coincidencias con las bases de los contextos actuales.
- Único cambio de configuración de la aplicación: se agregó `UnifiedDideConnection` en `appsettings.Development.json`, con autenticación Windows local y base `tesis_unified`. No se reemplazó ninguna conexión existente ni se modificó DI.

## Cambios respecto a la POC

| Entidad / área | Decisión final |
|---|---|
| Identity y RefreshToken | Siete tablas Identity y copia Unified de RefreshToken. FK `RefreshTokens.UserId → AspNetUsers.Id`; hash de token UNIQUE. |
| AppUser | Se mantienen IdUser, IdLocal e IdAsp. Se incorpora `IdLocal → AspNetUsers.Id`. UNIQUE filtrados de IdLocal/IdAsp; IdAsp sigue sin FK externa. |
| Product | `ProjectId` ahora nullable: permite producción científica independiente. Título, descripción, tipo y auditoría permanecen aquí. |
| Article | `ProductId` obligatorio y UNIQUE: `Product 1 → 0..1 Article`. No tiene ProjectId ni IsProjectResult; la pertenencia al proyecto se deriva del producto. |
| Author | XOR AppUser/ExternalResearcher, UNIQUE filtrados para cada fuente, ORCID y nueva propiedad `ExternalAuthorId`. Esta última es opaca y nullable, sin asumir proveedor ni unicidad global. |
| ProductAuthor | Conserva autor, producto, orden, participación, autor principal y snapshots justificados. UNIQUE `(ProductId, AuthorId)` y `(ProductId, AuthorOrder)` filtrado cuando hay orden. CHECK de orden positivo. |
| Faculty | PK local, ExternalFacultyId, Name, Acronym y LastSyncedAt; CreatedAt/IsActive son metadatos locales. Compartida por Project y Article. |
| AcademicTerm | PK local, ExternalPeriodId, Name, StartDate, EndDate y LastSyncedAt. La propiedad `Visit.AcademicPeriodId` se renombra a `AcademicTermId`, igual que Article. |
| ObjectiveActivityUser | Conserva `UserId → AppUsers.IdUser`. |
| Participación en proyectos | GroupMembers, ExternalResearcherProjects y ProductAuthors siguen independientes. |
| Catálogos restantes | Countries/Institutions locales. ResearchCategories/Types/Groups, ResearchLines y campos OECD siguen separados. IndexingSources permanece compartido. |

**Evidencia para ProjectId nullable:** el código activo de `ArticleRegistrationCommandService.RegisterAsync` copia `request.Article.IsProjectResult` sin exigir que sea true; `ValidateRequestAsync` no exige ni recibe un proyecto. `ArticleAggregateCoreDto en RegisterArticleAggregateDtos.cs` (datos del agregado) contiene el booleano y no un ProjectId. `ArticlesRegister.razor` asigna ese booleano desde el formulario. Por tanto, el contrato de registro admite artículos independientes. Se probaron físicamente ambos casos; no se añadió un booleano redundante.

## Campos heredados de ArticleParticipants

| Campo anterior | Destino / justificación |
|---|---|
| Id / ArticleId | Nueva identidad ProductAuthor.Id y vínculo ProductId mediante Article.ProductId. No se migraron datos ni IDs antiguos. |
| Index | ProductAuthor.AuthorOrder: orden de firma, nullable para productos sin orden declarado. |
| Participacion / IsPrimaryAuthor | ProductAuthor.Participation / IsPrimaryAuthor: autoría concreta del producto. |
| Nombre | NameSnapshot: nombre de firma registrado/publicado, que puede diferir del nombre actual del directorio o investigador. |
| Identificacion | IdentificationSnapshot: identificación declarada al registrar la participación; conserva el dato con el que se documentó esa autoría. No actúa como clave ni determina automáticamente la fuente. |
| Email | EmailSnapshot: contacto declarado para esa publicación, independiente del correo actual de la persona. |
| Affiliation | AffiliationSnapshot: afiliación declarada en ese producto, distinta de la institución actual. |
| ParticipantType | ParticipantTypeSnapshot: condición al registrar (Docente/Estudiante/Administrativo/Externo/Otro), distinta del rol Autor/Coautor y de la fuente XOR. |
| InstitutionalPersonId | Se elimina InstitutionalPersonIdSnapshot. El código lo trata como referencia de identidad seleccionada/importada, sin uso histórico ni equivalencia demostrada a IdUser/IdAsp. El vínculo institucional nuevo es Author.AppUserId; no se inventa otra FK ni se copia el entero ambiguo. |
| Orcid | Author.Orcid: identidad académica reutilizable. |
| ExternalAuthorId | Author.ExternalAuthorId: referencia externa del autor. Se elimina su repetición en cada participación. |
| CreatedAt / UpdatedAt | Auditoría de ProductAuthor. |
| DynamicFieldValues | ProductAuthorDynamicFieldValue: valores y FK al catálogo de campos conservados. |

La justificación de los snapshots se apoya en `ArticleRegistrationCommandService`, que persiste los valores suministrados, y `ArticleQueryService`, que presenta los participantes registrados sin sustituirlos por una consulta al directorio. Se conservan los datos declarados de la publicación; no se crearon snapshots de claves internas.

**ExternalAuthorId:** el código activo únicamente guarda/expone ese campo como texto; no acredita su asociación con Scopus, OpenAlex u otro proveedor. El explorador externo archivado tiene proveedores y búsqueda de autores Scopus, pero no una relación persistida entre esos resultados y ExternalAuthorId. No se crea `AuthorExternalIdentifier` ni se impone unicidad global sin esa evidencia. Esto evita repetir una referencia de autor en cada producto sin sobrearquitecturar el modelo.

## Migración y base física

- Inicial: **`20260906042715_InitialUnifiedDide`**.
- Ubicación: `tesisproject.backend/Migrations/Unified/`, archivo principal, Designer y `UnifiedDideDbContextModelSnapshot.cs`.
- Historial: **`dbo.__EFMigrationsHistoryUnifiedDide`**, una única migración aplicada con EF Core 9.0.8.
- La migración anterior de la POC se retiró de la compilación y quedó archivada en `artifacts/unified-poc-migration-archive/`. No se alteró la base POC ya existente.
- Antes de aplicar se comprobó mediante `master.sys.databases` que `tesis_unified` no existía. Se comparó la conexión con DefaultConnection, ArticlesOltpConnection y ArticlesOlapConnection y se imprimieron servidor, base, contexto y migración sin credenciales.

Comandos ejecutados desde la raíz:

```powershell
dotnet ef migrations add InitialUnifiedDide --context UnifiedDideDbContext --project tesisproject.backend/tesisproject.backend.csproj --output-dir Migrations/Unified -- --environment Development
dotnet ef database update InitialUnifiedDide --context UnifiedDideDbContext --project tesisproject.backend/tesisproject.backend.csproj --no-build -- --environment Development
dotnet build TesisProject.sln --no-restore --verbosity quiet
dotnet run --project tests/tesisproject.unifiedmodeltests/tesisproject.unifiedmodeltests.csproj --no-restore --verbosity quiet -- --database
```

## Validación realizada

| Comprobación | Resultado |
|---|---|
| Compilación y generación inicial | Correctas. Compilación final de solución incremental: 0 errores y 0 advertencias. La compilación completa del backend mantiene 19 advertencias preexistentes fuera de Unified. |
| database update | Correcto; EF creó la nueva BD y aplicó únicamente InitialUnifiedDide. |
| Contratos del modelo | **211 comprobaciones correctas**, incluyendo cobertura de todas las entidades de AppDbContext/ArticlesDbContext y sus dos reemplazos, aislamiento de tipos, snapshot actualizado y ausencia de DW/cascadas/columnas ocultas. |
| Modelo físico | **82 tablas de modelo + 1 de historial**, incluidas **7 Identity**. **82 PK, 109 FK, 140 índices secundarios y 3 CHECK**. |
| Tipos y columnas | Comparación de todas las columnas físicas con EF: tipo, longitud/precisión, nullable e indicador identity. |
| Claves y constraints | PK, columnas/principales de FK, UNIQUE, filtros de índices y definiciones CHECK coinciden con EF. FK/CHECK habilitadas y confiables; todas las FK NoAction. |
| Tablas sustituidas | No existen ArticleParticipants ni ArticleParticipantDynamicFieldValues. |
| Datos mínimos | Se persistieron y consultaron artículo de proyecto e independiente, Identity/AppUser/RefreshToken, facultad/período compartidos, visita/actividad y un mismo producto con autor institucional y externo. |
| Integridad negativa | **13 escrituras rechazadas por SQL Server**: XOR sin/ambas fuentes, perfiles duplicados, autoría/orden duplicados, orden no positivo, segundo Article por Product, FK de autor/Identity/actividad inválidas, borrado NoAction y fechas de período inválidas. |
| Limpieza | Transacción de prueba revertida. **0 filas de negocio** al terminar; únicamente queda el registro del historial. No se ejecutaron seeds. |

Las pruebas son un ejecutable de validación fuera del runtime, no Services. Sin `--database` solo construyen y verifican metadatos. La modalidad con BD está restringida a `tesis_unified`.

Evidencias regenerables: `artifacts/unified-final-database-update.log`, `artifacts/unified-final-database-validation.log`, `artifacts/unified-final-physical-summary.json`, `artifacts/unified-final-solution-build.log` y SQL generado `artifacts/unified-final.sql`.

## Alcance preservado y decisiones funcionales pendientes

Se verificaron hashes de los archivos existentes al inicio: no se modificaron controllers, services, repositories, UoW, DTOs, frontend, DW, contextos anteriores ni Program.cs. Solo se agregó la conexión independiente en Development y se cambiaron archivos propios de Unified, pruebas y documentación. La aplicación sigue utilizando sus contextos actuales.

No hay bloqueos para usar el nuevo modelo EF. Antes de integrar fuentes/datos históricos deben definirse dos puntos: el proveedor/contrato de `Author.ExternalAuthorId` y la regla de deduplicación de artículos sin DOI (el anterior índice de título/año/venue no puede mantenerse como índice simple después de trasladar el título a Product). No se inventaron reglas para resolverlos.

El listado exacto de archivos creados, modificados y retirados en esta fase está en [unified-dide-final-files.txt](unified-dide-final-files.txt).
