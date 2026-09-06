using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

public class RegistrationMatrix
{
    public int RegistrationMatrixId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityName { get; set; } = "Article";
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public int? LastImportBatchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<RegistrationMatrixColumn> Columns { get; set; } = new List<RegistrationMatrixColumn>();
    public ICollection<RegistrationMatrixRow> Rows { get; set; } = new List<RegistrationMatrixRow>();
}

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
