using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ExportTemplatesController : ControllerBase
    {
        private readonly IExportTemplateService _service;
        private readonly IMatrixTemplateExcelExportService _matrixTemplateExcelService;

        public ExportTemplatesController(
            IExportTemplateService service,
            IMatrixTemplateExcelExportService matrixTemplateExcelService)
        {
            _service = service;
            _matrixTemplateExcelService = matrixTemplateExcelService;
        }

        // =========================
        //       FIELDS (GET)
        // =========================
        // GET: api/ExportTemplates/fields
        [HttpGet("fields")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ExportFieldListItemDTO>>>> GetFields(
            CancellationToken ct)
            => (await _service.ListFieldsAsync(ct)).ToActionResult();

        // =========================
        //      TEMPLATES CRUD
        // =========================

        // GET: api/ExportTemplates
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ExportTemplateListItemDTO>>>> GetTemplates(
            CancellationToken ct)
            => (await _service.ListTemplatesAsync(ct)).ToActionResult();

        // GET: api/ExportTemplates/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ExportTemplateDetailDTO>>> GetTemplate(
            int id,
            CancellationToken ct)
            => (await _service.GetTemplateAsync(id, ct)).ToActionResult();

        // POST: api/ExportTemplates
        [HttpPost]
        public async Task<ActionResult<ServiceResult<ExportTemplateDetailDTO>>> CreateTemplate(
            [FromBody] ExportTemplateCreateRequestDTO body,
            CancellationToken ct)
        {
            var result = await _service.CreateTemplateAsync(body, ct);

            if (!result.Success || result.Data is null)
                return result.ToActionResult();

            var response = ServiceResult<ExportTemplateDetailDTO>.Ok(
                result.Data,
                "Template created.");

            return CreatedAtAction(
                nameof(GetTemplate),
                new { id = result.Data.Id },
                response);
        }

        // PUT: api/ExportTemplates/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ExportTemplateDetailDTO>>> UpdateTemplate(
            int id,
            [FromBody] ExportTemplateUpdateRequestDTO body,
            CancellationToken ct)
            => (await _service.UpdateTemplateAsync(id, body, ct)).ToActionResult();

        // DELETE: api/ExportTemplates/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> DeleteTemplate(
            int id,
            CancellationToken ct)
            => (await _service.DeleteTemplateAsync(id, ct)).ToActionResult();

        // =========================
        //   EXPORT MATRIX (DTO)
        // =========================
        // POST: api/ExportTemplates/matrix-excel
        [HttpPost("matrix-excel")]
        public async Task<IActionResult> ExportMatrixToExcel(
            [FromBody] ExportByTemplateRequestDTO request,
            CancellationToken ct)
        {
            var result = await _matrixTemplateExcelService.GenerateExcelAsync(request, ct);

            if (!result.Success || result.Data is null)
            {
                var error = ServiceResult<NoContent>.Fail(
                    result.Message ?? "Error al generar el archivo Excel de matriz.",
                    result.Error == ErrorType.None ? ErrorType.Unexpected : result.Error,
                    result.ErrorCode,
                    result.ValidationErrors);

                return error.Error switch
                {
                    ErrorType.NotFound => NotFound(error),
                    ErrorType.Validation => BadRequest(error),
                    ErrorType.Conflict => Conflict(error),
                    ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, error),
                    ErrorType.Unauthorized => Unauthorized(error),
                    _ => BadRequest(error)
                };
            }

            var fileName = string.IsNullOrWhiteSpace(request.NameOverride)
                ? "matriz_proyectos.xlsx"
                : $"{request.NameOverride}.xlsx";

            return File(
                fileContents: result.Data,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: fileName);
        }
    }
}