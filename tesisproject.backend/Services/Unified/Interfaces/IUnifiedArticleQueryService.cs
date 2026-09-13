using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedArticleQueryService
{
    Task<ServiceResult<ArticlePageDto>> GetPageAsync(ArticleListQuery query, CancellationToken ct = default);
    Task<ServiceResult<ArticleDetailDto>> GetDetailAsync(int articleId, CancellationToken ct = default);
}
