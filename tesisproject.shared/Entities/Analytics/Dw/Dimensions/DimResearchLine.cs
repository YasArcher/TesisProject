using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimResearchLine
{
    [Key]
    public int ResearchLineKey { get; set; }
    public int ResearchLineId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
}
