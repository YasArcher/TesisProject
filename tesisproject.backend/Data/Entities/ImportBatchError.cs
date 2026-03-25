using System;

namespace tesisproject.backend.Data.Entities
{
    public class ImportBatchError
    {
        public int ImportBatchErrorId { get; set; }
        public int ImportBatchId { get; set; }
        public int? ImportBatchRowId { get; set; }
        public int? FieldId { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public ImportBatch? Batch { get; set; }
        public ImportBatchRow? Row { get; set; }
        public FieldCatalogEntry? Field { get; set; }
    }
}
