using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimAcademicTerm
{
    [Key]
    public int AcademicTermKey { get; set; }
    public int AcademicTermId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
}
