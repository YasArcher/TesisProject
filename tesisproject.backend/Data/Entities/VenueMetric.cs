namespace tesisproject.backend.Data.Entities
{
    public class VenueMetric
    {
        public int VenueId { get; set; }
        public short Year { get; set; }

        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }

        public Venue Venue { get; set; } = null!;
    }
}
