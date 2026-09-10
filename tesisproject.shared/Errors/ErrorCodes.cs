using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Errors
{
    public static class ErrorCodes
    {
        public static class CatalogSynchronization
        {
            public const string ProviderUnavailable = "CATALOG_SYNC_PROVIDER_UNAVAILABLE";
            public const string InvalidResponse = "CATALOG_SYNC_INVALID_RESPONSE";
            public const string DuplicateExternalId = "CATALOG_SYNC_DUPLICATE_EXTERNAL_ID";
            public const string PersistenceFailed = "CATALOG_SYNC_PERSISTENCE_FAILED";
            public const string PendingChanges = "CATALOG_SYNC_PENDING_CHANGES";
        }

        public static class IdentityProvisioning
        {
            public const string MappingConflict = "IDENTITY_MAPPING_CONFLICT";
            public const string PendingChanges = "IDENTITY_PENDING_DOMAIN_CHANGES";
            public const string InvalidRole = "IDENTITY_ROLE_INVALID";
            public const string OperationFailed = "IDENTITY_PROVISIONING_FAILED";
        }

        public static class AcademicReferences
        {
            public const string FacultyNotSynchronized = "FACULTY_NOT_SYNCHRONIZED";
            public const string AcademicTermNotSynchronized = "ACADEMIC_TERM_NOT_SYNCHRONIZED";
        }

        public static class Author
        {
            public const string NotFound = "AUTHOR_NOT_FOUND";
            public const string ExactlyOneSource = "AUTHOR_EXACTLY_ONE_SOURCE";
            public const string SourceAlreadyAssigned = "AUTHOR_SOURCE_ALREADY_ASSIGNED";
            public const string OrcidTooLong = "AUTHOR_ORCID_TOO_LONG";
            public const string OrcidAlreadyExists = "AUTHOR_ORCID_ALREADY_EXISTS";
            public const string InUse = "AUTHOR_IN_USE";
        }
        public static class Common
        {
            public const string UnexpectedError = "COMMON_UNEXPECTED_ERROR";
            public const string PersistenceConflict = "COMMON_PERSISTENCE_CONFLICT";
            public const string NotFound = "COMMON_NOT_FOUND";
            public const string InvalidId = "COMMON_INVALID_ID";
            public const string NameRequired = "COMMON_NAME_REQUIRED";
            public const string NameAlreadyExists = "COMMON_NAME_ALREADY_EXISTS";
            public const string OperationCanceled = "COMMON_OPERATION_CANCELED";
            public const string InvalidRequest = "COMMON_INVALID_REQUEST";
            public const string RequestRequired = "COMMON_REQUEST_REQUIRED";
        }
        public static class AppUser
        {
            public const string InvalidLocalUserId = "APP_USER_INVALID_LOCAL_USER_ID";
            public const string NotFound = "APP_USER_NOT_FOUND";
        }
        public static class Auth
        {
            public const string UserNotAuthenticated = "AUTH_USER_NOT_AUTHENTICATED";
            public const string ActorUserNotFound = "AUTH_ACTOR_USER_NOT_FOUND";
            public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
            public const string NoRefreshCookie = "AUTH_NO_REFRESH_COOKIE";
            public const string InvalidOrInactiveRefreshToken = "AUTH_INVALID_OR_INACTIVE_REFRESH_TOKEN";
            public const string UserNotFound = "AUTH_USER_NOT_FOUND";
        }
        public static class Project
        {
            public const string NotFound = "PROJECT_NOT_FOUND";
            public const string NoneFound = "PROJECT_NONE_FOUND";
            public const string NameAlreadyExists = "PROJECT_NAME_ALREADY_EXISTS";
            public const string NameAlreadyExistsInGroup = "PROJECT_NAME_ALREADY_EXISTS_IN_GROUP";
            public const string InvalidProjectData = "PROJECT_INVALID_PROJECT_DATA";
            public const string AtLeastOneGroupMemberRequired = "PROJECT_AT_LEAST_ONE_GROUP_MEMBER_REQUIRED";
            public const string PrincipalCoordinatorRequired = "PROJECT_PRINCIPAL_COORDINATOR_REQUIRED";
            public const string PrincipalCoordinatorExternalProfileNotFound = "PROJECT_PRINCIPAL_COORDINATOR_EXTERNAL_PROFILE_NOT_FOUND";
            public const string ExternalAcademicPeriodsNotAvailable = "PROJECT_EXTERNAL_ACADEMIC_PERIODS_NOT_AVAILABLE";
            public const string NoDistributivoForPrincipalCoordinator = "PROJECT_NO_DISTRIBUTIVO_FOR_PRINCIPAL_COORDINATOR";
            public const string StartDateRequired = "PROJECT_START_DATE_REQUIRED";
            public const string FacultyCareerResolutionFailed = "PROJECT_FACULTY_CAREER_RESOLUTION_FAILED";
            public const string InvalidFacultyId = "PROJECT_INVALID_FACULTY_ID";
            public const string InvalidProjectTypeId = "PROJECT_INVALID_PROJECT_TYPE_ID";
            public const string InvalidProjectStateId = "PROJECT_INVALID_PROJECT_STATE_ID";
            public const string FacultyProjectCodeRequired = "PROJECT_FACULTY_PROJECT_CODE_REQUIRED";
            public const string GeneratedProjectCodeTooLong = "PROJECT_GENERATED_PROJECT_CODE_TOO_LONG";
            public const string ErrorEnsuringAppUsers = "PROJECT_ERROR_ENSURING_APP_USERS";
            public const string InconsistentAppUserMapping = "PROJECT_INCONSISTENT_APP_USER_MAPPING";
            public const string ErrorRetrievingDetail = "PROJECT_ERROR_RETRIEVING_DETAIL";
            public const string CannotRetrieveFaculties = "PROJECT_CANNOT_RETRIEVE_FACULTIES";
            public const string NoImportedProjectsFound = "PROJECT_NO_IMPORTED_PROJECTS_FOUND";
            public const string CannotRetrieveExternalDirectory = "PROJECT_CANNOT_RETRIEVE_EXTERNAL_DIRECTORY";
        }

        public static class Budget
        {
            public const string NotFound = "BUDGET_NOT_FOUND";
            public const string NoneFound = "BUDGET_NONE_FOUND";
            public const string NoneFoundForProject = "BUDGET_NONE_FOUND_FOR_PROJECT";
            public const string CertificationExceedsInitialAmount = "BUDGET_CERTIFICATION_EXCEEDS_INITIAL_AMOUNT";
            public const string TotalCertifiedExceedsInitialAmount = "BUDGET_TOTAL_CERTIFIED_EXCEEDS_INITIAL_AMOUNT";
            public const string TotalExecutedExceedsCertifiedAmount = "BUDGET_TOTAL_EXECUTED_EXCEEDS_CERTIFIED_AMOUNT";
        }

        public static class BudgetTransaction
        {
            public const string NotFound = "BUDGET_TRANSACTION_NOT_FOUND";
            public const string InvalidStateForExecution = "BUDGET_TRANSACTION_INVALID_STATE_FOR_EXECUTION";
            public const string ExecutedAmountExceedsCertifiedAmount = "BUDGET_TRANSACTION_EXECUTED_AMOUNT_EXCEEDS_CERTIFIED_AMOUNT";
            public const string ExecutedTotalExceedsBudgetCertifiedAmount = "BUDGET_TRANSACTION_EXECUTED_TOTAL_EXCEEDS_CERTIFIED_AMOUNT";
            public const string ExecutedTransactionsCannotBeCancelled = "BUDGET_TRANSACTION_EXECUTED_CANNOT_BE_CANCELLED";
            public const string AlreadyCancelled = "BUDGET_TRANSACTION_ALREADY_CANCELLED";
            public const string CancelledTransactionsCannotBeUpdated = "BUDGET_TRANSACTION_CANCELLED_CANNOT_BE_UPDATED";
            public const string ExecutedAmountRequired = "BUDGET_TRANSACTION_EXECUTED_AMOUNT_REQUIRED";
            public const string ExecutedAtRequired = "BUDGET_TRANSACTION_EXECUTED_AT_REQUIRED";
        }

        public static class Catalog
        {
            public const string ItemNotFound = "CATALOG_ITEM_NOT_FOUND";
            public const string ItemLockedForModify = "CATALOG_ITEM_LOCKED_FOR_MODIFY";
            public const string ItemLockedForDelete = "CATALOG_ITEM_LOCKED_FOR_DELETE";
            public const string SimilarNameCandidatesFound = "CATALOG_SIMILAR_NAME_CANDIDATES_FOUND";
        }
        public static class CatalogQuery
        {
            public const string NoItemsFound = "CATALOG_QUERY_NO_ITEMS_FOUND";
        }
        public static class Convocation
        {
            public const string NotFound = "CONVOCATION_NOT_FOUND";
            public const string NoneFound = "CONVOCATION_NONE_FOUND";
        }
        public static class Country
        {
            public const string NotFound = "COUNTRY_NOT_FOUND";
            public const string IsoCodeInvalidLength = "COUNTRY_ISO_CODE_INVALID_LENGTH";
            public const string IsoAlpha3InvalidLength = "COUNTRY_ISO_ALPHA3_INVALID_LENGTH";
        }
        public static class DocumentRecognition
        {
            public const string FileEmpty = "DOCUMENT_RECOGNITION_FILE_EMPTY";
        }
        public static class Document
        {
            public const string NotFound = "DOCUMENT_NOT_FOUND";
            public const string DocumentTypeIdRequired = "DOCUMENT_DOCUMENT_TYPE_ID_REQUIRED";
            public const string FileEmpty = "DOCUMENT_FILE_EMPTY";
            public const string FileNotFoundOnServer = "DOCUMENT_FILE_NOT_FOUND_ON_SERVER";
            public const string FileReplaceFailed = "DOCUMENT_FILE_REPLACE_FAILED";
            public const string UploadFailed = "DOCUMENT_UPLOAD_FAILED";
        }
        public static class Export
        {
            public const string NoColumnsDefined = "EXPORT_NO_COLUMNS_DEFINED";
            public const string FlatReportUnavailable = "EXPORT_FLAT_REPORT_UNAVAILABLE";
            public const string NoProjectsInFlatReport = "EXPORT_NO_PROJECTS_IN_FLAT_REPORT";
            public const string ExcelGenerationFailed = "EXPORT_EXCEL_GENERATION_FAILED";
        }
        public static class MatrixTemplateExport
        {
            public const string InvalidTemplateId = "MATRIX_TEMPLATE_EXPORT_INVALID_TEMPLATE_ID";
            public const string TemplateNotRecovered = "MATRIX_TEMPLATE_EXPORT_TEMPLATE_NOT_RECOVERED";
            public const string TemplateInactive = "MATRIX_TEMPLATE_EXPORT_TEMPLATE_INACTIVE";
            public const string TemplateWithoutColumns = "MATRIX_TEMPLATE_EXPORT_TEMPLATE_WITHOUT_COLUMNS";
            public const string IncludedColumnsNotInTemplate = "MATRIX_TEMPLATE_EXPORT_INCLUDED_COLUMNS_NOT_IN_TEMPLATE";
            public const string MissingRequiredColumns = "MATRIX_TEMPLATE_EXPORT_MISSING_REQUIRED_COLUMNS";
            public const string NoValidSelectedColumns = "MATRIX_TEMPLATE_EXPORT_NO_VALID_SELECTED_COLUMNS";
        }
        public static class ExportTemplate
        {
            public const string NotFound = "EXPORT_TEMPLATE_NOT_FOUND";
            public const string KeyRequired = "EXPORT_TEMPLATE_KEY_REQUIRED";
            public const string KeyAlreadyExists = "EXPORT_TEMPLATE_KEY_ALREADY_EXISTS";
        }
        public static class ExternalAcademics
        {
            public const string NoFacultiesFound = "EXTERNAL_ACADEMICS_NO_FACULTIES_FOUND";
            public const string FacultyNotFound = "EXTERNAL_ACADEMICS_FACULTY_NOT_FOUND";
            public const string NoProgramsFoundForFaculty = "EXTERNAL_ACADEMICS_NO_PROGRAMS_FOUND_FOR_FACULTY";
            public const string ProgramNotFound = "EXTERNAL_ACADEMICS_PROGRAM_NOT_FOUND";
            public const string EndpointNotFound = "EXTERNAL_ACADEMICS_ENDPOINT_NOT_FOUND";
        }
        public static class ExternalDirectory
        {
            public const string NoProfilesFound = "EXTERNAL_DIRECTORY_NO_PROFILES_FOUND";
            public const string ConfigEndpointMissing = "EXTERNAL_DIRECTORY_CONFIG_ENDPOINT_MISSING";
            public const string ConfigParamMissing = "EXTERNAL_DIRECTORY_CONFIG_PARAM_MISSING";
            public const string UnauthorizedExternalApi = "EXTERNAL_DIRECTORY_UNAUTHORIZED_EXTERNAL_API";
            public const string ForbiddenExternalApi = "EXTERNAL_DIRECTORY_FORBIDDEN_EXTERNAL_API";
            public const string AtLeastOneEmailRequired = "EXTERNAL_DIRECTORY_AT_LEAST_ONE_EMAIL_REQUIRED";
            public const string AtLeastOneDocumentRequired = "EXTERNAL_DIRECTORY_AT_LEAST_ONE_DOCUMENT_REQUIRED";
        }
        public static class ExternalDistributivos
        {
            public const string NoDistributivosFound = "EXTERNAL_DISTRIBUTIVOS_NO_DISTRIBUTIVOS_FOUND";
            public const string ConfigEndpointMissing = "EXTERNAL_DISTRIBUTIVOS_CONFIG_ENDPOINT_MISSING";
            public const string ConfigParamMissing = "EXTERNAL_DISTRIBUTIVOS_CONFIG_PARAM_MISSING";
            public const string InvalidDistributivoId = "EXTERNAL_DISTRIBUTIVOS_INVALID_DISTRIBUTIVO_ID";
            public const string DistributivoNotFound = "EXTERNAL_DISTRIBUTIVOS_DISTRIBUTIVO_NOT_FOUND";
            public const string UnauthorizedExternalApi = "EXTERNAL_DISTRIBUTIVOS_UNAUTHORIZED_EXTERNAL_API";
            public const string ForbiddenExternalApi = "EXTERNAL_DISTRIBUTIVOS_FORBIDDEN_EXTERNAL_API";
            public const string CedulasRequired = "EXTERNAL_DISTRIBUTIVOS_CEDULAS_REQUIRED";
            public const string NoDistributivosForCedulas = "EXTERNAL_DISTRIBUTIVOS_NO_DISTRIBUTIVOS_FOR_CEDULAS";
            public const string CorreosRequired = "EXTERNAL_DISTRIBUTIVOS_CORREOS_REQUIRED";
            public const string NoDistributivosForCorreos = "EXTERNAL_DISTRIBUTIVOS_NO_DISTRIBUTIVOS_FOR_CORREOS";
            public const string PeriodosRequired = "EXTERNAL_DISTRIBUTIVOS_PERIODOS_REQUIRED";
            public const string NoDistributivosForPeriodos = "EXTERNAL_DISTRIBUTIVOS_NO_DISTRIBUTIVOS_FOR_PERIODOS";
            public const string FacultadesRequired = "EXTERNAL_DISTRIBUTIVOS_FACULTADES_REQUIRED";
            public const string NoDistributivosForFacultades = "EXTERNAL_DISTRIBUTIVOS_NO_DISTRIBUTIVOS_FOR_FACULTADES";
        }
        public static class ExternalPeriods
        {
            public const string NoExternalPeriodsFound = "EXTERNAL_PERIODS_NO_EXTERNAL_PERIODS_FOUND";
            public const string AtLeastOnePeriodNameRequired = "EXTERNAL_PERIODS_AT_LEAST_ONE_PERIOD_NAME_REQUIRED";
            public const string ValidPeriodIdRequired = "EXTERNAL_PERIODS_VALID_PERIOD_ID_REQUIRED";
            public const string ExternalPeriodNotFound = "EXTERNAL_PERIODS_EXTERNAL_PERIOD_NOT_FOUND";
            public const string ConfigEndpointMissing = "EXTERNAL_PERIODS_CONFIG_ENDPOINT_MISSING";
            public const string ConfigParamMissing = "EXTERNAL_PERIODS_CONFIG_PARAM_MISSING";
            public const string UnauthorizedExternalApi = "EXTERNAL_PERIODS_UNAUTHORIZED_EXTERNAL_API";
            public const string ForbiddenExternalApi = "EXTERNAL_PERIODS_FORBIDDEN_EXTERNAL_API";
        }
        public static class ExternalResearcherProject
        {
            public const string InvalidProjectId = "EXTERNAL_RESEARCHER_PROJECT_INVALID_PROJECT_ID";
            public const string InvalidId = "EXTERNAL_RESEARCHER_PROJECT_INVALID_ID";
            public const string NotFound = "EXTERNAL_RESEARCHER_PROJECT_NOT_FOUND";
            public const string ExternalResearcherIdRequired = "EXTERNAL_RESEARCHER_PROJECT_EXTERNAL_RESEARCHER_ID_REQUIRED";
            public const string ProjectIdRequired = "EXTERNAL_RESEARCHER_PROJECT_PROJECT_ID_REQUIRED";
            public const string RoleRequired = "EXTERNAL_RESEARCHER_PROJECT_ROLE_REQUIRED";
            public const string ExternalResearcherNotFound = "EXTERNAL_RESEARCHER_PROJECT_EXTERNAL_RESEARCHER_NOT_FOUND";
            public const string AlreadyAssigned = "EXTERNAL_RESEARCHER_PROJECT_ALREADY_ASSIGNED";
        }

        public static class ExternalResearcher
        {
            public const string InvalidId = "EXTERNAL_RESEARCHER_INVALID_ID";
            public const string InvalidRequest = "EXTERNAL_RESEARCHER_INVALID_REQUEST";
            public const string FullNameRequired = "EXTERNAL_RESEARCHER_FULL_NAME_REQUIRED";
            public const string EmailRequired = "EXTERNAL_RESEARCHER_EMAIL_REQUIRED";
            public const string EmailAlreadyExists = "EXTERNAL_RESEARCHER_EMAIL_ALREADY_EXISTS";
            public const string NotFound = "EXTERNAL_RESEARCHER_NOT_FOUND";
        }

        public static class Visit
        {
            public const string NotFound = "VISIT_NOT_FOUND";
            public const string ProjectIdRequired = "VISIT_PROJECT_ID_REQUIRED";
            public const string VisitStateIdRequired = "VISIT_VISIT_STATE_ID_REQUIRED";
            public const string VisitStateIdInvalid = "VISIT_VISIT_STATE_ID_INVALID";
            public const string VisitIdRequired = "VISIT_VISIT_ID_REQUIRED";
            public const string FinalVisitStateIdRequired = "VISIT_FINAL_VISIT_STATE_ID_REQUIRED";
            public const string NoneFound = "VISIT_NONE_FOUND";
            public const string NoneFoundForProject = "VISIT_NONE_FOUND_FOR_PROJECT";
            public const string NoneFoundForState = "VISIT_NONE_FOUND_FOR_STATE";
            public const string LoadAfterCreationFailed = "VISIT_LOAD_AFTER_CREATION_FAILED";
            public const string LoadAfterUpdateFailed = "VISIT_LOAD_AFTER_UPDATE_FAILED";
            public const string LoadAfterFinalizeFailed = "VISIT_LOAD_AFTER_FINALIZE_FAILED";
            public const string BulkInvalidRequest = "VISIT_BULK_INVALID_REQUEST";
            public const string BulkNoValidProjectIds = "VISIT_BULK_NO_VALID_PROJECT_IDS";
            public const string BulkScheduledDateRequired = "VISIT_BULK_SCHEDULED_DATE_REQUIRED";
            public const string BulkScheduleFailed = "VISIT_BULK_SCHEDULE_FAILED";
        }

        public static class VisitIssue
        {
            public const string NotFound = "VISIT_ISSUE_NOT_FOUND";
            public const string ReporterUserNotFound = "VISIT_ISSUE_REPORTER_USER_NOT_FOUND";
        }
        public static class ProductTypeDesign
        {
            public const string ProductTypeNotFound = "PRODUCT_TYPE_DESIGN_PRODUCT_TYPE_NOT_FOUND";
            public const string RequestOrProductTypeRequired = "PRODUCT_TYPE_DESIGN_REQUEST_OR_PRODUCT_TYPE_REQUIRED";
            public const string ProductTypeNameRequired = "PRODUCT_TYPE_DESIGN_PRODUCT_TYPE_NAME_REQUIRED";
            public const string ProductTypeNameAlreadyExists = "PRODUCT_TYPE_DESIGN_PRODUCT_TYPE_NAME_ALREADY_EXISTS";
            public const string ProductTypeLocked = "PRODUCT_TYPE_DESIGN_PRODUCT_TYPE_LOCKED";
            public const string ErrorSavingProductType = "PRODUCT_TYPE_DESIGN_ERROR_SAVING_PRODUCT_TYPE";
            public const string ErrorSavingProductAttribute = "PRODUCT_TYPE_DESIGN_ERROR_SAVING_PRODUCT_ATTRIBUTE";
        }
        public static class ProjectMatrix
        {
            public const string FileStreamRequired = "PROJECT_MATRIX_FILE_STREAM_REQUIRED";
        }
        public static class ResearchCategory
        {
            public const string InvalidId = "RESEARCH_CATEGORY_INVALID_ID";
            public const string NotFound = "RESEARCH_CATEGORY_NOT_FOUND";
            public const string TypeIdRequired = "RESEARCH_CATEGORY_TYPE_ID_REQUIRED";
            public const string ParentNotFound = "RESEARCH_CATEGORY_PARENT_NOT_FOUND";
            public const string ParentCannotBeSameAsId = "RESEARCH_CATEGORY_PARENT_CANNOT_BE_SAME_AS_ID";
        }
        public static class ObjectiveActivityUser
        {
            public const string ObjectiveActivityIdRequired = "OBJECTIVE_ACTIVITY_USER_OBJECTIVE_ACTIVITY_ID_REQUIRED";
            public const string UserIdRequired = "OBJECTIVE_ACTIVITY_USER_USER_ID_REQUIRED";
            public const string VisitIdRequired = "OBJECTIVE_ACTIVITY_USER_VISIT_ID_REQUIRED";
            public const string ObjectiveActivityNotFound = "OBJECTIVE_ACTIVITY_USER_OBJECTIVE_ACTIVITY_NOT_FOUND";
            public const string AssignmentNotFound = "OBJECTIVE_ACTIVITY_USER_ASSIGNMENT_NOT_FOUND";
            public const string AlreadyAssigned = "OBJECTIVE_ACTIVITY_USER_ALREADY_ASSIGNED";
        }
        public static class Product
        {
            public const string NotFound = "PRODUCT_NOT_FOUND";
            public const string NoneFound = "PRODUCT_NONE_FOUND";
            public const string NoneFoundForProject = "PRODUCT_NONE_FOUND_FOR_PROJECT";
            public const string ProjectIdRequired = "PRODUCT_PROJECT_ID_REQUIRED";
            public const string TitleRequired = "PRODUCT_TITLE_REQUIRED";
            public const string ProductTypeIdRequired = "PRODUCT_PRODUCT_TYPE_ID_REQUIRED";
            public const string ProductTypeNotFound = "PRODUCT_PRODUCT_TYPE_NOT_FOUND";
            public const string LoadAfterCreationFailed = "PRODUCT_LOAD_AFTER_CREATION_FAILED";
            public const string LoadAfterUpdateFailed = "PRODUCT_LOAD_AFTER_UPDATE_FAILED";
            public const string RequiredAttributeValueMissing = "PRODUCT_REQUIRED_ATTRIBUTE_VALUE_MISSING";
            public const string AttributeDefinitionMismatch = "PRODUCT_ATTRIBUTE_DEFINITION_MISMATCH";
            public const string InvalidAttributeValue = "PRODUCT_INVALID_ATTRIBUTE_VALUE";
        }
    }
}
