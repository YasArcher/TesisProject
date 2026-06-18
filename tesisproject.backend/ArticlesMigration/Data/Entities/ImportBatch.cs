using System;
using System.Collections.Generic;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Data.Entities
{
    public class ImportBatch
    {
        public int ImportBatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? SourceReference { get; set; }
        public int TotalRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int ErrorRows { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? Notes { get; set; }

        public ApplicationUser? CreatedByUser { get; set; }
        public WorkflowInstance? WorkflowInstance { get; set; }
        public ICollection<ImportBatchRow> Rows { get; set; } = new List<ImportBatchRow>();
        public ICollection<ImportBatchError> Errors { get; set; } = new List<ImportBatchError>();
    }
}
