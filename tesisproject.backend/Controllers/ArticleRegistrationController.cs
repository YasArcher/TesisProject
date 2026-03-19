using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/articles/aggregate")]
    [AllowAnonymous]
    public class ArticleRegistrationController : ControllerBase
    {
        private readonly IArticleRegistrationService _articleRegistrationService;

        public ArticleRegistrationController(IArticleRegistrationService articleRegistrationService)
        {
            _articleRegistrationService = articleRegistrationService;
        }

        [HttpPost]
        public async Task<ActionResult<RegisterArticleAggregateResponse>> Register(
            [FromBody] RegisterArticleAggregateRequest request,
            CancellationToken ct)
        {
            var result = await _articleRegistrationService.RegisterArticleAggregateAsync(request, ct);
            return Ok(result);
        }
    }
}
