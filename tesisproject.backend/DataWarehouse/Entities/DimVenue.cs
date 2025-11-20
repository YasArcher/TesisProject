namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimVenue
    {
        public int VenueKey { get; set; }         
        public int VenueId { get; set; }        

        public string Name { get; set; } = null!;
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string Type { get; set; } = null!;
    }
}
