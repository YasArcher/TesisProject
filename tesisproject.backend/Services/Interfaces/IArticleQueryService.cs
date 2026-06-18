// [ARTICLES-MIGRATION] Origen: sistema de articulos. Servicio de consulta previo a exponer endpoints.
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IArticleQueryService
    {
        Task<ServiceResult<ArticlePageDto>> GetPageAsync(ArticleListQuery query, CancellationToken ct = default);
        Task<ServiceResult<ArticleDetailDto>> GetDetailAsync(int articleId, CancellationToken ct = default);
    }
}
