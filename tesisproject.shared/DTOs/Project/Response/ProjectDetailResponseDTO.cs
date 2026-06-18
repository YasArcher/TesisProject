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

        public ICollection<ProjectDocumentRefDTO> Documents { get; set; }
            = new List<ProjectDocumentRefDTO>();

        // --- Convocatoria ---
        public int ConvocationId { get; set; }
        public int DurationInMonths { get; set; }

        public ICollection<ProjectObjectiveListItemDTO>? ProjectObjectives { get; set; }

        public ICollection<int> ResearchCategoryIds { get; set; } = new List<int>();

        public int ProjectTypeId { get; set; }
        public string ProjectTypeName { get; set; } = string.Empty;

        public int ProjectStateId { get; set; }
        public string ProjectStateName { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? TentativeEndDate { get; set; }
        public DateTime? RealEndDate { get; set; }
        public decimal ExecutionPercentage { get; set; }
        public int ProjectOriginTypeId { get; set; }
        public int ProjectGroupId { get; set; }
        public string ProjectGroupName { get; set; } = string.Empty;

        public ICollection<ProjectBudgetDetailDTO> Budgets { get; set; } = new List<ProjectBudgetDetailDTO>();
    }

    public class ProjectDocumentRefDTO
    {
        public int DocumentId { get; set; }
        public int DocumentTypeId { get; set; }
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
}