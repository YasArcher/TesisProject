namespace tesisproject.backend.DataWarehouse.Entities
{
    public class FactVenueMetricYear
    {
        public int Id { get; set; } 

        public int VenueKey { get; set; }
        public DimVenue Venue { get; set; } = null!;

        public short Year { get; set; }

        public int? YearDateKey { get; set; }
        public DimDate? YearDate { get; set; }

        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }
    }
}
