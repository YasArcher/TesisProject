namespace tesisproject.frontend.Models.Workflow
{
    public static class ReviewStagingContexts
    {
        private static readonly IReadOnlyDictionary<string, ReviewStagingContextDefinition> Definitions =
            new Dictionary<string, ReviewStagingContextDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["uodide"] = new(
                    Key: "uodide",
                    PageTitle: "Staging UODIDE",
                    PageHeading: "Staging de validación UODIDE",
                    PageEyebrow: "Validación institucional",
                    PageSummary: "Espacio de corrección y validación inicial para envíos de autores antes de pasar a Área Técnica.",
                    WorkspaceTitle: "Revisión y corrección del envío",
                    WorkspaceDescription: "Esta vista usa el mismo motor de staging y validación, pero está enfocada solo en los envíos de autores que pasan por UODIDE.",
                    AudienceLabel: "UODIDE",
                    AutoApproveAfterValidation: true,
                    ApprovalComment: "[Para siguiente revisor] El lote fue validado por UODIDE y quedó listo para revisión del Área Técnica.",
                    SuccessTitle: "Envío validado y derivado",
                    SuccessMessageFormat: "El lote {0} quedó validado sin observaciones y fue enviado automáticamente al Área Técnica.",
                    LoadingMessage: "Cargando staging de revisión UODIDE y preparando el lote seleccionado...",
                    LoadErrorPrefix: "No pude cargar el staging UODIDE",
                    BatchPendingHint: "Pendiente de validación UODIDE",
                    BatchErrorHint: "Revisar observaciones UODIDE",
                    BatchReadyHint: "Listo para derivar",
                    BatchProcessedHint: "Procesado",
                    ValidationPendingMessage: "Voy a validar este envío para normalizar referencias, detectar observaciones por fila y confirmar la revisión institucional antes de derivarlo.",
                    ValidationErrorPrefix: "No pude validar el envío en revisión UODIDE",
                    EmptyGuidanceTitle: "Abre un envío UODIDE",
                    EmptyGuidance: "Abre un envío desde la bandeja UODIDE para revisar observaciones y continuar con la validación institucional.",
                    NoRowsGuidance: "Este envío todavía no tiene información lista para revisión institucional.",
                    EmptySelectionTitle: "Sin envío seleccionado",
                    EmptySelectionMessage: "Abre este staging desde la bandeja UODIDE para cargar el lote que necesita corrección.",
                    PendingGuidanceTitle: "Validar revisión institucional",
                    PendingStatusLabel: "En revisión UODIDE",
                    PendingGuidance: "Revisa el contenido del envío y valida nuevamente para confirmar que no quedan observaciones antes de enviarlo al siguiente revisor.",
                    InProgressGuidance: "Revisa el contenido del envío y valida nuevamente para confirmar que no quedan observaciones antes de enviarlo al siguiente revisor.",
                    HasErrorsGuidanceTitle: "Corregir observaciones",
                    HasErrorsGuidance: "Conviene corregir primero las observaciones detectadas antes de derivar el envío a Área Técnica.",
                    ValidatedGuidanceTitle: "Derivar a Área Técnica",
                    ValidatedGuidance: "El envío ya superó la validación UODIDE y está listo para pasar a revisión técnica.",
                    ReadyGuidanceTitle: "Derivar a Área Técnica",
                    CompletedGuidanceTitle: "Proceso completado",
                    EmptyStageLabel: "Sin envío",
                    LoadedStageLabel: "Cargado",
                    ProcessedStageLabel: "Procesado",
                    ValidatedStageLabel: "Validado por UODIDE",
                    ErrorStageLabel: "Con observaciones UODIDE",
                    OpeningStatusLabel: "Abriendo envío",
                    OpeningStatusDetail: "Estoy cargando la información del envío seleccionado para que puedas revisarlo sin bloquear la pantalla.",
                    ValidatingStatusLabel: "Validando envío",
                    ValidatingStatusDetail: "La validación está revisando el envío, normalizando valores y registrando observaciones por fila.",
                    ProcessingStatusLabel: "Procesando envío",
                    ProcessingStatusDetail: "El sistema está integrando el envío aprobado dentro de la base principal.",
                    OpeningOverlayTitle: "Abriendo envío",
                    OpeningOverlayMessage: "Estoy cargando el envío seleccionado y preparando su vista de revisión.",
                    ValidatingOverlayTitle: "Validando envío",
                    ValidatingOverlayMessage: "Estoy ejecutando la revisión institucional del envío, la normalización y las validaciones por fila.",
                    ProcessingOverlayTitle: "Procesando envío",
                    ProcessingOverlayMessage: "Estoy integrando el envío aprobado dentro de la base principal.",
                    ValidatedStatusLabel: "Validado",
                    ErrorStatusLabel: "Con observaciones",
                    ReadyStatusLabel: "Listo para enviar"),
                ["area-tecnica"] = new(
                    Key: "area-tecnica",
                    PageTitle: "Staging Área Técnica",
                    PageHeading: "Staging de validación Área Técnica",
                    PageEyebrow: "Validación técnica final",
                    PageSummary: "Espacio de revisión final y habilitación de procesamiento para envíos ya validados por UODIDE.",
                    WorkspaceTitle: "Revisión y cierre técnico del envío",
                    WorkspaceDescription: "Esta vista usa el motor de staging para la validación técnica final, corrección de observaciones y habilitación del procesamiento definitivo.",
                    AudienceLabel: "Área Técnica",
                    AutoApproveAfterValidation: true,
                    ApprovalComment: "[Nota interna] La validación técnica final fue completada y el envío quedó habilitado para procesamiento.",
                    SuccessTitle: "Validación final aprobada",
                    SuccessMessageFormat: "El lote {0} quedó validado sin observaciones y ya está listo para procesamiento final.",
                    LoadingMessage: "Cargando staging de revisión técnica y preparando el lote seleccionado...",
                    LoadErrorPrefix: "No pude cargar el staging de Área Técnica",
                    BatchPendingHint: "Pendiente de validación técnica",
                    BatchErrorHint: "Corregir observaciones técnicas",
                    BatchReadyHint: "Listo para procesar",
                    BatchProcessedHint: "Procesado",
                    ValidationPendingMessage: "Voy a validar este envío para confirmar la revisión técnica final y dejarlo listo para procesamiento.",
                    ValidationErrorPrefix: "No pude validar el envío en revisión técnica",
                    EmptyGuidanceTitle: "Abre un envío técnico",
                    EmptyGuidance: "Abre un envío desde la bandeja de Área Técnica para completar la revisión final y habilitar el procesamiento.",
                    NoRowsGuidance: "Este envío todavía no tiene información lista para revisión técnica final.",
                    EmptySelectionTitle: "Sin envío técnico seleccionado",
                    EmptySelectionMessage: "Abre este staging desde la bandeja de Área Técnica para cargar el lote que necesita validación final.",
                    PendingGuidanceTitle: "Validar revisión final",
                    PendingStatusLabel: "En revisión técnica",
                    PendingGuidance: "Revisa el contenido validado por UODIDE y confirma que no quedan observaciones antes de habilitar el procesamiento.",
                    InProgressGuidance: "Revisa el contenido validado por UODIDE y confirma que no quedan observaciones antes de habilitar el procesamiento.",
                    HasErrorsGuidanceTitle: "Corregir observaciones finales",
                    HasErrorsGuidance: "Conviene corregir primero las observaciones técnicas finales antes de cerrar el workflow.",
                    ValidatedGuidanceTitle: "Habilitar procesamiento",
                    ValidatedGuidance: "El envío ya superó la validación técnica final y quedó listo para procesamiento definitivo.",
                    ReadyGuidanceTitle: "Procesar envío",
                    CompletedGuidanceTitle: "Proceso completado",
                    EmptyStageLabel: "Sin envío",
                    LoadedStageLabel: "Cargado",
                    ProcessedStageLabel: "Procesado",
                    ValidatedStageLabel: "Validación final aprobada",
                    ErrorStageLabel: "Con observaciones técnicas",
                    OpeningStatusLabel: "Abriendo envío",
                    OpeningStatusDetail: "Estoy cargando la información del envío técnico seleccionado para que puedas revisarlo sin bloquear la pantalla.",
                    ValidatingStatusLabel: "Validando envío técnico",
                    ValidatingStatusDetail: "La validación está revisando el envío técnico, normalizando valores y registrando observaciones por fila.",
                    ProcessingStatusLabel: "Procesando envío",
                    ProcessingStatusDetail: "El sistema está integrando el envío validado dentro de la base principal.",
                    OpeningOverlayTitle: "Abriendo envío técnico",
                    OpeningOverlayMessage: "Estoy cargando el envío técnico seleccionado y preparando su vista de revisión.",
                    ValidatingOverlayTitle: "Validando revisión final",
                    ValidatingOverlayMessage: "Estoy ejecutando la revisión técnica final del envío, la normalización y las validaciones por fila.",
                    ProcessingOverlayTitle: "Procesando envío",
                    ProcessingOverlayMessage: "Estoy integrando el envío validado dentro de la base principal.",
                    ValidatedStatusLabel: "Validación final lista",
                    ErrorStatusLabel: "Con observaciones",
                    ReadyStatusLabel: "Listo para procesar")
            };

        public static string? ResolveContextKey(string? explicitContextKey, string? workflowStageKey)
        {
            var explicitKey = Normalize(explicitContextKey);
            if (!string.IsNullOrWhiteSpace(explicitKey))
            {
                return explicitKey;
            }

            return Normalize(workflowStageKey) switch
            {
                "uodide-validation" => "uodide",
                "technical-validation" => "area-tecnica",
                _ => null
            };
        }

        public static ReviewStagingContextDefinition? Resolve(string? contextKey)
        {
            var normalized = Normalize(contextKey);
            return normalized is not null && Definitions.TryGetValue(normalized, out var definition)
                ? definition
                : null;
        }

        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "uodide" => "uodide",
                "area-tecnica" => "area-tecnica",
                _ => normalized
            };
        }
    }
}
