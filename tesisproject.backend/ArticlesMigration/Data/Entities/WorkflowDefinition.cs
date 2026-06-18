using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Entities
{
    public class WorkflowDefinition
    {
        public int WorkflowDefinitionId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<WorkflowStageDefinition> Stages { get; set; } = new List<WorkflowStageDefinition>();
        public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
    }
}
