using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimVenue
{
    [Key]
    public int VenueKey { get; set; }

    public int VenueId { get; set; }
    [Required, MaxLength(300)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? IssnCode { get; set; }
    [MaxLength(100)] public string? Issue { get; set; }
    [MaxLength(100)] public string? Volume { get; set; }
    [MaxLength(2048)] public string? Url { get; set; }
    [MaxLength(100)] public string? Type { get; set; }
}
