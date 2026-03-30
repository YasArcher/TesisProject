using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Entities
{
    public class RegistrationMatrix
    {
        public int RegistrationMatrixId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EntityName { get; set; } = "Article";
        public string Status { get; set; } = "Draft";
        public string? Notes { get; set; }
        public int? LastImportBatchId { get; set; }
        public ImportBatch? LastImportBatch { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RegistrationMatrixColumn> Columns { get; set; } = new List<RegistrationMatrixColumn>();
        public ICollection<RegistrationMatrixRow> Rows { get; set; } = new List<RegistrationMatrixRow>();
    }
}
