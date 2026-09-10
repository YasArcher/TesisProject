using tesisproject.backend.Data.ReadModels;

namespace tesisproject.backend.Repositories.Unified.Interfaces;

public interface IUnifiedArticleReadRepository
{
    /// <summary>Server-composable query; FacultyId is the article's explicit faculty.</summary>
    IQueryable<ArticleReadModel> Query();
    Task<(IReadOnlyList<ArticleReadAggregate> Items, int TotalCount)> GetPageAsync(tesisproject.shared.DTOs.Articles.ArticleListQuery query, CancellationToken ct = default);
    Task<ArticleReadAggregate?> GetDetailAsync(int articleId, CancellationToken ct = default);
}
