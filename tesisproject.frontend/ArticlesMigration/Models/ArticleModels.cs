namespace tesisproject.frontend.Models
{

    public record ArticleDto
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Title { get; init; } = string.Empty;
        public List<string> Authors { get; init; } = new();
        public string Venue { get; init; } = string.Empty;
        public int Year { get; init; }
        public List<string> Keywords { get; init; } = new();
        public string? Doi { get; init; }
        public string? Url { get; init; }
        public bool OpenAccess { get; init; }
        public int? CitationCount { get; init; }
    }

    public record DashboardKpis(int TotalArticles, double OpenAccessPct, int MedianYear, int TotalCitations);

    public record CountByYear(int Year, int Count);
    public record TopItem(string Name, int Count);
    public record RecommendationItem(string ArticleId, double Score, List<string> Why);
    public record PredictionPoint(DateOnly Period, double Value, double? Lower = null, double? Upper = null);
    public record Filters(
        string? Query = null,
        int? From = null,
        int? To = null,
        string? Venue = null,
        string? Author = null,
        bool? OpenAccess = null,
        List<string>? Tags = null
    );
    public record ImportResult(int Inserted, int Skipped, IReadOnlyList<string> Errors);
}
