using System;

namespace tesisproject.backend.Data.Entities
{
    public class RegistrationMatrixColumn
    {
        public int RegistrationMatrixColumnId { get; set; }
        public int RegistrationMatrixId { get; set; }
        public RegistrationMatrix? Matrix { get; set; }
        public int FieldId { get; set; }
        public FieldCatalogEntry? Field { get; set; }
        public int DisplayOrder { get; set; }
        public int WidthUnits { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
