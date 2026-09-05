namespace tesisproject.shared.DTOs.Venues
{
    public class VenueUpsertRequest
    {
        public string Name { get; set; } = default!;
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string Type { get; set; } = "Journal";
    }

    public class VenueDto
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = default!;
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string Type { get; set; } = "Journal";
    }

    public class VenueUpsertResponse
    {
        public int VenueId { get; set; }
        public bool Created { get; set; }
        public VenueDto? Venue { get; set; }
        public string? Message { get; set; }
    }

    public class VenueMetricUpsertRequest
    {
        public short Year { get; set; }       // smallint
        public decimal? SJR { get; set; }     // 👈 mayúsculas para casar con VenuesService
        public string? Quartile { get; set; } // Q1..Q4
    }

    public class VenueMetricDto
    {
        public int VenueId { get; set; }
        public short Year { get; set; }
        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }
    }

    public class VenueMetricUpsertResponse
    {
        public int VenueId { get; set; }
        public short Year { get; set; }
        public bool Created { get; set; }
        public VenueMetricDto? Metric { get; set; }
        public string? Message { get; set; }
    }
}
