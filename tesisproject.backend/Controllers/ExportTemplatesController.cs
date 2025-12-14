using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportTemplatesController : ControllerBase
    {
        private readonly IExportTemplateService _service;
        private readonly IExportTemplateExcelService _excelService;
        private readonly IMatrixExcelExportService _matrixExcelService;
        private readonly IMatrixTemplateExcelExportService _matrixTemplateExcelService; // <- NUEVO

        public ExportTemplatesController(
            IExportTemplateService service,
            IExportTemplateExcelService excelService,
            IMatrixExcelExportService matrixExcelService,
            IMatrixTemplateExcelExportService matrixTemplateExcelService) // <- NUEVO
        {
            _service = service;
            _excelService = excelService;
            _matrixExcelService = matrixExcelService;
            _matrixTemplateExcelService = matrixTemplateExcelService;     // <- NUEVO
        }

        // =========================
        //       FIELDS (GET)
        // =========================
        // GET: api/ExportTemplates/fields
        [HttpGet("fields")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExportFieldListItemDTO>>>> GetFields(
            CancellationToken ct)
            => (await _service.ListFieldsAsync(ct)).ToActionResult();

        // =========================
        //      TEMPLATES CRUD
        // =========================

        // GET: api/ExportTemplates
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExportTemplateListItemDTO>>>> GetTemplates(
            CancellationToken ct)
            => (await _service.ListTemplatesAsync(ct)).ToActionResult();

        // GET: api/ExportTemplates/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExportTemplateDetailDTO>>> GetTemplate(
            int id,
            CancellationToken ct)
            => (await _service.GetTemplateAsync(id, ct)).ToActionResult();

        // POST: api/ExportTemplates
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ExportTemplateDetailDTO>>> CreateTemplate(
            [FromBody] ExportTemplateCreateRequestDTO body,
            CancellationToken ct)
        {
            var result = await _service.CreateTemplateAsync(body, ct);

            if (!result.Success || result.Data is null)
                return result.ToActionResult();

            var response = ApiResponse<ExportTemplateDetailDTO>.Ok(
                result.Data,
                "Template created.");

            return CreatedAtAction(
                nameof(GetTemplate),
                new { id = result.Data.Id },
                response);
        }

        // PUT: api/ExportTemplates/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExportTemplateDetailDTO>>> UpdateTemplate(
            int id,
            [FromBody] ExportTemplateUpdateRequestDTO body,
            CancellationToken ct)
            => (await _service.UpdateTemplateAsync(id, body, ct)).ToActionResult();

        // DELETE: api/ExportTemplates/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> DeleteTemplate(
            int id,
            CancellationToken ct)
            => (await _service.DeleteTemplateAsync(id, ct)).ToActionResult();

        //// =========================
        ////   EXPORT DINÁMICO (DTO)
        //// =========================
        //// POST: api/ExportTemplates/excel
        //[HttpPost("excel")]
        //public async Task<IActionResult> ExportToExcel(
        //    [FromBody] ExportRequestDTO request,
        //    CancellationToken ct)
        //{
        //    var result = await _excelService.GenerateExcelAsync(request, ct);

        //    if (!result.Success || result.Data is null)
        //    {
        //        var message = result.Message ?? "Error al generar el archivo Excel.";
        //        return BadRequest(message);
        //    }

        //    var fileName = string.IsNullOrWhiteSpace(request.Name)
        //        ? "reporte_proyectos.xlsx"
        //        : $"{request.Name}.xlsx";

        //    return File(
        //        fileContents: result.Data,
        //        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //        fileDownloadName: fileName);
        //}

        //// =========================
        ////   EXPORT FULL MATRIX
        //// =========================
        //// GET: api/ExportTemplates/excelfull
        //[HttpGet("excelfull")]
        //public async Task<IActionResult> ExportToExcelFull(CancellationToken ct)
        //{
        //    var result = await _matrixExcelService.GenerateExcelAsync(ct);

        //    if (!result.Success || result.Data is null)
        //    {
        //        var message = result.Message ?? "Error al generar el archivo Excel.";
        //        return BadRequest(message);
        //    }

        //    const string fileName = "reporte_proyectos.xlsx";

        //    return File(
        //        fileContents: result.Data,
        //        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //        fileDownloadName: fileName);
        //}

        // =========================
        //   EXPORT MATRIX (DTO)
        // =========================
        // POST: api/ExportTemplates/matrix-excel
        [HttpPost("matrix-excel")]
        public async Task<IActionResult> ExportMatrixToExcel(
            [FromBody] ExportRequestDTO request,
            CancellationToken ct)
        {
            var result = await _matrixTemplateExcelService.GenerateExcelAsync(request, ct);

            if (!result.Success || result.Data is null)
            {
                var message = result.Message ?? "Error al generar el archivo Excel de matriz.";
                return BadRequest(message);
            }

            var fileName = string.IsNullOrWhiteSpace(request.Name)
                ? "matriz_proyectos.xlsx"
                : $"{request.Name}.xlsx";

            return File(
                fileContents: result.Data,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: fileName);
        }
    }
}
