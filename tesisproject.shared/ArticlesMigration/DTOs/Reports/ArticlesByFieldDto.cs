namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesByFieldDto
    {
        public string? BroadFieldName { get; set; }
        public string? SpecificFieldName { get; set; }
        public string? DetailedFieldName { get; set; }
        public int Count { get; set; }
    }
}
