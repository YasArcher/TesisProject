using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Articles.Entities;

public class SpecificField
{
    public int SpecificFieldId { get; set; }
    public int BroadFieldId { get; set; }
    public BroadField BroadField { get; set; } = default!;
    [MaxLength(20)] public string? Code { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
    public ICollection<DetailedField> DetailedFields { get; set; } = new List<DetailedField>();
}
