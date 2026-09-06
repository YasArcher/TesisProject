namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class VenueMetric
    {
        public int VenueId { get; set; }
        public short Year { get; set; }

        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }

        public Venue Venue { get; set; } = null!;
    }
