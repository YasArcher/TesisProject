using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Errors
{
    public static class ErrorMessages
    {
        public static class Common
        {
            public const string RequestRequired = "Request is required.";
            public const string InvalidId = "Id is required.";
            public const string NameRequired = "Name is required.";
            public const string PersistenceConflict = "Persistence conflict.";
            public const string OperationCanceled = "Operation was canceled.";
            public const string UnexpectedError = "Unexpected error.";
            public const string InvalidRequest = "Invalid request.";
            public const string NameAlreadyExists = "Name already exists.";
        }
        public static class Project
        {
            public const string NotFound = "Project not found.";
        }
        public static class Auth
        {
            public const string UserNotAuthenticated = "User not authenticated.";
            public const string ActorUserNotFound = "Current user was not found.";
        }
        public static class ExportTemplate
        {
            public const string NotFound = "Template not found.";
            public const string KeyRequired = "Key is required.";
            public const string KeyAlreadyExists = "Template key already exists.";
        }
        public static class ExternalAcademics
        {
            public const string NoFacultiesFound = "No faculties found.";
            public const string FacultyNotFound = "Faculty not found.";
            public const string NoProgramsFoundForFaculty = "No programs found for the specified faculty.";
            public const string ProgramNotFound = "Program not found.";
            public const string EndpointNotFound = "Faculties not found.";
        }
        public static class ExternalDirectory
        {
            public const string NoProfilesFound = "No external profiles found.";
            public const string ConfigEndpointMissing = "External API misconfiguration: UsersEndpoint is missing.";
            public const string ConfigParamMissing = "External API misconfiguration: query parameter name is missing.";
            public const string UnauthorizedExternalApi = "Unauthorized external API.";
            public const string ForbiddenExternalApi = "Forbidden external API.";
            public const string AtLeastOneEmailRequired = "At least one email is required.";
            public const string AtLeastOneDocumentRequired = "At least one document is required.";
        }
        public static class ExternalDistributivos
        {
            public const string NoDistributivosFound = "No distributivos found.";
            public const string ConfigEndpointMissing = "External API misconfiguration: DistributivosEndpoint is missing.";
            public const string ConfigParamMissing = "External API misconfiguration: query parameter name is missing.";
            public const string InvalidDistributivoId = "Invalid distributivoId.";
            public const string DistributivoNotFound = "Distributivo not found.";
            public const string UnauthorizedExternalApi = "Unauthorized external API.";
            public const string ForbiddenExternalApi = "Forbidden external API.";
            public const string CedulasRequired = "Cedulas are required.";
            public const string NoDistributivosForCedulas = "No distributivos found for the specified cedulas.";
            public const string CorreosRequired = "Correos are required.";
            public const string NoDistributivosForCorreos = "No distributivos found for the specified correos.";
            public const string PeriodosRequired = "Periodos are required.";
            public const string NoDistributivosForPeriodos = "No distributivos found for the specified periodos.";
            public const string FacultadesRequired = "Facultades are required.";
            public const string NoDistributivosForFacultades = "No distributivos found for the specified facultades.";
        }
        public static class ExternalPeriods
        {
            public const string NoExternalPeriodsFound = "No external periods found.";
            public const string AtLeastOnePeriodNameRequired = "At least one period name is required.";
            public const string ValidPeriodIdRequired = "A valid period id is required.";
            public const string ExternalPeriodNotFound = "External period not found.";
            public const string ConfigEndpointMissing = "External API misconfiguration: PeriodsEndpoint is missing.";
            public const string ConfigParamMissing = "External API misconfiguration: query parameter name is missing.";
            public const string UnauthorizedExternalApi = "Unauthorized external API.";
            public const string ForbiddenExternalApi = "Forbidden external API.";
        }
        public static class ExternalResearcherProject
        {
            public const string InvalidProjectId = "Invalid project id.";
            public const string InvalidId = "Invalid id.";
            public const string NotFound = "Record not found.";
            public const string ExternalResearcherIdRequired = "ExternalResearcherId is required.";
            public const string ProjectIdRequired = "ProjectId is required.";
            public const string RoleRequired = "Role is required.";
            public const string ExternalResearcherNotFound = "External researcher not found.";
            public const string AlreadyAssigned = "External researcher is already assigned to this project.";
        }
    }
}