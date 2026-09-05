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
    [ApiController]
    [Route("api/articles")]
    public sealed class ArticlesController : ControllerBase
    {
        private readonly IArticleQueryService _service;
        private readonly IArticleRegistrationCommandService _registrationService;
        private readonly ArticlesModuleOptions _options;

        public ArticlesController(
            IArticleQueryService service,
            IArticleRegistrationCommandService registrationService,
            IOptions<ArticlesModuleOptions> options)
        {
            _service = service;
            _registrationService = registrationService;
            _options = options.Value;
        }

        [HttpGet]
        [Authorize(Policy = ArticlePolicyNames.Listing)]
        public async Task<ActionResult<ServiceResult<ArticlePageDto>>> GetPage(
            [FromQuery] ArticleListQuery query,
            CancellationToken ct)
        {
            if (!_options.Enabled)
                return ModuleUnavailable<ArticlePageDto>();

            return (await _service.GetPageAsync(query, ct)).ToActionResult();
        }

        [HttpPost]
        [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
        public async Task<ActionResult<ServiceResult<RegisterArticleAggregateResponse>>> Register(
            [FromBody] RegisterArticleAggregateRequest request,
            CancellationToken ct)
        {
            if (!_options.Enabled)
                return ModuleUnavailable<RegisterArticleAggregateResponse>();

            return (await _registrationService.RegisterAsync(request, ct)).ToActionResult();
        }

        [HttpGet("{articleId:int}")]
        [Authorize(Policy = ArticlePolicyNames.Listing)]
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
