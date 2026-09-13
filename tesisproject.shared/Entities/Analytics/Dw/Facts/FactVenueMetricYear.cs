using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts;

public class FactVenueMetricYear
{
    [Key]
    public int FactVenueMetricYearId { get; set; }
    public int VenueKey { get; set; }
    public short Year { get; set; }
    public decimal? Sjr { get; set; }
    [MaxLength(50)] public string? Quartile { get; set; }

    public DimVenue Venue { get; set; } = null!;
}
