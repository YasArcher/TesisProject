namespace tesisproject.shared.DTOs.Articles;

// Modulo articulos: resultados agregados del Data Warehouse para reporteria e inteligencia.
public sealed class ArticleAnalyticsSummaryDto
{
    public int TotalArticles { get; set; }
    public int WithDoi { get; set; }
    public int OpenAccess { get; set; }
    public int WithAuthors { get; set; }
    public int WithIndexing { get; set; }
    public int WithFaculty { get; set; }
    public List<ArticleAnalyticsGroupDto> ByPublicationYear { get; set; } = [];
    public List<ArticleAnalyticsGroupDto> ByFaculty { get; set; } = [];
    public List<ArticleAnalyticsGroupDto> ByIndexingDatabase { get; set; } = [];
    public List<ArticleAnalyticsGroupDto> ByPublicationStatus { get; set; } = [];
}

public sealed class ArticleAnalyticsGroupDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
