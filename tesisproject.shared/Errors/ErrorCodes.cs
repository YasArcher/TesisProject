using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Errors
{
    public static class ErrorCodes
    {
        public static class Common
        {
            public const string UnexpectedError = "COMMON_UNEXPECTED_ERROR";
            public const string PersistenceConflict = "COMMON_PERSISTENCE_CONFLICT";
            public const string InvalidId = "COMMON_INVALID_ID";
            public const string NameRequired = "COMMON_NAME_REQUIRED";
            public const string NameAlreadyExists = "COMMON_NAME_ALREADY_EXISTS";
            public const string OperationCanceled = "COMMON_OPERATION_CANCELED";
            public const string InvalidRequest = "COMMON_INVALID_REQUEST";
        }

        public static class Auth
        {
            public const string UserNotAuthenticated = "AUTH_USER_NOT_AUTHENTICATED";
            public const string ActorUserNotFound = "AUTH_ACTOR_USER_NOT_FOUND";
        }

        public static class Project
        {
            public const string NotFound = "PROJECT_NOT_FOUND";
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
    }
}