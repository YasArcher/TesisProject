# Primera pasada mecánica del modelo DIDE

Fecha: 2026-09-06. Estado: código preparado por etapas; compilación correcta. **La integración de ejecución sigue pendiente.**

## A. Cambios aplicados

Se modificaron 30 archivos de código, con 320 referencias semánticas migradas y 7 registros de importación migrados. No se modificaron reglas, DTO, frontend, autenticación, DW/ETL, Program, migraciones ni el modelo Unified. No se ejecutó ninguna operación de BD en esta fase.

El detalle exacto de tipos, contextos y líneas retiradas/agregadas está en [applied.csv](applied.csv); cada aparición semántica antes/después está en [migrated-references.csv](migrated-references.csv).

| Archivo | Tipo/contexto antiguo → nuevo | Referencias |
| --- | --- | --- |
| tesisproject.backend/Repositories/Implementations/ConvocationRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.Convocation> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.Convocation><br>tesisproject.shared.Entities.Core.Convocation → tesisproject.backend.Data.UnifiedEntities.Core.Convocation<br>tesisproject.shared.Entities.Core.ConvocationRule → tesisproject.backend.Data.UnifiedEntities.Core.ConvocationRule<br>tesisproject.shared.Entities.Core.ConvocationRuleIndexing → tesisproject.backend.Data.UnifiedEntities.Core.ConvocationRuleIndexing | 39 |
| tesisproject.backend/Repositories/Implementations/ExportFieldRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.shared.Entities.Export.ExportField → tesisproject.backend.Data.UnifiedEntities.Export.ExportField | 12 |
| tesisproject.backend/Repositories/Implementations/ExportTemplateColumnRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.shared.Entities.Export.ExportTemplateColumn → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplateColumn | 9 |
| tesisproject.backend/Repositories/Implementations/ExportTemplateRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.shared.Entities.Export.ExportTemplate → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplate | 12 |
| tesisproject.backend/Repositories/Implementations/GenericRepository.cs | Campo AppDbContext → DbContext; constructor protegido DbContext adicional | 0 |
| tesisproject.backend/Repositories/Implementations/ObjectiveActivityUserRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.ObjectiveActivityUser> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.ObjectiveActivityUser><br>tesisproject.shared.Entities.Core.ObjectiveActivityUser → tesisproject.backend.Data.UnifiedEntities.Core.ObjectiveActivityUser | 13 |
| tesisproject.backend/Repositories/Implementations/ProductAttributeDefinitionRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition><br>tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition → tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition | 12 |
| tesisproject.backend/Repositories/Implementations/ProjectObjectiveRepository.cs | DbSet por propiedad → Set<T> equivalente; contexto y entidades legacy conservados | 0 |
| tesisproject.backend/Repositories/Implementations/VisitIssueRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.VisitIssue> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.VisitIssue><br>tesisproject.shared.Entities.Core.VisitIssue → tesisproject.backend.Data.UnifiedEntities.Core.VisitIssue | 14 |
| tesisproject.backend/Repositories/Implementations/VisitObjectiveActivityProgressRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress><br>tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress → tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress | 14 |
| tesisproject.backend/Repositories/Interfaces/IConvocationRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.Convocation> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.Convocation><br>tesisproject.shared.Entities.Core.Convocation → tesisproject.backend.Data.UnifiedEntities.Core.Convocation<br>tesisproject.shared.Entities.Core.ConvocationRule → tesisproject.backend.Data.UnifiedEntities.Core.ConvocationRule | 14 |
| tesisproject.backend/Repositories/Interfaces/IExportFieldRepository.cs | tesisproject.shared.Entities.Export.ExportField → tesisproject.backend.Data.UnifiedEntities.Export.ExportField | 10 |
| tesisproject.backend/Repositories/Interfaces/IExportTemplateColumnRepository.cs | tesisproject.shared.Entities.Export.ExportTemplateColumn → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplateColumn | 7 |
| tesisproject.backend/Repositories/Interfaces/IExportTemplateRepository.cs | tesisproject.shared.Entities.Export.ExportTemplate → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplate | 10 |
| tesisproject.backend/Repositories/Interfaces/IObjectiveActivityUserRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.ObjectiveActivityUser> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.ObjectiveActivityUser><br>tesisproject.shared.Entities.Core.ObjectiveActivityUser → tesisproject.backend.Data.UnifiedEntities.Core.ObjectiveActivityUser | 7 |
| tesisproject.backend/Repositories/Interfaces/IProductAttributeDefinitionRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition><br>tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition → tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition | 7 |
| tesisproject.backend/Repositories/Interfaces/IVisitIssueRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.VisitIssue> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.VisitIssue><br>tesisproject.shared.Entities.Core.VisitIssue → tesisproject.backend.Data.UnifiedEntities.Core.VisitIssue | 9 |
| tesisproject.backend/Repositories/Interfaces/IVisitObjectiveActivityProgressRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress><br>tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress → tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress | 5 |
| tesisproject.backend/Services/Implementations/AppConfigurationRepository.cs | tesisproject.backend.Data.AppDbContext → tesisproject.backend.Data.UnifiedDideDbContext<br>tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Core.AppConfiguration> → tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.AppConfiguration><br>tesisproject.shared.Entities.Core.AppConfiguration → tesisproject.backend.Data.UnifiedEntities.Core.AppConfiguration | 14 |
| tesisproject.backend/Services/Implementations/ConvocationService.cs | tesisproject.shared.Entities.Core.Convocation → tesisproject.backend.Data.UnifiedEntities.Core.Convocation<br>tesisproject.shared.Entities.Core.ConvocationRule → tesisproject.backend.Data.UnifiedEntities.Core.ConvocationRule<br>tesisproject.shared.Entities.Core.ConvocationRuleIndexing → tesisproject.backend.Data.UnifiedEntities.Core.ConvocationRuleIndexing | 20 |
| tesisproject.backend/Services/Implementations/ExportTemplateService.cs | tesisproject.shared.Entities.Export.ExportField → tesisproject.backend.Data.UnifiedEntities.Export.ExportField<br>tesisproject.shared.Entities.Export.ExportTemplate → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplate<br>tesisproject.shared.Entities.Export.ExportTemplateColumn → tesisproject.backend.Data.UnifiedEntities.Export.ExportTemplateColumn | 23 |
| tesisproject.backend/Services/Implementations/ObjectiveActivityService.cs | tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress → tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress | 5 |
| tesisproject.backend/Services/Implementations/ObjectiveActivityUserService.cs | tesisproject.shared.Entities.Core.ObjectiveActivityUser → tesisproject.backend.Data.UnifiedEntities.Core.ObjectiveActivityUser | 8 |
| tesisproject.backend/Services/Implementations/ProductAttributeDefinitionService.cs | tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition → tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition | 11 |
| tesisproject.backend/Services/Implementations/ProductService.cs | tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition → tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition | 7 |
| tesisproject.backend/Services/Implementations/ProductTypeDesignService.cs | tesisproject.shared.Entities.Core.Products.ProductAttributeDefinition → tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition | 9 |
| tesisproject.backend/Services/Implementations/ProjectService.cs | tesisproject.shared.Entities.Core.Convocation → tesisproject.backend.Data.UnifiedEntities.Core.Convocation | 10 |
| tesisproject.backend/Services/Implementations/VisitIssueService.cs | tesisproject.shared.Entities.Core.VisitIssue → tesisproject.backend.Data.UnifiedEntities.Core.VisitIssue | 7 |
| tesisproject.backend/Services/Implementations/VisitObjectiveActivityProgressService.cs | tesisproject.shared.Entities.Core.VisitObjectiveActivityProgress → tesisproject.backend.Data.UnifiedEntities.Core.VisitObjectiveActivityProgress | 5 |
| tesisproject.backend/Services/Interfaces/IAppConfigurationRepository.cs | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.AppConfiguration> → tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.backend.Data.UnifiedEntities.Core.AppConfiguration><br>tesisproject.shared.Entities.Core.AppConfiguration → tesisproject.backend.Data.UnifiedEntities.Core.AppConfiguration | 7 |

GenericRepository mantiene su constructor público AppDbContext y añade uno protegido DbContext; el campo base admite ambos. ProjectObjectiveRepository conserva las entidades legacy y usa Set<T> equivalente en tres accesos; VisitObjectiveActivityProgressRepository lo usa en dos. Se conservaron BOM y saltos de línea originales.

Los intentos que rompían navegaciones o contratos se revirtieron: Project/Group/GroupMember, objetivos, documentos, presupuesto, investigadores externos y ProductValue. La cadena genérica de catálogos también queda pendiente: por ejemplo, GroupService asigna MemberRoleType al GroupMember legacy. Una clase hoja equivalente no vuelve compatible todo el agregado.

## B. Pendientes funcionales

**P0 — Integración de ejecución:** los constructores migrados necesitan UnifiedDideDbContext, todavía no registrado en Program. UnitOfWork.SaveChangesAsync sigue guardando AppDbContext. La aplicación no está lista para activar estos repositorios: puede fallar la resolución de DI y registrar solo Unified tampoco garantiza guardar sus cambios ni compartir transacciones. Se dejó intacta la DI global por instrucción expresa; esta fase acredita compilación, no ejecución de los flujos.

[pending.csv](pending.csv) contiene cada referencia pendiente con archivo, línea, clase, método, motivo y adaptación; incluye usos inferidos por el compilador y contratos bloqueados, no solamente cambios que exigen lógica nueva. P1: resolver semántica de negocio; P2: migrar contratos/agregados conectados.

### ProductAuthor

33 referencias; [tabla completa por archivo y método](pending-productauthor.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Repositories/Implementations/ProductAuthorRepository.cs:21 | ProductAuthorRepository.ExistsForUserAsync | tesisproject.shared.Entities.Core.Products.ProductAuthor.UserId | UserId identifica AppUser; AuthorId identifica otra entidad. | Resolver o crear Author desde AppUser/ExternalResearcher; adaptar consulta, vínculo y proyección. |
| P1 | tesisproject.backend/Services/Implementations/ProductService.cs:384 | ProductService.SyncAuthorsAsync | tesisproject.shared.Entities.Core.Products.ProductAuthor.UserId | UserId identifica AppUser; AuthorId identifica otra entidad. | Resolver o crear Author desde AppUser/ExternalResearcher; adaptar consulta, vínculo y proyección. |
| P1 | tesisproject.backend/Repositories/Interfaces/IProductAuthorRepository.cs:1 | (file).(namespace/import) | tesisproject.shared.Entities.Core.Products | UserId identifica AppUser; AuthorId identifica otra entidad. | Resolver o crear Author desde AppUser/ExternalResearcher; adaptar consulta, vínculo y proyección. |

### Articles

171 referencias; [tabla completa por archivo y método](pending-articles.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Repositories/Implementations/ArticleReadRepository.cs:74 | ArticleReadRepository.ApplyFilters | tesisproject.backend.Data.Articles.Entities.Article.Title | El contrato pertenece al agregado Articles legacy o depende de sus participantes, campos y borrados. | Migrar Product + Article y consultas/proyecciones conjuntamente; revisar Title/ProjectId/IsProjectResult y borrado con NoAction. Catálogos aislados quedan bloqueados por ese contrato. |
| P1 | tesisproject.backend/Services/Implementations/ArticleQueryService.cs:92 | ArticleQueryService.MapListItem | tesisproject.backend.Data.Articles.Entities.Article.Title | El contrato pertenece al agregado Articles legacy o depende de sus participantes, campos y borrados. | Migrar Product + Article y consultas/proyecciones conjuntamente; revisar Title/ProjectId/IsProjectResult y borrado con NoAction. Catálogos aislados quedan bloqueados por ese contrato. |
| P1 | tesisproject.backend/Services/Implementations/ArticleRegistrationCommandService.cs:40 | ArticleRegistrationCommandService.RegisterAsync | tesisproject.backend.Data.Articles.Entities.Article.Title | El contrato pertenece al agregado Articles legacy o depende de sus participantes, campos y borrados. | Migrar Product + Article y consultas/proyecciones conjuntamente; revisar Title/ProjectId/IsProjectResult y borrado con NoAction. Catálogos aislados quedan bloqueados por ese contrato. |
| P1 | tesisproject.backend/Controllers/ArticlesConfigurationController.cs:4 | (file).(namespace/import) | tesisproject.backend.Data.Articles | El contrato pertenece al agregado Articles legacy o depende de sus participantes, campos y borrados. | Migrar Product + Article y consultas/proyecciones conjuntamente; revisar Title/ProjectId/IsProjectResult y borrado con NoAction. Catálogos aislados quedan bloqueados por ese contrato. |

### ArticleParticipants

87 referencias; [tabla completa por archivo y método](pending-articleparticipants.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Services/Implementations/ArticleRegistrationCommandService.cs:77 | ArticleRegistrationCommandService.RegisterAsync | tesisproject.backend.Data.Articles.Entities.ArticleParticipant.InstitutionalPersonId | El participante y sus valores dinámicos no equivalen a ProductAuthor. | Definir Author, ProductAuthor, snapshots, ORCID, ExternalAuthorId y destino de campos dinámicos; adaptar este uso. |
| P1 | tesisproject.backend/Services/Implementations/RegistrationMatrixService.cs:166 | RegistrationMatrixService.LoadEligibleFieldsAsync | ArticleParticipant | El participante y sus valores dinámicos no equivalen a ProductAuthor. | Definir Author, ProductAuthor, snapshots, ORCID, ExternalAuthorId y destino de campos dinámicos; adaptar este uso. |
| P1 | tesisproject.backend/Controllers/ArticlesConfigurationController.cs:359 | ArticlesConfigurationController.DeleteField | tesisproject.backend.Data.Articles.ArticlesDbContext.ArticleParticipantDynamicFieldValues | El participante y sus valores dinámicos no equivalen a ProductAuthor. | Definir Author, ProductAuthor, snapshots, ORCID, ExternalAuthorId y destino de campos dinámicos; adaptar este uso. |
| P1 | tesisproject.backend/Services/Implementations/ArticleQueryService.cs:87 | ArticleQueryService.MapListItem | System.Collections.Generic.List<tesisproject.backend.Data.Articles.Entities.ArticleParticipant> | El participante y sus valores dinámicos no equivalen a ProductAuthor. | Definir Author, ProductAuthor, snapshots, ORCID, ExternalAuthorId y destino de campos dinámicos; adaptar este uso. |

### Identity/AppUser

31 referencias; [tabla completa por archivo y método](pending-identity-appuser.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Repositories/Implementations/AppUserRepository.cs:8 | AppUserRepository.(namespace/import) | tesisproject.backend.Repositories.Implementations.GenericRepository<tesisproject.shared.Entities.Auth.AppUser> | AppUser mantiene vínculos de negocio y un contrato de repositorio legacy. | Preservar IdLocal → Identity y IdUser → persona; migrar repositorio y consumidores juntos. ArticleUserContext ya resuelve GetByLocalIdAsync: conservar esa resolución. |
| P1 | tesisproject.backend/Repositories/Interfaces/IAppUserRepository.cs:5 | IAppUserRepository.(namespace/import) | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Auth.AppUser> | AppUser mantiene vínculos de negocio y un contrato de repositorio legacy. | Preservar IdLocal → Identity y IdUser → persona; migrar repositorio y consumidores juntos. ArticleUserContext ya resuelve GetByLocalIdAsync: conservar esa resolución. |
| P1 | tesisproject.backend/Services/Implementations/AppUserService.cs:126 | AppUserService.GetAppUserIdByLocalIdAsync | tesisproject.shared.Entities.Auth.AppUser | AppUser mantiene vínculos de negocio y un contrato de repositorio legacy. | Preservar IdLocal → Identity y IdUser → persona; migrar repositorio y consumidores juntos. ArticleUserContext ya resuelve GetByLocalIdAsync: conservar esa resolución. |
| P1 | tesisproject.backend/Services/Implementations/ArticleUserContext.cs:68 | ArticleUserContext.GetAppUserIdAsync | tesisproject.shared.Entities.Auth.AppUser | AppUser mantiene vínculos de negocio y un contrato de repositorio legacy. | Preservar IdLocal → Identity y IdUser → persona; migrar repositorio y consumidores juntos. ArticleUserContext ya resuelve GetByLocalIdAsync: conservar esa resolución. |

### Faculties

125 referencias; [tabla completa por archivo y método](pending-faculties.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Controllers/ArticlesConfigurationController.cs:756 | ArticlesConfigurationController.ReadCatalogItemsAsync | tesisproject.backend.Data.Articles.Entities.Faculty.FacultyId | El contrato comparte IDs de facultad de origen externo o navegaciones legacy; no acredita identidad local. | Resolver ExternalFacultyId a FacultyId local y revisar scopes, filtros y DTO; Code/Acronym en Articles requiere mapeo explícito. |
| P1 | tesisproject.backend/Repositories/Implementations/ArticleReadRepository.cs:88 | ArticleReadRepository.ApplyFilters | tesisproject.backend.Data.Articles.Entities.Article.FacultyId | El contrato comparte IDs de facultad de origen externo o navegaciones legacy; no acredita identidad local. | Resolver ExternalFacultyId a FacultyId local y revisar scopes, filtros y DTO; Code/Acronym en Articles requiere mapeo explícito. |
| P1 | tesisproject.backend/Repositories/Implementations/FacultyScopeRepository.cs:31 | FacultyScopeRepository.QueryWithRefs | tesisproject.shared.Entities.Core.UserFacultyScopeAssignment.User | El contrato comparte IDs de facultad de origen externo o navegaciones legacy; no acredita identidad local. | Resolver ExternalFacultyId a FacultyId local y revisar scopes, filtros y DTO; Code/Acronym en Articles requiere mapeo explícito. |
| P1 | tesisproject.backend/Repositories/Implementations/UserFacultyScopeAssignmentRepository.cs:16 | UserFacultyScopeAssignmentRepository.QueryWithRefs | tesisproject.shared.Entities.Core.UserFacultyScopeAssignment.User | El contrato comparte IDs de facultad de origen externo o navegaciones legacy; no acredita identidad local. | Resolver ExternalFacultyId a FacultyId local y revisar scopes, filtros y DTO; Code/Acronym en Articles requiere mapeo explícito. |

### AcademicTerms

120 referencias; [tabla completa por archivo y método](pending-academicterms.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Services/Implementations/ArticleQueryService.cs:103 | ArticleQueryService.MapListItem | tesisproject.backend.Data.Articles.Entities.Article.AcademicTermId | Debe verificarse/resolverse el período externo contra el catálogo local. | Migrar el agregado y usar AcademicTermId; resolver ExternalPeriodId cuando el origen sea externo. |
| P1 | tesisproject.backend/Services/Implementations/ArticleRegistrationCommandService.cs:54 | ArticleRegistrationCommandService.RegisterAsync | tesisproject.backend.Data.Articles.Entities.Article.AcademicTermId | Debe verificarse/resolverse el período externo contra el catálogo local. | Migrar el agregado y usar AcademicTermId; resolver ExternalPeriodId cuando el origen sea externo. |
| P1 | tesisproject.backend/Services/Implementations/ProjectExtensionService.cs:118 | ProjectExtensionService.CreateAsync | tesisproject.shared.Entities.Core.Visit.AcademicPeriodId | El agregado Visit sigue siendo legacy; esta asignación nula no necesita resolver IDs. | Migrar el agregado y usar AcademicTermId; resolver ExternalPeriodId cuando el origen sea externo. |
| P1 | tesisproject.backend/Services/Implementations/ProjectService.cs:1706 | ProjectService.ImportFromMatrixAsync | tesisproject.shared.Entities.Core.Visit.AcademicPeriodId | Debe verificarse/resolverse el período externo contra el catálogo local. | Migrar el agregado y usar AcademicTermId; resolver ExternalPeriodId cuando el origen sea externo. |

### Product.ProjectId nullable

54 referencias; [tabla completa por archivo y método](pending-product-projectid-nullable.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P1 | tesisproject.backend/Repositories/Implementations/ProductRepository.cs:24 | ProductRepository.GetByProjectAsync | tesisproject.shared.Entities.Core.Products.Product.ProjectId | El contrato Product legacy exige proyecto y está conectado a autores y DTO no nullable. | Migrar contrato Product con autores; definir proyección para productos sin proyecto, manteniendo filtros/asignaciones válidos. |
| P1 | tesisproject.backend/Services/Implementations/ProductService.cs:32 | ProductService.MapToListItemExpression | tesisproject.shared.Entities.Core.Products.Product.ProjectId | El contrato Product legacy exige proyecto y está conectado a autores y DTO no nullable. | Migrar contrato Product con autores; definir proyección para productos sin proyecto, manteniendo filtros/asignaciones válidos. |
| P1 | tesisproject.backend/Services/Implementations/ProjectFlatReportService.cs:141 | ProjectFlatReportService.GetFlatReportAsync | tesisproject.shared.Entities.Core.Products.Product.ProjectId | El contrato Product legacy exige proyecto y está conectado a autores y DTO no nullable. | Migrar contrato Product con autores; definir proyección para productos sin proyecto, manteniendo filtros/asignaciones válidos. |
| P1 | tesisproject.backend/Repositories/Interfaces/IProductRepository.cs:5 | IProductRepository.(namespace/import) | tesisproject.backend.Repositories.Interfaces.IGenericRepository<tesisproject.shared.Entities.Core.Products.Product> | El contrato Product legacy exige proyecto y está conectado a autores y DTO no nullable. | Migrar contrato Product con autores; definir proyección para productos sin proyecto, manteniendo filtros/asignaciones válidos. |

### Otros: unidad de trabajo

108 referencias; [tabla completa por archivo y método](pending-otros-unidad-de-trabajo.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P0 | tesisproject.backend/UnitOfWork/Implementations/UnitOfWork.cs:5 | (file).(namespace/import) | tesisproject.shared.Entities.Catalogs | SaveChangesAsync sigue ejecutándose sobre AppDbContext; hay repositorios adaptados a Unified. | Integrar contexto, transacciones, DI y repositorios de cada operación como una unidad antes de activar. No basta registrar Unified ni guardar dos contextos por separado. |
| P0 | tesisproject.backend/UnitOfWork/Interfaces/IUnitOfWork.cs:4 | (file).(namespace/import) | tesisproject.shared.Entities.Analytics.Dw.Dimensions | SaveChangesAsync sigue ejecutándose sobre AppDbContext; hay repositorios adaptados a Unified. | Integrar contexto, transacciones, DI y repositorios de cada operación como una unidad antes de activar. No basta registrar Unified ni guardar dos contextos por separado. |

### Otros: agregados y contratos

1343 referencias; [tabla completa por archivo y método](pending-otros-agregados-y-contratos.md).

| Prioridad | Archivo:línea | Clase/Método | Referencia | Motivo | Cambio |
| --- | --- | --- | --- | --- | --- |
| P2 | tesisproject.backend/Repositories/Implementations/BudgetRepository.cs:70 | BudgetRepository.GetByProjectIdAsync | tesisproject.shared.Entities.Core.Budget.ProjectId | El tipo forma parte de un agregado o contrato genérico que todavía devuelve entidades legacy. | Migrar conjuntamente repositorio, interfaz, UoW y navegaciones del agregado. Revisar Project/Group/GroupMember, objetivos, documentos, presupuesto y catálogos; no mezclar instancias EF de ambos modelos. |
| P2 | tesisproject.backend/Repositories/Implementations/ExternalResearcherProjectRepository.cs:38 | ExternalResearcherProjectRepository.ListByProjectAsync | tesisproject.shared.Entities.Core.ExternalResearcherProject.ProjectId | El tipo forma parte de un agregado o contrato genérico que todavía devuelve entidades legacy. | Migrar conjuntamente repositorio, interfaz, UoW y navegaciones del agregado. Revisar Project/Group/GroupMember, objetivos, documentos, presupuesto y catálogos; no mezclar instancias EF de ambos modelos. |
| P2 | tesisproject.backend/Repositories/Implementations/GroupMemberRepository.cs:16 | GroupMemberRepository.ExistsAsync | tesisproject.shared.Entities.Core.GroupMember.UserId | El tipo forma parte de un agregado o contrato genérico que todavía devuelve entidades legacy. | Migrar conjuntamente repositorio, interfaz, UoW y navegaciones del agregado. Revisar Project/Group/GroupMember, objetivos, documentos, presupuesto y catálogos; no mezclar instancias EF de ambos modelos. |
| P2 | tesisproject.backend/Repositories/Implementations/GroupRepository.cs:40 | GroupRepository.GetByProjectIdAsync | tesisproject.shared.Entities.Core.Project.ProjectId | El tipo forma parte de un agregado o contrato genérico que todavía devuelve entidades legacy. | Migrar conjuntamente repositorio, interfaz, UoW y navegaciones del agregado. Revisar Project/Group/GroupMember, objetivos, documentos, presupuesto y catálogos; no mezclar instancias EF de ambos modelos. |

Precisiones: ProductAuthor.UserId tiene cinco usos activos: ExistsForUserAsync, tres en SyncAuthorsAsync y uno en MapToDetailDTO. Visit.AcademicPeriodId aparece en ImportFromMatrixAsync (período externo) y CreateAsync de ProjectExtensionService (asigna null; bloqueado por el agregado legacy, no por conversión del valor). ArticleUserContext ya traduce IdentityUserId mediante GetByLocalIdAsync y devuelve IdUser: no debe sustituirse por el ID Identity. RegistrationMatrixService también usa la cadena ArticleParticipant y su borrado debe revisarse contra NoAction.

## C. Legacy que todavía queda

[legacy-search.csv](legacy-search.csv) conserva cada coincidencia textual final, incluyendo declaraciones y comentarios; [remaining-semantic.csv](remaining-semantic.csv) distingue MANUAL de KEEP en el backend activo. [initial-inventory.csv](initial-inventory.csv) registra la clasificación inicial SAFE/MANUAL/KEEP.

| Categoría | Coincidencias textuales |
| --- | --- |
| docs | 20 |
| legacy no usado (excluido del backend) | 163 |
| migrations | 1028 |
| runtime | 325 |
| shared protegido | 139 |
| tests | 13 |

Runtime incluye los contextos/modelos legacy, factories y componentes DW protegidos; KEEP no significa innecesario. ArticlesMigration, NewFolder/NewFolder1 y Data/Identity están excluidos mediante Compile Remove: son candidatos a revisión futura, sin eliminarse. El resto no se declara sin uso solamente por carecer de una referencia textual. Shared se informa aparte porque sus entidades y contratos permanecen protegidos. Docs no incluye los archivos de este propio informe, para evitar autorreferencias.

## Validación y métricas

| Métrica | Resultado |
| --- | --- |
| modified_code_files | 30 |
| migrated_semantic_references | 320 |
| migrated_namespace_records | 7 |
| pending_manual_semantic_references | 2072 |
| remaining_keep_semantic_references | 767 |
| final_build_errors | 0 |
| final_build_warnings | 39 |
| baseline_build_warnings | 39 |
| protected_files_unchanged | 1234 |

Se ejecutó `dotnet build TesisProject.sln --no-restore -t:Rebuild --verbosity quiet`: 0 errores, 39 advertencias; la compilación inicial tenía las mismas 39. No se inició la aplicación ni se ejecutaron pruebas de integración de runtime en esta fase. La validación de alcance comprobó los hashes de 1.234 archivos protegidos y que los cuerpos de negocio permanecen iguales salvo adaptaciones equivalentes de Set<T> y del constructor base.

Las referencias semánticas cuentan ocurrencias AST (incluyen tipos inferidos y genéricos), emparejadas por archivo/clase/método/tipo de nodo/símbolo antes y después; no equivalen a líneas editadas. Las importaciones se cuentan aparte. MANUAL incluye dependencias de agregados aún legacy. La búsqueda textual usa otra unidad y no se suma al total semántico. El análisis semántico abarca fuentes activas del backend; el textual complementa archivos excluidos, shared, tests, migraciones y docs.
