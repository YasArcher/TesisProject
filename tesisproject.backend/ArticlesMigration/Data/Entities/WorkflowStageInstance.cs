using System;
using System.Collections.Generic;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data.Entities
{
    public class WorkflowStageInstance
    {
        public int WorkflowStageInstanceId { get; set; }
        public int WorkflowInstanceId { get; set; }
        public int WorkflowStageDefinitionId { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AssignedToUserId { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? Notes { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }
        public WorkflowStageDefinition? WorkflowStageDefinition { get; set; }
        public ApplicationUser? AssignedToUser { get; set; }
        public ApplicationUser? ApprovedByUser { get; set; }
        public ICollection<WorkflowActionLog> ActionLogs { get; set; } = new List<WorkflowActionLog>();
    }
}
