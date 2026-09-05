using System.Globalization;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Implementations;

public sealed class ArticleClientService : IArticleClientService
{
    private readonly IApiClient _api;

    public ArticleClientService(IApiClient api)
    {
        _api = api;
    }

    public Task<HttpResponseWrapper<ArticlePageDto?>> GetPageAsync(
        ArticleListQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<string>
        {
            $"page={Math.Max(1, query.Page)}",
            $"pageSize={Math.Clamp(query.PageSize, 1, 100)}",
            $"sortBy={Uri.EscapeDataString(query.SortBy ?? "CreatedAt")}",
            $"sortDesc={query.SortDesc.ToString().ToLowerInvariant()}"
        };

        Add(parameters, "search", query.Search);
        Add(parameters, "year", query.Year);
        Add(parameters, "publicationStatusId", query.PublicationStatusId);
        Add(parameters, "facultyId", query.FacultyId);
        Add(parameters, "indexingSourceId", query.IndexingSourceId);
        Add(parameters, "researchLineId", query.ResearchLineId);

        return _api.GetAsync<ArticlePageDto>(
            $"articles?{string.Join("&", parameters)}",
            ct);
    }

    public Task<HttpResponseWrapper<ArticleDetailDto?>> GetDetailAsync(
        int articleId,
        CancellationToken ct = default)
    {
        if (articleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(articleId));

        return _api.GetAsync<ArticleDetailDto>($"articles/{articleId}", ct);
    }

    public Task<HttpResponseWrapper<RegisterArticleAggregateResponse?>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _api.PostAsync<RegisterArticleAggregateRequest, RegisterArticleAggregateResponse>("articles", request, ct);
    }

    private static void Add(List<string> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
    }

    private static void Add<T>(List<string> parameters, string key, T? value)
        where T : struct, IFormattable
    {
        if (value.HasValue)
            parameters.Add($"{key}={value.Value.ToString(null, CultureInfo.InvariantCulture)}");
    }
}
