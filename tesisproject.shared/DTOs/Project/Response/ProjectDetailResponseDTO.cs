using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Budgets.Request;

namespace tesisproject.shared.DTOs.Project.Response
{
    public class ProjectDetailResponseDTO
    {
        // --- General ---
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectObjective { get; set; } = string.Empty;
        public string ResearchLine { get; set; } = string.Empty;

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

        public int? SenesytGroupId { get; set; }        // null => no asignado
        public string? SenesytGroupName { get; set; }   // null => no asignado

        // --- Budget (solo Id para pestaña Presupuesto) ---
        public int BudgetId { get; set; }
        public decimal BudgetAmount { get; set; }
    }
}
