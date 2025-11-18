using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Algorithms.Response
{
    public class ResolutionInfo
    {
        public string? ResolutionCode { get; set; }
        public string? ResolutionHeaderDate { get; set; }
        public string? MeetingDate { get; set; }
        public string? Duration { get; set; }
        public string? Budget { get; set; }
        public string? ExecutionStartDate { get; set; }
        public string? MainDecisionVerb { get; set; }
    }

    public class ResearcherInfo
    {
        public string Role { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }

    // ============================================
    //   DIDE FORM - OBJECTIVES & ACTIVITIES
    // ============================================

    /// <summary>
    /// Representa una actividad vinculada a un objetivo,
    /// extraída de las tablas de metodología del proyecto.
    /// </summary>
    public class DideObjectiveActivityInfo
    {
        /// <summary>
        /// Descripción de la actividad tal como aparece en el documento
        /// (columna "Actividad").
        /// </summary>
        public string ActivityText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa un objetivo del proyecto (general o específico)
    /// extraído del FORMATO DIDE-PRY-002-2020.
    /// </summary>
    public class DideObjectiveInfo
    {
        /// <summary>
        /// Tipo del objetivo tal como viene en el documento.
        /// Ejemplo típico: "General", "Específico".
        /// Luego se mapeará a ObjectiveTypeId en la capa de servicio.
        /// </summary>
        public string ObjectiveType { get; set; } = string.Empty;
        public int? objetiveNumber { get; set; }

        /// <summary>
        /// Descripción textual del objetivo.
        /// Se mapea a ProjectObjective.Objetive.
        /// </summary>
        public string ObjectiveText { get; set; } = string.Empty;

        /// <summary>
        /// Actividades asociadas a este objetivo,
        /// según las tablas de "ACTIVIDADES DEL PROYECTO - METODOLOGÍA".
        /// </summary>
        public List<DideObjectiveActivityInfo> Activities { get; set; } = new();
    }

    // ============================================
    //   ROOT DTO - FORMATO DIDE COMPLETO
    // ============================================

    /// <summary>
    /// Resultado completo del reconocimiento del FORMATO DIDE-PRY-002-2020:
    /// datos del proyecto, investigadores, objetivos y actividades.
    /// </summary>
    public class DideProjectFormInfo
    {
        /// <summary>
        /// Nombre / título del proyecto
        /// tal como aparece en el formulario DIDE.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;


        /// <summary>
        /// Todas las líneas de investigación detectadas en el formulario.
        /// </summary>
        public List<string> ResearchLines { get; set; } = new();

        /// <summary>
        /// Tipo de investigación (Aplicada, Experimental, etc.)
        /// tal como aparece en el documento.
        /// </summary>
        public string ResearchType { get; set; } = string.Empty;

        /// <summary>
        /// Investigadores normalizados (Coordinador Principal,
        /// Subrogante, Investigadores N, mapeados a tus roles reales).
        /// </summary>
        public List<ResearcherInfo> Researchers { get; set; } = new();

        /// <summary>
        /// Objetivos del proyecto (principal/general y secundarios/específicos)
        /// con sus actividades asociadas.
        /// </summary>
        public List<DideObjectiveInfo> Objectives { get; set; } = new();
    }
}