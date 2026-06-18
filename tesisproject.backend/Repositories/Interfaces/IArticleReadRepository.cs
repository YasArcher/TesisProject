// [ARTICLES-MIGRATION] Origen: sistema de articulos. Repositorio temporal adaptado a la arquitectura base de proyectos.
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IArticleReadRepository
    {
        Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPageAsync(
            ArticleListQuery query,
            CancellationToken ct = default);

        Task<Article?> GetDetailAsync(int articleId, CancellationToken ct = default);
    }
}
