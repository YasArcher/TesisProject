namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesKpiSummaryDto
    {
        public int TotalArticles { get; set; }
        public int PublishedThisYear { get; set; }
        public int Q1Q2Count { get; set; }
        public int OpenAccessCount { get; set; }
        public int IndexedInScopusCount { get; set; }
    }
}
