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
            public const string RequestRequired = "Debes enviar la información requerida.";
            public const string InvalidId = "El identificador enviado no es válido.";
            public const string NameRequired = "Debes ingresar un nombre.";
            public const string PersistenceConflict = "No se pudo guardar la información por un conflicto en los datos.";
            public const string OperationCanceled = "La operación fue cancelada.";
            public const string UnexpectedError = "Ocurrió un error inesperado.";
            public const string InvalidRequest = "La información enviada no es válida.";
            public const string NameAlreadyExists = "Ya existe un registro con ese nombre.";
        }
        public static class AppUser
        {
            public const string InvalidLocalUserId = "El identificador local del usuario no es válido.";
            public const string NotFound = "No se encontró el usuario de aplicación.";
        }

        public static class Budget
        {
            public const string NoneFound = "No se encontraron presupuestos.";
            public const string NotFound = "No se encontró el presupuesto.";
            public const string NoneFoundForProject = "No se encontraron presupuestos para este proyecto.";
            public const string CertificationExceedsInitialAmount = "El valor de la certificación no puede ser mayor al monto inicial.";
            public const string TotalCertifiedExceedsInitialAmount = "El total certificado no puede ser mayor al monto inicial del presupuesto.";
            public const string TotalExecutedExceedsCertifiedAmount = "El total ejecutado no puede ser mayor al total certificado.";
        }

        public static class BudgetTransaction
        {
            public const string NotFound = "No se encontró la transacción presupuestaria.";
            public const string InvalidStateForExecution = "Solo se pueden ejecutar transacciones de certificación.";
            public const string ExecutedAmountExceedsCertifiedAmount = "El valor ejecutado no puede ser mayor al valor certificado de esta transacción.";
            public const string ExecutedTotalExceedsBudgetCertifiedAmount = "El total ejecutado no puede ser mayor al total certificado del presupuesto.";
            public const string ExecutedTransactionsCannotBeCancelled = "Las transacciones ejecutadas no se pueden cancelar.";
            public const string AlreadyCancelled = "Esta transacción ya fue cancelada.";
            public const string CancelledTransactionsCannotBeUpdated = "Las transacciones canceladas no se pueden actualizar.";
            public const string ExecutedAmountRequired = "Debes ingresar el valor ejecutado para esta transacción.";
            public const string ExecutedAtRequired = "Debes indicar la fecha de ejecución para marcar esta transacción como ejecutada.";
        }

        public static class Project
        {
            public const string NotFound = "No se encontró el proyecto.";
            public const string NoneFound = "No se encontraron proyectos.";
            public const string NameAlreadyExists = "Ya existe un proyecto con ese nombre.";
            public const string NameAlreadyExistsInGroup = "Ya existe un proyecto con ese nombre dentro de este grupo.";
            public const string InvalidProjectData = "La información del proyecto no es válida.";
            public const string AtLeastOneGroupMemberRequired = "Debes agregar al menos un integrante al grupo.";
            public const string PrincipalCoordinatorRequired = "Debes seleccionar un coordinador principal.";
            public const string PrincipalCoordinatorExternalProfileNotFound = "No se encontró la información externa del coordinador principal.";
            public const string ExternalAcademicPeriodsNotAvailable = "No se pudo obtener la información de los períodos académicos externos.";
            public const string NoDistributivoForPrincipalCoordinator = "No se encontró distributivo para el coordinador principal en el sistema externo.";
            public const string StartDateRequired = "Debes ingresar la fecha de inicio del proyecto.";
            public const string FacultyCareerResolutionFailed = "No se pudo determinar la facultad o carrera del proyecto con la información externa disponible.";
            public const string InvalidFacultyId = "La facultad seleccionada no es válida.";
            public const string InvalidProjectTypeId = "El tipo de proyecto seleccionado no es válido.";
            public const string InvalidProjectStateId = "El estado del proyecto seleccionado no es válido.";
            public const string FacultyProjectCodeRequired = "Debes configurar el prefijo del código de proyecto de la facultad.";
            public const string GeneratedProjectCodeTooLong = "El código generado para el proyecto es demasiado largo.";
            public const string ErrorEnsuringAppUsers = "Ocurrió un problema al validar los usuarios de la aplicación.";
            public const string InconsistentAppUserMapping = "Se detectó una inconsistencia en la relación de usuarios de la aplicación.";
            public const string ErrorRetrievingDetail = "Ocurrió un problema al obtener el detalle del proyecto.";
            public const string CannotRetrieveFaculties = "No se pudo obtener la información de facultades desde el sistema externo.";
            public const string NoImportedProjectsFound = "No se encontraron proyectos importados en el resumen.";
            public const string CannotRetrieveExternalDirectory = "No se pudo obtener la información del directorio externo.";
        }
        public static class Auth
        {
            public const string UserNotAuthenticated = "Debes iniciar sesión para continuar.";
            public const string ActorUserNotFound = "No se encontró la información del usuario actual.";
            public const string InvalidCredentials = "Las credenciales ingresadas no son válidas.";
            public const string NoRefreshCookie = "No se encontró el token de actualización requerido.";
            public const string InvalidOrInactiveRefreshToken = "El token de actualización no es válido o ya no está activo.";
            public const string UserNotFound = "No se encontró el usuario asociado a la autenticación.";
        }
        public static class ExportTemplate
        {
            public const string NotFound = "No se encontró la plantilla.";
            public const string KeyRequired = "Debes ingresar una clave para la plantilla.";
            public const string KeyAlreadyExists = "Ya existe una plantilla con esa clave.";
        }

        public static class ExternalAcademics
        {
            public const string NoFacultiesFound = "No se encontraron facultades.";
            public const string FacultyNotFound = "No se encontró la facultad.";
            public const string NoProgramsFoundForFaculty = "No se encontraron programas para la facultad seleccionada.";
            public const string ProgramNotFound = "No se encontró el programa.";
            public const string EndpointNotFound = "No se encontró la información de facultades.";
        }

        public static class MatrixTemplateExport
        {
            public const string InvalidTemplateId = "Debes seleccionar una plantilla válida.";
            public const string TemplateNotRecovered = "No se pudo recuperar la plantilla seleccionada.";
            public const string TemplateInactive = "La plantilla seleccionada no está disponible.";
            public const string TemplateWithoutColumns = "La plantilla seleccionada no tiene columnas configuradas.";
            public const string IncludedColumnsNotInTemplate = "La selección contiene columnas que no pertenecen a la plantilla.";
            public const string MissingRequiredColumns = "Faltan columnas obligatorias de la plantilla en la selección.";
            public const string NoValidSelectedColumns = "No se encontraron columnas válidas para exportar.";
        }

        public static class ExternalDirectory
        {
            public const string NoProfilesFound = "No se encontraron perfiles externos.";
            public const string ConfigEndpointMissing = "La configuración del servicio externo no está completa.";
            public const string ConfigParamMissing = "Falta configurar un parámetro requerido del servicio externo.";
            public const string UnauthorizedExternalApi = "El servicio externo rechazó la autenticación.";
            public const string ForbiddenExternalApi = "No tienes permisos para acceder al servicio externo.";
            public const string AtLeastOneEmailRequired = "Debes enviar al menos un correo electrónico.";
            public const string AtLeastOneDocumentRequired = "Debes enviar al menos un documento.";
        }

        public static class ExternalDistributivos
        {
            public const string NoDistributivosFound = "No se encontraron distributivos.";
            public const string ConfigEndpointMissing = "La configuración del servicio externo no está completa.";
            public const string ConfigParamMissing = "Falta configurar un parámetro requerido del servicio externo.";
            public const string InvalidDistributivoId = "El distributivo seleccionado no es válido.";
            public const string DistributivoNotFound = "No se encontró el distributivo.";
            public const string UnauthorizedExternalApi = "El servicio externo rechazó la autenticación.";
            public const string ForbiddenExternalApi = "No tienes permisos para acceder al servicio externo.";
            public const string CedulasRequired = "Debes enviar al menos una cédula.";
            public const string NoDistributivosForCedulas = "No se encontraron distributivos para las cédulas enviadas.";
            public const string CorreosRequired = "Debes enviar al menos un correo.";
            public const string NoDistributivosForCorreos = "No se encontraron distributivos para los correos enviados.";
            public const string PeriodosRequired = "Debes enviar al menos un período.";
            public const string NoDistributivosForPeriodos = "No se encontraron distributivos para los períodos enviados.";
            public const string FacultadesRequired = "Debes enviar al menos una facultad.";
            public const string NoDistributivosForFacultades = "No se encontraron distributivos para las facultades enviadas.";
        }

        public static class ExternalPeriods
        {
            public const string NoExternalPeriodsFound = "No se encontraron períodos externos.";
            public const string AtLeastOnePeriodNameRequired = "Debes enviar al menos un nombre de período.";
            public const string ValidPeriodIdRequired = "Debes enviar un período válido.";
            public const string ExternalPeriodNotFound = "No se encontró el período externo.";
            public const string ConfigEndpointMissing = "La configuración del servicio externo no está completa.";
            public const string ConfigParamMissing = "Falta configurar un parámetro requerido del servicio externo.";
            public const string UnauthorizedExternalApi = "El servicio externo rechazó la autenticación.";
            public const string ForbiddenExternalApi = "No tienes permisos para acceder al servicio externo.";
        }

        public static class ExternalResearcherProject
        {
            public const string InvalidProjectId = "El proyecto seleccionado no es válido.";
            public const string InvalidId = "El identificador enviado no es válido.";
            public const string NotFound = "No se encontró el registro solicitado.";
            public const string ExternalResearcherIdRequired = "Debes seleccionar un investigador externo.";
            public const string ProjectIdRequired = "Debes seleccionar un proyecto.";
            public const string RoleRequired = "Debes indicar el rol.";
            public const string ExternalResearcherNotFound = "No se encontró el investigador externo.";
            public const string AlreadyAssigned = "Este investigador externo ya está asignado al proyecto.";
        }

        public static class ExternalResearcher
        {
            public const string InvalidId = "El identificador enviado no es válido.";
            public const string InvalidRequest = "La información enviada no es válida.";
            public const string FullNameRequired = "Debes ingresar el nombre completo.";
            public const string EmailRequired = "Debes ingresar el correo electrónico.";
            public const string EmailAlreadyExists = "Ya existe un investigador externo con ese correo.";
            public const string NotFound = "No se encontró el investigador externo.";
        }

        public static class Visit
        {
            public const string NotFoundById = "No se encontró la visita {0}.";
            public const string NotFound = "No se encontró la visita.";
            public const string ProjectIdRequired = "Debes seleccionar un proyecto.";
            public const string VisitStateIdRequired = "Debes seleccionar un estado de visita.";
            public const string VisitStateIdInvalid = "El estado de visita seleccionado no es válido.";
            public const string VisitIdRequired = "Debes enviar una visita válida.";
            public const string FinalVisitStateIdRequired = "Debes indicar el estado final de la visita.";
            public const string NoneFound = "No se encontraron visitas.";
            public const string NoneFoundForProject = "No se encontraron visitas para este proyecto.";
            public const string NoneFoundForState = "No se encontraron visitas para este estado.";
            public const string LoadAfterCreationFailed = "La visita se creó, pero no se pudo cargar su información.";
            public const string LoadAfterUpdateFailed = "La visita se actualizó, pero no se pudo cargar su información.";
            public const string LoadAfterFinalizeFailed = "La visita se finalizó, pero no se pudo cargar su información.";
            public const string BulkInvalidRequest = "La información enviada no es válida.";
            public const string BulkNoValidProjectIds = "Debes enviar al menos un proyecto válido.";
            public const string BulkScheduledDateRequired = "Debes indicar una fecha para planificar las visitas.";
            public const string BulkScheduleFailed = "No se pudieron planificar las visitas.";
        }

        public static class VisitIssue
        {
            public const string NotFoundById = "No se encontró la novedad de visita {0}.";
            public const string ReporterUserNotFound = "No se encontró el usuario reportante.";
        }

        public static class Catalog
        {
            public const string ItemNotFound = "No se encontró el elemento del catálogo.";
            public const string ItemLockedForModify = "Este elemento del catálogo está bloqueado y no se puede modificar.";
            public const string ItemLockedForDelete = "Este elemento del catálogo está bloqueado y no se puede eliminar.";
            public const string NameAlreadyExistsDetailed = "Ya existe un elemento con ese nombre. Revisa el catálogo para evitar duplicados.";
            public const string SimilarNameCandidatesFound = "El nombre ingresado se parece mucho a otros registros existentes. Revísalo antes de guardar. Coincidencias: {0}";
        }

        public static class ProductTypeDesign
        {
            public const string ProductTypeNotFound = "No se encontró el tipo de producto.";
            public const string RequestOrProductTypeRequired = "No se recibió la información necesaria para procesar el tipo de producto.";
            public const string ErrorSavingProductType = "Ocurrió un problema al guardar el tipo de producto.";
            public const string ErrorSavingProductAttribute = "Ocurrió un problema al guardar el atributo '{0}'.";
            public const string ProductTypeNameRequired = "Debes ingresar el nombre del tipo de producto.";
            public const string ProductTypeNameAlreadyExists = "Ya existe un tipo de producto con ese nombre.";
            public const string ProductTypeLocked = "Este tipo de producto está bloqueado y no se puede modificar.";
        }

        public static class Export
        {
            public const string FlatReportUnavailable = "No se pudo obtener el reporte plano de proyectos.";
            public const string NoProjectsInFlatReport = "No se encontraron proyectos en el reporte.";
            public const string NoColumnsDefined = "No se definieron columnas para la exportación.";
            public const string ExcelGenerationFailed = "Ocurrió un problema al generar el archivo Excel.";
        }

        public static class Convocation
        {
            public const string NotFound = "No se encontró la convocatoria.";
            public const string NoneFound = "No se encontraron convocatorias.";
            public const string ConvocationIdRequired = "Debes seleccionar una convocatoria.";
            public const string IdAndConvocationIdRequired = "Debes enviar un registro y una convocatoria válidos.";
            public const string ConvocationIdAndRuleIdRequired = "Debes enviar una convocatoria y una regla válidas.";
        }

        public static class ProjectMatrix
        {
            public const string FileStreamRequired = "Debes enviar un archivo válido.";
        }

        public static class DocumentRecognition
        {
            public const string FileEmpty = "El archivo está vacío.";
        }

        public static class ResearchCategory
        {
            public const string InvalidId = "El identificador enviado no es válido.";
            public const string NotFound = "No se encontró la categoría de investigación.";
            public const string TypeIdRequired = "Debes seleccionar un tipo de categoría de investigación.";
            public const string ParentNotFound = "No se encontró la categoría padre.";
            public const string ParentCannotBeSameAsId = "La categoría padre no puede ser la misma categoría actual.";
        }

        public static class Document
        {
            public const string NotFound = "No se encontró el documento.";
            public const string DocumentTypeIdRequired = "Debes seleccionar un tipo de documento.";
            public const string FileEmpty = "El archivo está vacío.";
            public const string FileNotFoundOnServer = "No se encontró el archivo en el servidor.";
            public const string FileReplaceFailed = "No se pudo reemplazar el archivo del documento.";
            public const string UploadFailed = "No se pudo cargar el documento.";
        }

        public static class Country
        {
            public const string NotFound = "No se encontró el país.";
            public const string IsoCodeInvalidLength = "El código ISO debe tener 2 caracteres.";
            public const string IsoAlpha3InvalidLength = "El código ISO Alpha 3 debe tener 3 caracteres.";
        }

        public static class ObjectiveActivityUser
        {
            public const string ObjectiveActivityIdRequired = "Debes seleccionar una actividad objetivo.";
            public const string UserIdRequired = "Debes seleccionar un usuario.";
            public const string VisitIdRequired = "Debes seleccionar una visita.";
            public const string ObjectiveActivityNotFound = "No se encontró la actividad objetivo.";
            public const string AssignmentNotFound = "No se encontró la asignación.";
            public const string AlreadyAssigned = "Ya existe una asignación para esta actividad, usuario y visita.";
        }

        public static class Product
        {
            public const string NotFound = "No se encontró el producto.";
            public const string NoneFound = "No se encontraron productos.";
            public const string NoneFoundForProject = "No se encontraron productos para este proyecto.";
            public const string ProjectIdRequired = "Debes seleccionar un proyecto.";
            public const string TitleRequired = "Debes ingresar un título.";
            public const string ProductTypeIdRequired = "Debes seleccionar un tipo de producto.";
            public const string ProductTypeNotFound = "No se encontró el tipo de producto.";
            public const string LoadAfterCreationFailed = "El producto se creó, pero no se pudo cargar su información.";
            public const string LoadAfterUpdateFailed = "El producto se actualizó, pero no se pudo cargar su información.";
            public const string RequiredAttributeValueMissing = "Falta un valor obligatorio en uno de los atributos del producto (AttributeDefinitionId={0}).";
            public const string AttributeDefinitionDoesNotBelongToProductType = "Uno de los atributos enviados no corresponde al tipo de producto seleccionado (AttributeDefinitionId={0}).";
            public const string InvalidAttributeValue = "Uno de los valores ingresados no es válido (AttributeDefinitionId={0}: {1}).";
            public const string ExpectedNumericValue = "Debes ingresar un valor numérico válido.";
            public const string ExpectedValidDate = "Debes ingresar una fecha válida.";
            public const string ExpectedAbsoluteUrl = "Debes ingresar un enlace válido.";
            public const string TextTooLong = "El texto ingresado es demasiado largo (máximo 4000 caracteres).";
        }
    }
}