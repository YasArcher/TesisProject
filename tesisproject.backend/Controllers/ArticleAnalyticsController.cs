using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers;

// Modulo articulos: lectura analitica compartida por reporteria e inteligencia.
[ApiController]
[Route("api/articles/analytics")]
public sealed class ArticleAnalyticsController(
    IArticleAnalyticsService service,
    IOptions<ArticlesModuleOptions> options) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = ArticlePolicyNames.AnalyticsRead)]
    public async Task<ActionResult<ServiceResult<ArticleAnalyticsSummaryDto>>> GetSummary(
        [FromQuery] int? year, [FromQuery] string? search, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ServiceResult<ArticleAnalyticsSummaryDto>.Fail(
                    "El modulo de articulos no esta habilitado en este entorno.",
                    ErrorType.Unexpected, "ARTICLES_MODULE_DISABLED"));

        if (year is < 1900 or > 9999)
            return BadRequest(ServiceResult<ArticleAnalyticsSummaryDto>.Fail(
                "El ano de publicacion no es valido.", ErrorType.Validation, "INVALID_PUBLICATION_YEAR"));

        return Ok(ServiceResult<ArticleAnalyticsSummaryDto>.Ok(
            await service.GetSummaryAsync(year, search, ct)));
    }
}
