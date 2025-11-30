using System;
using System.Collections.Generic;
using tesisproject.shared.DTOs.ProjectObjective.Response;

namespace tesisproject.shared.DTOs.Project.Response
{
    public class ProjectDetailResponseDTO
    {
        // --- General ---
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        public ICollection<ProjectObjectiveListItemDTO>? ProjectObjectives { get; set; }

        // 🔹 Dominios con sus líneas de investigación (solo las del proyecto)
        public ICollection<ProjectResearchDomainDTO> ResearchDomains { get; set; }
            = new List<ProjectResearchDomainDTO>();

        public int ProjectTypeId { get; set; }
        public string ProjectTypeName { get; set; } = string.Empty;

        public int ProjectStateId { get; set; }
        public string ProjectStateName { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? TentativeEndDate { get; set; }
        public DateTime? RealEndDate { get; set; }
        public decimal ExecutionPercentage { get; set; }

        // --- Groups (main + optional SENESCYT) ---
        public int ProjectGroupId { get; set; }
        public string ProjectGroupName { get; set; } = string.Empty;

        public int? SenesytGroupId { get; set; }
        public string? SenesytGroupName { get; set; }

        // --- Budget ---
        public ICollection<ProjectBudgetDetailDTO> Budgets { get; set; } = new List<ProjectBudgetDetailDTO>();

    }

    // 🔸 Dominio de investigación con sus líneas (para este proyecto)
    public class ProjectResearchDomainDTO
    {
        public int ResearchDomainTypeId { get; set; }
        public string ResearchDomainTypeName { get; set; } = string.Empty;

        public ICollection<ProjectResearchLineDTO> ResearchLines { get; set; }
            = new List<ProjectResearchLineDTO>();
    }

    public class ProjectBudgetDetailDTO
    {
        public int BudgetId { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal CertifiedAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public int FundingTypeId { get; set; }
        public string FundingTypeName { get; set; } = string.Empty;
    }

    // 🔹 Línea de investigación dentro de un dominio
    public class ProjectResearchLineDTO
    {
        public int ResearchLineTypeId { get; set; }
        public string ResearchLineTypeName { get; set; } = string.Empty;
    }
}
