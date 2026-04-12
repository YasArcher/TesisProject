using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Wrappers;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Authorize(Policy = AppPolicies.ExternalApiAccess)]
    [Route("api/external-api-explorer")]
    public class ExternalApiExplorerController : ControllerBase
    {
        private readonly IExternalApiExplorerService _service;

        public ExternalApiExplorerController(IExternalApiExplorerService service)
        {
            _service = service;
        }

        [HttpGet("providers")]
        public async Task<ActionResult<ApiResult<List<ExternalApiProviderDto>>>> GetProviders(CancellationToken ct)
            => Ok(ApiResult<List<ExternalApiProviderDto>>.Success(await _service.GetProvidersAsync(ct)));

        [HttpPost("query")]
        public async Task<ActionResult<ApiResult<ExternalApiQueryResultDto>>> Query([FromBody] ExternalApiQueryRequest request, CancellationToken ct)
        {
            try
            {
                return Ok(ApiResult<ExternalApiQueryResultDto>.Success(await _service.QueryAsync(request, ct)));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<ExternalApiQueryResultDto>.Fail(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ApiResult<ExternalApiQueryResultDto>.Fail($"No pude consultar la API externa: {ex.Message}"));
            }
        }

        [HttpPost("providers/{providerKey}/enrich")]
        public async Task<ActionResult<ApiResult<ExternalArticlePreviewDto>>> Enrich(string providerKey, [FromBody] ExternalArticlePreviewDto article, CancellationToken ct)
        {
            try
            {
                return Ok(ApiResult<ExternalArticlePreviewDto>.Success(await _service.EnrichArticleAsync(providerKey, article, ct)));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<ExternalArticlePreviewDto>.Fail(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ApiResult<ExternalArticlePreviewDto>.Fail($"No pude enriquecer el artículo desde la API externa: {ex.Message}"));
            }
        }
    }
}
