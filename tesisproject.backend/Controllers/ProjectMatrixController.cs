using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/matrix/projects")]
    public class ProjectMatrixController : ControllerBase
    {
        private readonly IProjectMatrixService _projectMatrixService;
        private readonly IProjectFlatReportService _service;

        public ProjectMatrixController(
            IProjectMatrixService projectMatrixService,
            IProjectFlatReportService service)
        {
            _projectMatrixService = projectMatrixService;
            _service = service;
        }

        /// <summary>
        /// Sube un archivo de matriz de proyectos (Excel/CSV) y devuelve un resumen inicial.
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<ProjectMatrixUploadSummaryDTO>>> UploadAsync(
            IFormFile? file,
            CancellationToken ct)
        {
            // Intentar obtener usuario autenticado (misma lógica que en ProjectsController)
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(ApiResponse<ProjectMatrixUploadSummaryDTO>.Fail(
                    "User is not authenticated."
                ));
            }

            if (file is null || file.Length == 0)
            {
                return BadRequest(ApiResponse<ProjectMatrixUploadSummaryDTO>.Fail(
                    "Excel file is required."
                ));
            }

            await using var stream = file.OpenReadStream();

            var result = await _projectMatrixService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                userId.Value,
                ct);

            // ServiceResult<ProjectMatrixUploadSummaryDTO> → ApiResponse<ProjectMatrixUploadSummaryDTO>
            return result.ToActionResult();
        }

        /// <summary>
        /// Returns a flat report of all projects with aggregated details.
        /// </summary>
        [HttpGet("flat")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectFlatReportDTO>>>> GetFlatReport(
            CancellationToken ct)
        {
            var result = await _service.GetFlatReportAsync(null,ct);

            return result.ToActionResult();
        }
    }
}
