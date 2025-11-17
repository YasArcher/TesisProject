namespace tesisproject.shared.DTOs.Articles
{
    public class ArticleListQuery
    {
        public string? Search { get; set; }
        public short? Year { get; set; }
        public int? VenueId { get; set; }
        public string? SearchTerm { get; set; }
        public byte? PublicationStatusId { get; set; }
        public int? ResearchLineId { get; set; }
        public int? BroadFieldId { get; set; }
        public int? SpecificFieldId { get; set; }
        public int? DetailedFieldId { get; set; }
        public int? ProjectId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDesc { get; set; } = true;
    }
}
