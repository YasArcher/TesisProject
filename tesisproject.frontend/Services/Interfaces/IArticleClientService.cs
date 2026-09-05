using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Interfaces;

public interface IArticleClientService
{
    Task<HttpResponseWrapper<ArticlePageDto?>> GetPageAsync(
        ArticleListQuery query,
        CancellationToken ct = default);

    Task<HttpResponseWrapper<ArticleDetailDto?>> GetDetailAsync(
        int articleId,
        CancellationToken ct = default);

    Task<HttpResponseWrapper<RegisterArticleAggregateResponse?>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default);
}
