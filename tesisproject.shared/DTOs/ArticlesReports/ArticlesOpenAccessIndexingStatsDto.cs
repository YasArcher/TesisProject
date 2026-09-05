namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesOpenAccessIndexingStatsDto
    {
        public int TotalArticles { get; set; }

        public int OpenAccessCount { get; set; }
        public double OpenAccessPercent { get; set; }

        public int ScopusIndexedCount { get; set; }
        public double ScopusIndexedPercent { get; set; }
    }
}
