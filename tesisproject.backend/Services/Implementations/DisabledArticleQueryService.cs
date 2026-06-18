// [ARTICLES-MIGRATION] Servicio inerte para mantener el controlador resoluble mientras el modulo esta deshabilitado.
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class DisabledArticleQueryService : IArticleQueryService
    {
        private const string Message = "El modulo de articulos todavia no esta habilitado en este entorno.";

        public Task<ServiceResult<ArticlePageDto>> GetPageAsync(ArticleListQuery query, CancellationToken ct = default)
            => Task.FromResult(ServiceResult<ArticlePageDto>.Fail(
                Message, ErrorType.Unexpected, "ARTICLES_MODULE_DISABLED"));

        public Task<ServiceResult<ArticleDetailDto>> GetDetailAsync(int articleId, CancellationToken ct = default)
            => Task.FromResult(ServiceResult<ArticleDetailDto>.Fail(
                Message, ErrorType.Unexpected, "ARTICLES_MODULE_DISABLED"));
    }
}
