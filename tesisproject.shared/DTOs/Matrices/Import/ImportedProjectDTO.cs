using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Matrices.Import
{
    public class ImportedProjectDTO
    {
        // Natural key
        public string? CallCode { get; set; }          // CONVOCATORIA
        public string? ProjectCode { get; set; }       // CODIGO
        public int? Number { get; set; }               // Nro.
        public decimal? ExecutionProgress { get; set; }
        // Básico
        public string? ProjectName { get; set; }       // PROYECTO
        public string? Faculty { get; set; }           // Facultad
        public string? State { get; set; }             // ESTADO
        public int? TermMonths { get; set; }           // PLAZO
        public DateTime? StartDate { get; set; }       // FECHA DE INICIO
        public DateTime? EstimatedEndDate { get; set; }// FECHA DE FINALIZACION ESTIMADA
        public bool HasExternalParticipants { get; set; }
        // Financiero
        public decimal? AssignedValue { get; set; }    // VALOR ASIGNADO
        public decimal? ExecutedValue { get; set; }    // VALOR EJECUTADO
        public decimal? RemainingValue { get; set; }   // VALOR POR EJECUTAR
        public decimal? ExecutionPercentage { get; set; } // % EJECUTADO PRESUPUESTARIA

        // Documentos principales
        public List<ProjectDocumentDTO> Documents { get; set; } = new();

        // Prórrogas (dinámico, n prórrogas)
        public List<ProjectExtensionDTO> Extensions { get; set; } = new();

        // Visitas / avances por periodo (dinámico, n columnas de rango de fecha)
        public List<ProjectVisitPeriodDTO> VisitPeriods { get; set; } = new();

        // Categorías
        public string? Domain { get; set; }
        public string? ResearchLine { get; set; }      // LINEA DE INVESTIGACION
        public string? BroadField { get; set; }        // CAMPO AMPLIO
        public string? SpecificField { get; set; }     // CAMPO ESPECIFICO
        public string? DetailedField { get; set; }     // CAMPO DETALLADO
        public string? TerritorialScope { get; set; }  // ALCANCE TERRITORIAL
        public string? ExpectedImpact { get; set; }    // IMPACTO ESPERADO

        // Objetivo
        public string? GeneralObjective { get; set; }  // OBJETIVO GENERAL
        public List<string> Coordinators { get; set; } = new();
        public List<string> AlternateCoordinators { get; set; } = new();

        // opcional para auditoría
        public List<string> CoordinatorDiscardedTokens { get; set; } = new();
        public List<string> AlternateCoordinatorDiscardedTokens { get; set; } = new();

    }

    public class ProjectExtensionDTO
    {
        public int Index { get; set; }                 // 1,2,3,... (primera, segunda, etc.)
        public string? ResolutionCode { get; set; }    // RESOLUCION PRIMERA PRORROGA
        public DateTime? NewEndDate { get; set; }      // FECHA DE FINALIZACION (de esa prórroga)
    }

    public class ProjectVisitPeriodDTO
    {
        public string PeriodKey { get; set; } = default!; // Header normalizado (clave estable)
        public string PeriodLabel { get; set; } = default!; // Header original completo
        public string? RawValue { get; set; }           // Texto original de la celda
        public bool HasReport { get; set; }            // true si hay algo registrado
    }

    public class ProjectDocumentDTO
    {
        public string? DocumentType { get; set; }      // "MEMORANDO_INFORME_FINAL", "APROBACION_HCU", etc.
        public string? Code { get; set; }              // Ej: 2342-CU-P-2013
        public DateTime? Date { get; set; }            // Fecha del doc
    }


}
