using System;
using System.Collections.Generic;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data.Entities
{
    public class WorkflowInstance
    {
        public int WorkflowInstanceId { get; set; }
        public int WorkflowDefinitionId { get; set; }
        public int ImportBatchId { get; set; }
        public string Status { get; set; } = "Draft";
        public int? CurrentStageDefinitionId { get; set; }
        public string? SubmittedByUserId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastActionAt { get; set; }

        public WorkflowDefinition? WorkflowDefinition { get; set; }
        public ImportBatch? Batch { get; set; }
        public WorkflowStageDefinition? CurrentStageDefinition { get; set; }
        public ApplicationUser? SubmittedByUser { get; set; }
        public ICollection<WorkflowStageInstance> StageInstances { get; set; } = new List<WorkflowStageInstance>();
        public ICollection<WorkflowActionLog> ActionLogs { get; set; } = new List<WorkflowActionLog>();
    }
}
