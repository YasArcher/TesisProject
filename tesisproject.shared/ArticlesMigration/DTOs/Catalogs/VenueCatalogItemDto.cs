namespace tesisproject.shared.DTOs.Catalogs
{
    public class VenueCatalogItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IssnCode { get; set; }
        public string? JournalUrl { get; set; }
    }
}
