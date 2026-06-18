// [ARTICLES-MIGRATION] Origen: sistema de articulos. Controlador de solo lectura protegido por bandera de activacion.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Policy = ArticlePolicyNames.Listing)]
    [ApiController]
    [Route("api/articles")]
    public sealed class ArticlesController : ControllerBase
    {
        private readonly IArticleQueryService _service;
        private readonly ArticlesModuleOptions _options;

        public ArticlesController(
            IArticleQueryService service,
            IOptions<ArticlesModuleOptions> options)
        {
            _service = service;
            _options = options.Value;
        }

        [HttpGet]
        public async Task<ActionResult<ServiceResult<ArticlePageDto>>> GetPage(
            [FromQuery] ArticleListQuery query,
            CancellationToken ct)
        {
            if (!_options.Enabled)
                return ModuleUnavailable<ArticlePageDto>();

            return (await _service.GetPageAsync(query, ct)).ToActionResult();
        }

        [HttpGet("{articleId:int}")]
        public async Task<ActionResult<ServiceResult<ArticleDetailDto>>> GetDetail(
            int articleId,
            CancellationToken ct)
        {
            if (!_options.Enabled)
                return ModuleUnavailable<ArticleDetailDto>();

            return (await _service.GetDetailAsync(articleId, ct)).ToActionResult();
        }

        private ActionResult<ServiceResult<T>> ModuleUnavailable<T>()
            => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ServiceResult<T>.Fail(
                    "El modulo de articulos todavia no esta habilitado en este entorno.",
                    ErrorType.Unexpected,
                    "ARTICLES_MODULE_DISABLED"));
    }
}
