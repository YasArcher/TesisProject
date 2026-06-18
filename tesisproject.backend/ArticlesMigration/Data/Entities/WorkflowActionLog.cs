using System;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data.Entities
{
    public class WorkflowActionLog
    {
        public int WorkflowActionLogId { get; set; }
        public int WorkflowInstanceId { get; set; }
        public int? WorkflowStageInstanceId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? PerformedByUserId { get; set; }
        public DateTime PerformedAt { get; set; }
        public string? Comments { get; set; }
        public string? PayloadJson { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }
        public WorkflowStageInstance? WorkflowStageInstance { get; set; }
        public ApplicationUser? PerformedByUser { get; set; }
    }
}
