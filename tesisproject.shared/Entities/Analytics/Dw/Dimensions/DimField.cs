using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimField
{
    [Key]
    public int FieldKey { get; set; }

    public int BroadFieldId { get; set; }
    [Required, MaxLength(200)] public string BroadFieldName { get; set; } = string.Empty;
    public int? SpecificFieldId { get; set; }
    [MaxLength(200)] public string? SpecificFieldName { get; set; }
    public int? DetailedFieldId { get; set; }
    [MaxLength(200)] public string? DetailedFieldName { get; set; }
}
