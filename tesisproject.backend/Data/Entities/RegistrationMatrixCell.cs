using System;

namespace tesisproject.backend.Data.Entities
{
    public class RegistrationMatrixCell
    {
        public int RegistrationMatrixCellId { get; set; }
        public int RegistrationMatrixRowId { get; set; }
        public RegistrationMatrixRow? Row { get; set; }
        public int FieldId { get; set; }
        public FieldCatalogEntry? Field { get; set; }
        public string? RawValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
