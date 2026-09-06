using System.Collections.Generic;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class Venue
    {
        public int VenueId { get; set; }

        public string Name { get; set; } = null!;
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string Type { get; set; } = null!;
        public ICollection<Article> Articles { get; set; } = new List<Article>();
        public ICollection<VenueMetric> VenueMetrics { get; set; } = new List<VenueMetric>();
    }
