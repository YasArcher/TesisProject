using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Authorize(Policy = AppPolicies.ExternalApiAccess)]
    [Route("api/external-api-explorer")]
    [Route("api/scientific-production/external-api-explorer")]
    public class ExternalApiExplorerController : ControllerBase
    {
        private readonly IExternalApiExplorerService _service;
        private readonly IBulkImportService _bulkImportService;

        public ExternalApiExplorerController(IExternalApiExplorerService service, IBulkImportService bulkImportService)
        {
            _service = service;
            _bulkImportService = bulkImportService;
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
            catch (HttpRequestException)
            {
                return BadRequest(new { message = "No pude consultar la API externa. Verifica la conexión, credenciales o intenta nuevamente en unos minutos." });
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
            catch (HttpRequestException)
            {
                return BadRequest(new { message = "No pude enriquecer el artículo desde la API externa. Intenta nuevamente en unos minutos." });
            }
        }

        [HttpPost("scopus/institutional-staging")]
        public async Task<ActionResult<ScopusInstitutionalStagingImportResultDto>> SendScopusInstitutionalDatasetToStaging(
            [FromBody] ScopusInstitutionalStagingImportRequest request,
            CancellationToken ct)
        {
            try
            {
                var institutionName = string.IsNullOrWhiteSpace(request.InstitutionName)
                    ? "Universidad Técnica de Ambato"
                    : request.InstitutionName.Trim();
                var chunkSize = Math.Clamp(request.ChunkSize <= 0 ? 250 : request.ChunkSize, 50, 500);

                var articles = (request.Articles ?? new List<ExternalArticlePreviewDto>())
                    .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Title))
                    .ToList();

                var result = new ScopusInstitutionalStagingImportResultDto
                {
                    InstitutionName = institutionName,
                    TotalRecovered = articles.Count,
                    ChunkSize = chunkSize
                };

                if (articles.Count == 0)
                {
                    result.Message = "No se recibieron artículos preparados para crear los lotes. Primero prepara el dataset institucional y vuelve a intentar.";
                    return Ok(result);
                }

                var userId = GetCurrentUserId();
                var chunkIndex = 0;
                foreach (var chunk in articles.Chunk(chunkSize))
                {
                    chunkIndex++;
                    var staging = await _bulkImportService.CreateBatchFromExternalArticlesAsync(new ExternalArticlesImportRequest
                    {
                        ProviderKey = "scopus",
                        ProviderName = "Scopus",
                        Notes = $"Carga institucional Scopus por filiación {institutionName}. Bloque {chunkIndex}.",
                        ValidateAfterCreate = request.ValidateAfterCreate,
                        Articles = chunk.ToList()
                    }, userId, ct);

                    if (staging.Batch?.Summary is not null && staging.Batch.Summary.ImportBatchId > 0)
                    {
                        result.Batches.Add(new ScopusInstitutionalStagingBatchDto
                        {
                            ImportBatchId = staging.Batch.Summary.ImportBatchId,
                            BatchCode = staging.Batch.Summary.BatchCode,
                            Rows = staging.Batch.Summary.TotalRows,
                            ErrorRows = staging.Batch.Summary.ErrorRows,
                            Status = staging.Batch.Summary.Status
                        });
                    }
                }

                result.BatchCount = result.Batches.Count;
                result.TotalSentToStaging = result.Batches.Sum(x => x.Rows);
                result.Message = $"Se recuperaron {result.TotalRecovered} artículo(s) desde Scopus y se crearon {result.BatchCount} lote(s) de staging.";
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException)
            {
                return BadRequest(new { message = "No pude consultar Scopus para crear el staging institucional. Verifica la conexión o intenta nuevamente en unos minutos." });
            }
        }

        private string? GetCurrentUserId()
            => User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User?.Identity?.Name;
    }
}
