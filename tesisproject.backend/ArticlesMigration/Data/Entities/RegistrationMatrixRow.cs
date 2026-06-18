using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Entities
{
    public class RegistrationMatrixRow
    {
        public int RegistrationMatrixRowId { get; set; }
        public int RegistrationMatrixId { get; set; }
        public RegistrationMatrix? Matrix { get; set; }
        public int RowNumber { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RegistrationMatrixCell> Cells { get; set; } = new List<RegistrationMatrixCell>();
    }
}
