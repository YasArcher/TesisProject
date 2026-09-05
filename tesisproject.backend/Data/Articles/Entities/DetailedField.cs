using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Articles.Entities;

public class DetailedField
{
    public int DetailedFieldId { get; set; }
    public int SpecificFieldId { get; set; }
    public SpecificField SpecificField { get; set; } = default!;
    [MaxLength(20)] public string? Code { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
}
