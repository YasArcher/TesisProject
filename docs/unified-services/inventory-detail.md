# Inventario fuente de Services Projects/core y fronteras

Incluye helpers privados en los commits transitivos. Los DTOs externos conservan su namespace de transporte.

## AppConfigurationRepository

Fuente: `tesisproject.backend/Services/Implementations/AppConfigurationRepository.cs`. Interface: `IAppConfigurationRepository` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: 

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: 

## AppUserService

Fuente: `tesisproject.backend/Services/Implementations/AppUserService.cs`. Interface: `IAppUserService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `UserManager<IdentityUser<int>> _userManager`, `IAppUserRepository _appUsers`, `IUnitOfWork _uow`, `IUserRoleService _userRoles`

Repositorios:  | IAppUserRepository

Entidades legacy importadas: tesisproject.shared.Entities.Auth

DTOs/contratos: InvalidRequest, RegisterRequest

## BudgetService

Fuente: `tesisproject.backend/Services/Implementations/BudgetService.cs`. Interface: `IBudgetService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ICurrentUserService _currentUser`

Repositorios: AppUsers; Budgets; Projects

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddCertificationRequestDTO, BudgetDTO, BudgetListItemDTO, BudgetTransactionDTO, CreateBudgetRequestDTO, ExecuteDevengadoRequestDTO, MapToDTO, Request, UpdateBudgetRequestDTO, UpdateBudgetTransactionRequestDTO

## CatalogCrudService

Fuente: `tesisproject.backend/Services/Implementations/CatalogCrudService.cs`. Interface: `ICatalogCrudService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ICatalogRepository<TCatalog> _repo`

Repositorios:  | ICatalogRepository<TCatalog>

Entidades legacy importadas: tesisproject.shared.Entities.Base

DTOs/contratos: AddCatalogRequestDTO, CatalogDetailDTO, CatalogListItemDTO, Request, Response, UpdateCatalogRequestDTO

## CatalogQueryService

Fuente: `tesisproject.backend/Services/Implementations/CatalogQueryService.cs`. Interface: `ICatalogQueryService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IServiceProvider _sp`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.Base

DTOs/contratos: KeyValueItemDTO

## ConvocationService

Fuente: `tesisproject.backend/Services/Implementations/ConvocationService.cs`. Interface: `IConvocationService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: Convocations

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: ConvocationCreateRequestDTO, ConvocationDetailResponseDTO, ConvocationListItemResponseDTO, ConvocationRuleCreateRequestDTO, ConvocationRuleResponseDTO, ConvocationRuleUpdateRequestDTO, ConvocationUpdateRequestDTO, InvalidRequest, MapRuleToDTO, MapToDetailDTO, MapToListDTO, Request, Response

## CountryService

Fuente: `tesisproject.backend/Services/Implementations/CountryService.cs`. Interface: `ICountryService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: Countries

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: AddCountryRequestDTO, CountryDetailDTO, CountryListItemDTO, InvalidRequest, KeyValueItemDTO, Request, Response, UpdateCountryRequestDTO

## CurrentUserService

Fuente: `tesisproject.backend/Services/Implementations/CurrentUserService.cs`. Interface: `ICurrentUserService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IHttpContextAccessor _httpContextAccessor`

Repositorios: 

Entidades legacy importadas: 

DTOs/contratos: 

## DocumentRecognitionService

Fuente: `tesisproject.backend/Services/Implementations/DocumentRecognitionService.cs`. Interface: `IDocumentRecognitionService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `ILogger<IDocumentRecognitionService> _logger`, `IExternalDirectoryClient _externalDirectory`, `IMemberRoleTypeService _memberRoleTypeService`, `ICatalogCrudService<ProjectType> _projectTypeService`, `IExternalPeriodsClient _periods`, `IExternalDistributivosService _distributivos`, `DocumentRecognitionOptions _opt`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs, tesisproject.shared.Entities.External

DTOs/contratos: Response

## DocumentService

Fuente: `tesisproject.backend/Services/Implementations/DocumentService.cs`. Interface: `IDocumentService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ICurrentUserService _currentUser`, `string _storageRootFullPath`

Repositorios: AppUsers; Documents

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: DocumentResponseDTO, ReplaceDocumentFileRequestDTO, Request, Response, UpdateDocumentRequestDTO, UploadDocumentRequestDTO

## ExportTemplateExcelService

Fuente: `tesisproject.backend/Services/Implementations/ExportTemplateExcelService.cs`. Interface: `IExportTemplateExcelService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IProjectFlatReportService _flatService`, `ILogger<ExportTemplateExcelService> _logger`

Repositorios: 

Entidades legacy importadas: 

DTOs/contratos: ExportColumnDTO, ExportRequestDTO, InvalidRequest, ProjectFlatReportDTO, Request, Response

## ExportTemplateService

Fuente: `tesisproject.backend/Services/Implementations/ExportTemplateService.cs`. Interface: `IExportTemplateService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ILogger<ExportTemplateService> _logger`

Repositorios: ExportFields; ExportTemplateColumns; ExportTemplates

Entidades legacy importadas: tesisproject.shared.Entities.Export

DTOs/contratos: ExportFieldListItemDTO, ExportTemplateColumnDTO, ExportTemplateColumnUpsertDTO, ExportTemplateCreateRequestDTO, ExportTemplateDetailDTO, ExportTemplateListItemDTO, ExportTemplateUpdateRequestDTO, MapToDetailDTO

## ExternalAcademicsService

Fuente: `tesisproject.backend/Services/Implementations/ExternalAcademicsService.cs`. Interface: `IExternalAcademicsService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `HttpClient _http`, `ILogger<ExternalAcademicsService> _logger`, `ExternalApiOptions _opts`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.External

DTOs/contratos: ExternalFacultyDTO, ExternalProgramDTO, KeyValueItemDTO

## ExternalDirectoryClient

Fuente: `tesisproject.backend/Services/Implementations/ExternalDirectoryClient.cs`. Interface: `IExternalDirectoryClient` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `HttpClient _http`, `ILogger<ExternalDirectoryClient> _logger`, `ExternalApiOptions _opts`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.External

DTOs/contratos: 

## ExternalDistributivosService

Fuente: `tesisproject.backend/Services/Implementations/ExternalDistributivosService.cs`. Interface: `IExternalDistributivosService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `HttpClient _http`, `ILogger<ExternalDistributivosService> _logger`, `ExternalApiOptions _opts`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.External

DTOs/contratos: 

## ExternalPeriodsClient

Fuente: `tesisproject.backend/Services/Implementations/ExternalPeriodsClient.cs`. Interface: `IExternalPeriodsClient` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `HttpClient _http`, `ILogger<ExternalPeriodsClient> _logger`, `ExternalApiOptions _opts`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.External

DTOs/contratos: 

## ExternalResearcherProjectService

Fuente: `tesisproject.backend/Services/Implementations/ExternalResearcherProjectService.cs`. Interface: `IExternalResearcherProjectService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ICurrentUserService _currentUser`

Repositorios: AppUsers; ExternalResearcherProjects; ExternalResearchers; Projects

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: ExternalResearcherProjectCreateRequestDTO, ExternalResearcherProjectDetailDTO, ExternalResearcherProjectListItemDTO, ExternalResearcherProjectUpdateRequestDTO, InvalidRequest, Request, Response, ToDetailDTO, ToListItemDTO

## ExternalResearcherService

Fuente: `tesisproject.backend/Services/Implementations/ExternalResearcherService.cs`. Interface: `IExternalResearcherService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ExternalResearchers

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: ExternalResearcherCreateRequestDTO, ExternalResearcherDetailDTO, ExternalResearcherListItemDTO, ExternalResearcherUpdateRequestDTO, InvalidRequest, KeyValueItemDTO, Request, Response, ToDetailDTO, ToListItemDTO

## FacultyScopeService

Fuente: `tesisproject.backend/Services/Implementations/FacultyScopeService.cs`. Interface: `IFacultyScopeService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `IAppUserService _appUsers`, `ICurrentUserService _currentUser`

Repositorios: AppUsers; FacultyScopeFaculties; FacultyScopes; UserFacultyScopeAssignments

Entidades legacy importadas: tesisproject.shared.Entities.Auth, tesisproject.shared.Entities.Core

DTOs/contratos: AssignFacultyScopeUserRequestDTO, CreateFacultyScopeRequestDTO, FacultyScopeFacultyItemDTO, FacultyScopeResponseDTO, InvalidRequest, RegisterRequest, Request, Response, SetFacultyScopeFacultiesRequestDTO, UpdateFacultyScopeRequestDTO

## GroupService

Fuente: `tesisproject.backend/Services/Implementations/GroupService.cs`. Interface: `IGroupService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `IExternalDirectoryClient _directory`, `ILogger<GroupService> _logger`, `IAppUserService _appUsers`, `IExternalPeriodsClient _periods`, `IExternalDistributivosService _distributivos`

Repositorios: AppUsers; AspNetUsers; GroupMembers; Groups; MemberRoleTypeRepository; ProjectExtensions; Projects

Entidades legacy importadas: tesisproject.shared.Entities.Core, tesisproject.shared.Entities.External

DTOs/contratos: AddGroupMemberRequestDTO, AddGroupRequestDTO, DTO, GroupMemberResponseDTO, GroupResponseDTO, InvalidRequest, ProjectMemberReportRowDTO, ProjectMembersReportDTO, ProjectMembersReportSectionDTO, RegisterRequest, Request, ResolvedUserProfileDTO, Response, ToGroupResponseDTO, UpdateGroupRequestDTO

## IndexingSourceService

Fuente: `tesisproject.backend/Services/Implementations/IndexingSourceService.cs`. Interface: `IIndexingSourceService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: IndexingSources

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: IndexingSourceCreateRequestDTO, IndexingSourceListItemDTO, IndexingSourceUpdateRequestDTO, InvalidRequest, KeyValueItemDTO, Request, Response, ToListItemDTO

## InstitutionService

Fuente: `tesisproject.backend/Services/Implementations/InstitutionService.cs`. Interface: `IInstitutionService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: Countries; Institutions

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: AddInstitutionRequestDTO, InstitutionDetailDTO, InstitutionListItemDTO, InvalidRequest, KeyValueItemDTO, Request, Response, ToDetailDTO, ToListItemDTO, UpdateInstitutionRequestDTO

## MatrixExcelExportService

Fuente: `tesisproject.backend/Services/Implementations/MatrixExcelExportService.cs`. Interface: `IMatrixExcelExportService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IProjectFlatReportService _flatService`, `IResearchCategoryService _categoryService`, `ILogger<MatrixExcelExportService> _logger`

Repositorios: 

Entidades legacy importadas: 

DTOs/contratos: ProjectFlatReportDTO, ResearchCategoryTreeItemDTO, Response

## MatrixTemplateExcelExportService

Fuente: `tesisproject.backend/Services/Implementations/MatrixTemplateExcelExportService.cs`. Interface: `IMatrixTemplateExcelExportService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IProjectFlatReportService _flatService`, `IResearchCategoryService _categoryService`, `IExportTemplateService _templateService`, `ILogger<MatrixTemplateExcelExportService> _logger`

Repositorios: 

Entidades legacy importadas: 

DTOs/contratos: ExportByTemplateRequestDTO, ExportTemplateColumnDTO, ExportTemplateDetailDTO, ProjectFlatReportDTO, Request, ResearchCategoryTreeItemDTO, Response

## MemberRoleTypeService

Fuente: `tesisproject.backend/Services/Implementations/MemberRoleTypeService.cs`. Interface: `IMemberRoleTypeService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: MemberRoleTypeRepository

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: AddMemberRoleTypeRequestDTO, InvalidRequest, KeyValueItemDTO, MemberRoleTypeDetailDTO, MemberRoleTypeListItemDTO, Request, Response, UpdateMemberRoleTypeRequestDTO

## ObjectiveActivityService

Fuente: `tesisproject.backend/Services/Implementations/ObjectiveActivityService.cs`. Interface: `IObjectiveActivityService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ObjectiveActivities; ProjectObjectives; Projects; VisitObjectiveActivityProgresses; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddObjectiveActivityRequestDTO, DTO, InvalidRequest, ObjectiveActivityDetailDTO, ObjectiveActivityListItemDTO, Request, Response, UpdateObjectiveActivityRequestDTO

## ObjectiveActivityUserService

Fuente: `tesisproject.backend/Services/Implementations/ObjectiveActivityUserService.cs`. Interface: `IObjectiveActivityUserService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ObjectiveActivities; ObjectiveActivityUsers; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AssignObjectiveActivityUserRequestDTO, ObjectiveActivityUserDTO, Request, Response, UpdateObjectiveActivityUserRequestDTO

## ProductAttributeDefinitionService

Fuente: `tesisproject.backend/Services/Implementations/ProductAttributeDefinitionService.cs`. Interface: `IProductAttributeDefinitionService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ProductAttributeDefinitions

Entidades legacy importadas: tesisproject.shared.Entities.Core.Products

DTOs/contratos: AddProductAttributeDefinitionRequestDTO, DTO, InvalidRequest, ProductAttributeDefinitionDetailDTO, ProductAttributeDefinitionListItemDTO, Request, Response, UpdateProductAttributeDefinitionRequestDTO

## ProductAttributeService

Fuente: `tesisproject.backend/Services/Implementations/ProductAttributeService.cs`. Interface: `IProductAttributeService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ProductAttributes

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: AddProductAttributeRequestDTO, InvalidRequest, ProductAttributeDetailDTO, ProductAttributeListItemDTO, Request, Response, UpdateProductAttributeRequestDTO

## ProductService

Fuente: `tesisproject.backend/Services/Implementations/ProductService.cs`. Interface: `IProductService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ProductAttributeDefinitions; ProductAuthors; ProductTypes; ProductValues; Products

Entidades legacy importadas: tesisproject.shared.Entities.Core.Products

DTOs/contratos: InvalidRequest, MapToDetailDTO, ProductAuthorResponseDTO, ProductCreateRequestDTO, ProductDetailResponseDTO, ProductListItemResponseDTO, ProductUpdateRequestDTO, ProductValueResponseDTO, ProductValueUpsertDTO, Request, Response

## ProductTypeDesignService

Fuente: `tesisproject.backend/Services/Implementations/ProductTypeDesignService.cs`. Interface: `IProductTypeDesignService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `IProductAttributeService _productAttributeService`

Repositorios: ProductAttributeDefinitions; ProductAttributes; ProductTypes

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs, tesisproject.shared.Entities.Core.Products

DTOs/contratos: AddProductAttributeRequestDTO, CatalogDetailDTO, ProductAttributeDefinitionDetailDTO, ProductAttributeDefinitionUpsertDTO, ProductAttributeDetailDTO, ProductAttributeUpsertDTO, ProductTypeDesignDetailDTO, ProductTypeUpsertDTO, Request, Response, SaveProductTypeDesignRequestDTO, UpdateProductAttributeRequestDTO

## ProjectExtensionService

Fuente: `tesisproject.backend/Services/Implementations/ProjectExtensionService.cs`. Interface: `IProjectExtensionService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ProjectExtensionTypes; ProjectExtensions; Projects; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddProjectExtensionRequestDTO, InvalidRequest, MapToListDTO, ProjectExtensionListResponseDTO, Request, Response, UpdateProjectExtensionRequestDTO

## ProjectFlatReportService

Fuente: `tesisproject.backend/Services/Implementations/ProjectFlatReportService.cs`. Interface: `IProjectFlatReportService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `AppDbContext _db`, `IExternalAcademicsService _externalAcademics`, `IExternalDirectoryClient _externalDirectory`, `ILogger<ProjectFlatReportService> _logger`

Repositorios: 

Entidades legacy importadas: tesisproject.shared.Entities.Core.Products, tesisproject.shared.Entities.External

DTOs/contratos: DTO, ExternalFacultyDTO, ExternalProfileDTO, ProductAttributeValueDTO, ProjectBudgetReportDTO, ProjectExternalResearcherReportDTO, ProjectFlatReportDTO, ProjectMemberReportDTO, ProjectObjectiveReportDTO, ProjectProductReportDTO, ProjectResearchCategoryReportDTO, Response

## ProjectMatrixService

Fuente: `tesisproject.backend/Services/Implementations/ProjectMatrixService.cs`. Interface: `IProjectMatrixService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IProjectService _projectService`, `ILogger<ProjectMatrixService> _logger`

Repositorios: 

Entidades legacy importadas: 

DTOs/contratos: ImportedProjectDTO, MatrixRangeDTO, ProjectDocumentDTO, ProjectExtensionDTO, ProjectMatrixColumnMapDTO, ProjectMatrixUploadErrorDTO, ProjectMatrixUploadSummaryDTO, ProjectVisitPeriodDTO, Response

## ProjectObjectiveService

Fuente: `tesisproject.backend/Services/Implementations/ProjectObjectiveService.cs`. Interface: `IProjectObjectiveService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ObjectiveTypes; ProjectObjectives; VisitObjectiveActivityProgresses

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddProjectObjectiveRequestDTO, DTO, InvalidRequest, ObjectiveActivityListItemDTO, ProjectObjectiveDetailDTO, ProjectObjectiveListItemDTO, ProjectObjectiveWithActivitiesDTO, Request, Response, UpdateProjectObjectiveRequestDTO

## ProjectResearchCategoryService

Fuente: `tesisproject.backend/Services/Implementations/ProjectResearchCategoryService.cs`. Interface: `IProjectResearchCategoryService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ProjectResearchCategories

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddProjectResearchCategoryRequestDTO, InvalidRequest, MapToDetailDTO, MapToListItemDTO, ProjectResearchCategoryDetailDTO, ProjectResearchCategoryListItemDTO, Request, Response, UpdateProjectResearchCategoryRequestDTO

## ProjectService

Fuente: `tesisproject.backend/Services/Implementations/ProjectService.cs`. Interface: `IProjectService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`, `ICurrentUserService _currentUser`, `IExternalPeriodsClient _externalPeriods`, `IAppUserService _appUsers`, `IExternalAcademicsService _externalAcademics`, `IResearchCategoryService _researchCategoryService`, `ILogger<ProjectService> _logger`, `IExternalDirectoryClient _externalDirectory`, `IExternalDistributivosService _externalDistributivosRaw`

Repositorios: AppUsers; Budgets; Convocations; DocumentTypes; Documents; ExternalResearcherProjects; GroupMembers; Groups; ObjectiveActivities; ProjectDocuments; ProjectExtensions; ProjectObjectives; ProjectResearchCategories; ProjectStates; Projects; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs, tesisproject.shared.Entities.Core, tesisproject.shared.Entities.External

DTOs/contratos: AddProjectFullRequestDTO, AddProjectRequestDTO, ExternalFacultyDTO, ImportedProjectDTO, MapToDTO, ProjectBudgetDetailDTO, ProjectDetailResponseDTO, ProjectDocumentRefDTO, ProjectListResponseDTO, ProjectMatrixUploadSummaryDTO, ProjectObjectiveListItemDTO, RegisterRequest, Request, ResearchCategoryListItemDTO, Response, UpdateProjectRequestDTO

## ProjectsFiltersService

Fuente: `tesisproject.backend/Services/Implementations/ProjectsFiltersService.cs`. Interface: `IProjectsFiltersService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `ICatalogQueryService _catalogs`, `IExternalAcademicsService _extTypes`, `ICatalogRepository<ResearchCategoryType> _researchTypes`

Repositorios:  | ICatalogRepository<ResearchCategoryType>

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: KeyValueItemDTO, ProjectsFilterBootstrapDTO, ResearchCategoryFilterTypeDTO, ResearchCategoryItemDTO

## ResearchCategoryService

Fuente: `tesisproject.backend/Services/Implementations/ResearchCategoryService.cs`. Interface: `IResearchCategoryService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ResearchCategories

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: AddResearchCategoryRequestDTO, Request, ResearchCategoryDetailDTO, ResearchCategoryListItemDTO, ResearchCategoryTreeItemDTO, Response, UpdateResearchCategoryRequestDTO

## ResearchCategoryTypeService

Fuente: `tesisproject.backend/Services/Implementations/ResearchCategoryTypeService.cs`. Interface: `IResearchCategoryTypeService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ResearchCategoryTypes

Entidades legacy importadas: tesisproject.shared.Entities.Catalogs

DTOs/contratos: Request, ResearchCategoryTypeCreateRequestDTO, ResearchCategoryTypeDetailDTO, ResearchCategoryTypeListItemDTO, ResearchCategoryTypeUpdateRequestDTO, Response

## VisitIssueService

Fuente: `tesisproject.backend/Services/Implementations/VisitIssueService.cs`. Interface: `IVisitIssueService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IVisitIssueRepository _issueRepo`, `IVisitRepository _visitRepo`, `IUnitOfWork _uow`, `ICurrentUserService _currentUser`

Repositorios: AppUsers | IVisitIssueRepository, IVisitRepository

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: Request, Response, ToResponse, VisitIssueCreateRequestDTO, VisitIssueResponseDTO, VisitIssueUpdateRequestDTO

## VisitObjectiveActivityProgressService

Fuente: `tesisproject.backend/Services/Implementations/VisitObjectiveActivityProgressService.cs`. Interface: `IVisitObjectiveActivityProgressService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: ObjectiveActivities; ProjectObjectives; VisitObjectiveActivityProgresses; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: InvalidRequest, Request, Response, UpsertSingleVisitObjectiveActivityProgressRequestDTO, VisitObjectiveActivityProgressSingleResponseDTO

## VisitService

Fuente: `tesisproject.backend/Services/Implementations/VisitService.cs`. Interface: `IVisitService` (AppConfigurationRepository es un repository mal ubicado, no un service).

Dependencias declaradas: `IUnitOfWork _uow`

Repositorios: Projects; VisitStates; Visits

Entidades legacy importadas: tesisproject.shared.Entities.Core

DTOs/contratos: AddVisitRequestDTO, BulkInvalidRequest, BulkScheduleVisitsRequestDTO, FinalizeVisitRequestDTO, MapToDetailDTO, MapToListDTO, Request, Response, UpdateVisitRequestDTO, VisitDetailResponseDTO, VisitListResponseDTO, VisitPlannedForExecutionListDTO
