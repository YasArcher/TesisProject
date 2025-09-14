using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Abstractions;
namespace tesisproject.frontend.Services.Interfaces
{
    public interface IArticlesClient
    {
        Task<Result<int>> CreateAsync(CreateArticleRequest req);
        Task<Result<IReadOnlyList<ArticleDto>>?> GetAllAsync();
    }
}
