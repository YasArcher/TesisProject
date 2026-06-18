using System;

namespace tesisproject.backend.Data.Entities
{
    public class ImportBatchRowValue
    {
        public int ImportBatchRowValueId { get; set; }
        public int ImportBatchRowId { get; set; }
        public int FieldId { get; set; }
        public string? RawValue { get; set; }
        public string? NormalizedValue { get; set; }
        public string? ValueType { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ImportBatchRow? Row { get; set; }
        public FieldCatalogEntry? Field { get; set; }
    }
}
