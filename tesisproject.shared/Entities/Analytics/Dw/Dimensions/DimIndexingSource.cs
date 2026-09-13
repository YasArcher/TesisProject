using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimIndexingSource
{
    [Key]
    public int IndexingSourceKey { get; set; }
    public int IndexingSourceId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
