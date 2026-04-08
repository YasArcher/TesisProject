using System;
using System.Collections.Generic;
namespace tesisproject.backend.Data.Entities
{
    public class WorkflowStageDefinition
    {
        public int WorkflowStageDefinitionId { get; set; }
        public int WorkflowDefinitionId { get; set; }
        public string StageKey { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string? StageGroupKey { get; set; }
        public string? StageGroupName { get; set; }
        public string? ResponsibleRoleId { get; set; }
        public bool CanEditData { get; set; }
        public bool CanReturn { get; set; } = true;
        public bool CanApprove { get; set; } = true;
        public bool CanProcessBatch { get; set; }
        public bool IsFinalStage { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public WorkflowDefinition? WorkflowDefinition { get; set; }
        public ICollection<WorkflowStageInstance> StageInstances { get; set; } = new List<WorkflowStageInstance>();
    }
}
