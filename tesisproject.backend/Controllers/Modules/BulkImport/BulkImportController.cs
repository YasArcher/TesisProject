using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Authorize(Policy = AppPolicies.BulkImportAccess)]
    [Route("api/import-batches")]
    public class BulkImportController : ControllerBase
    {
        private readonly IBulkImportService _service;

        public BulkImportController(IBulkImportService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<BulkImportBatchSummaryDto>>> GetBatches([FromQuery] string? entityName = "Article", [FromQuery] int take = 20, CancellationToken ct = default)
        {
            return Ok(await _service.GetBatchesAsync(entityName, take, ct));
        }

        [HttpPost("template")]
        public async Task<IActionResult> GenerateTemplate([FromBody] BulkImportTemplateRequest request, CancellationToken ct)
        {
            var file = await _service.GenerateTemplateAsync(request, ct);
            return File(file.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(25_000_000)]
        public async Task<ActionResult<BulkImportBatchDetailDto>> Upload([FromForm] IFormFile file, [FromForm] string? sourceType, [FromForm] string? notes, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("Debes seleccionar un archivo Excel o CSV.");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var userId = User?.Identity?.Name ?? "system";
                var detail = await _service.CreateBatchFromFileAsync(stream, file.FileName, sourceType ?? "Excel", notes, userId, ct);
                return Ok(detail);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude crear el lote en staging.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{batchId:int}")]
        public async Task<ActionResult<BulkImportBatchDetailDto>> GetBatch(int batchId, [FromQuery] int previewRows = 25, CancellationToken ct = default)
        {
            try
            {
                var detail = await _service.GetBatchAsync(batchId, previewRows, ct);
                return detail is null ? NotFound() : Ok(detail);
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude abrir el lote.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("external-article")]
        public async Task<ActionResult<BulkImportActionResultDto>> CreateFromExternalArticle([FromBody] ExternalArticleImportRequest request, CancellationToken ct)
        {
            try
            {
                var userId = User?.Identity?.Name ?? "system";
                return Ok(await _service.CreateBatchFromExternalArticleAsync(request, userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude crear el lote externo en staging.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("external-articles")]
        public async Task<ActionResult<BulkImportActionResultDto>> CreateFromExternalArticles([FromBody] ExternalArticlesImportRequest request, CancellationToken ct)
        {
            try
            {
                var userId = User?.Identity?.Name ?? "system";
                return Ok(await _service.CreateBatchFromExternalArticlesAsync(request, userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude crear el lote externo múltiple en staging.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{batchId:int}/rows/{rowId:int}")]
        public async Task<ActionResult<BulkImportActionResultDto>> CorrectRow(int batchId, int rowId, [FromBody] BulkImportRowCorrectionRequest request, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.CorrectRowAsync(batchId, rowId, request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude corregir la fila del staging.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{batchId:int}/validate")]
        public async Task<ActionResult<BulkImportActionResultDto>> Validate(int batchId, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.ValidateBatchAsync(batchId, ct));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{batchId:int}/process")]
        [Authorize(Policy = AppPolicies.WorkflowProcess)]
        public async Task<ActionResult<BulkImportActionResultDto>> Process(int batchId, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.ProcessBatchAsync(batchId, ct));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
