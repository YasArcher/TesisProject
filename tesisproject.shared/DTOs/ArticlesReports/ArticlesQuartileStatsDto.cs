namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesQuartileStatsDto
    {
        public int TotalArticles { get; set; }

        public int Q1Count { get; set; }
        public int Q2Count { get; set; }
        public int Q3Count { get; set; }
        public int Q4Count { get; set; }
        public int NoQuartileCount { get; set; }

        public double Q1Q2Percent { get; set; }
        public double Q3Q4Percent { get; set; }
        public double NoQuartilePercent { get; set; }
    }
}
