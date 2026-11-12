using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.External.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers;

// Modulo articulos: acceso autorizado a consultas externas de solo lectura.
[ApiController]
[Route("api/articles/external-search")]
public sealed class ArticleExternalSearchController(
    IArticleExternalSearchService service, IOptions<ArticlesModuleOptions> options) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ArticlePolicyNames.ExternalApis)]
    public async Task<ActionResult<ServiceResult<ExternalApiQueryResultDto>>> Search(
        [FromBody] ExternalApiQueryRequest request, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ServiceResult<ExternalApiQueryResultDto>.Fail(
                    "El modulo de articulos no esta habilitado.", ErrorType.Unexpected,
                    "ARTICLES_MODULE_DISABLED"));
        var result = await service.SearchAsync(request, ct);
        return result.Success ? Ok(result) : result.Error == ErrorType.Validation
            ? BadRequest(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
