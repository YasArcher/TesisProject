using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimPublicationStatus
{
    [Key]
    public int PublicationStatusKey { get; set; }
    public byte PublicationStatusId { get; set; }
    [Required, MaxLength(20)] public string Name { get; set; } = string.Empty;
}
