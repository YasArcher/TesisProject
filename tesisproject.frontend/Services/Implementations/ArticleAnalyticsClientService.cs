using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Implementations;

// Modulo articulos: consulta de resultados agregados del DW.
public sealed class ArticleAnalyticsClientService(IApiClient api) : IArticleAnalyticsClientService
{
    public Task<HttpResponseWrapper<ArticleAnalyticsSummaryDto?>> GetSummaryAsync(
        int? year, string? search, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (year.HasValue) filters.Add($"year={year.Value}");
        if (!string.IsNullOrWhiteSpace(search))
            filters.Add($"search={Uri.EscapeDataString(search.Trim())}");
        var query = filters.Count == 0 ? string.Empty : $"?{string.Join("&", filters)}";
        return api.GetAsync<ArticleAnalyticsSummaryDto>($"articles/analytics/summary{query}", ct);
    }
}
