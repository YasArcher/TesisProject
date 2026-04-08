using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
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
        public async Task<ActionResult<List<ExternalApiProviderDto>>> GetProviders(CancellationToken ct)
            => Ok(await _service.GetProvidersAsync(ct));

        [HttpPost("query")]
        public async Task<ActionResult<ExternalApiQueryResultDto>> Query([FromBody] ExternalApiQueryRequest request, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.QueryAsync(request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { message = $"No pude consultar la API externa: {ex.Message}" });
            }
        }

        [HttpPost("providers/{providerKey}/enrich")]
        public async Task<ActionResult<ExternalArticlePreviewDto>> Enrich(string providerKey, [FromBody] ExternalArticlePreviewDto article, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.EnrichArticleAsync(providerKey, article, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { message = $"No pude enriquecer el artículo desde la API externa: {ex.Message}" });
            }
        }
    }
}
