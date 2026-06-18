using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Entities
{
    public class ImportBatchRow
    {
        public int ImportBatchRowId { get; set; }
        public int ImportBatchId { get; set; }
        public int RowNumber { get; set; }
        public string RowStatus { get; set; } = string.Empty;
        public string? RawJson { get; set; }
        public int? TargetArticleId { get; set; }
        public int? TargetParticipantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ImportBatch? Batch { get; set; }
        public ICollection<ImportBatchRowValue> Values { get; set; } = new List<ImportBatchRowValue>();
        public ICollection<ImportBatchError> Errors { get; set; } = new List<ImportBatchError>();
    }
}
