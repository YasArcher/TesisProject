namespace tesisproject.shared.DTOs.Workflow
{
    public class WorkflowBatchDetailDto
    {
        public int WorkflowInstanceId { get; set; }
        public int ImportBatchId { get; set; }
        public string WorkflowKey { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CurrentStageDefinitionId { get; set; }
        public string? CurrentStageKey { get; set; }
        public string? CurrentStageName { get; set; }
        public string? CurrentStageGroupKey { get; set; }
        public string? CurrentStageGroupName { get; set; }
        public string? SubmittedByUserId { get; set; }
        public string? SubmittedByUserName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? LastActionAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<WorkflowStageInstanceDto> Stages { get; set; } = new();
        public List<WorkflowActionLogDto> Actions { get; set; } = new();
    }

    public class WorkflowInboxItemDto
    {
        public int WorkflowInstanceId { get; set; }
        public int ImportBatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string WorkflowKey { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string WorkflowStatus { get; set; } = string.Empty;
        public string? CurrentStageKey { get; set; }
        public string? CurrentStageName { get; set; }
        public string? CurrentStageGroupName { get; set; }
        public string? ResponsibleRoleName { get; set; }
        public string? SubmittedByUserId { get; set; }
        public string? SubmittedByUserName { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int TotalRows { get; set; }
        public int ErrorRows { get; set; }
        public int ValidRows { get; set; }
        public int ProcessedRows { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? LastActionAt { get; set; }
        public bool CanClaim { get; set; }
        public bool CanReturn { get; set; }
        public bool CanApprove { get; set; }
        public bool CanProcessBatch { get; set; }
    }

    public class WorkflowStageInstanceDto
    {
        public int WorkflowStageInstanceId { get; set; }
        public int WorkflowStageDefinitionId { get; set; }
        public string StageKey { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StageGroupKey { get; set; }
        public string? StageGroupName { get; set; }
        public string? ResponsibleRoleId { get; set; }
        public bool CanEditData { get; set; }
        public bool CanReturn { get; set; }
        public bool CanApprove { get; set; }
        public bool CanProcessBatch { get; set; }
        public bool IsFinalStage { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class WorkflowActionLogDto
    {
        public int WorkflowActionLogId { get; set; }
        public int? WorkflowStageInstanceId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? PerformedByUserId { get; set; }
        public string? PerformedByUserName { get; set; }
        public DateTime PerformedAt { get; set; }
        public string? Comments { get; set; }
    }

    public class WorkflowActionRequest
    {
        public string? Comments { get; set; }
    }
}
