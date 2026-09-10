using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/matrix/projects")]
    public class UnifiedProjectMatrixController : ControllerBase
    {
        private readonly IUnifiedProjectMatrixService _projectMatrixService;
        private readonly IUnifiedProjectFlatReportService _service;

        public UnifiedProjectMatrixController(
            IUnifiedProjectMatrixService projectMatrixService,
            IUnifiedProjectFlatReportService service)
        {
            _projectMatrixService = projectMatrixService;
            _service = service;
        }

        /// <summary>
        /// Sube un archivo de matriz de proyectos (Excel/CSV) y devuelve un resumen inicial.
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<ServiceResult<ProjectMatrixUploadSummaryDTO>>> UploadAsync(
            IFormFile? file,
            CancellationToken ct)
        {
            // Intentar obtener usuario autenticado (misma lógica que en ProjectsController)
            var userId = User.GetUserId();

            if (userId is null)
            {
                var authResult = new ServiceResult<ProjectMatrixUploadSummaryDTO>
                {
                    Success = false,
                    Message = "User is not authenticated.",
                    Error = ErrorType.Unauthorized,
                    ErrorCode = "AUTH_USER_NOT_AUTHENTICATED"
                };

                return authResult.ToActionResult();
            }

            if (file is null || file.Length == 0)
            {
                var validationResult = new ServiceResult<ProjectMatrixUploadSummaryDTO>
                {
                    Success = false,
                    Message = "Excel file is required.",
                    Error = ErrorType.Validation,
                    ErrorCode = "MATRIX_FILE_REQUIRED",
                    ValidationErrors = new Dictionary<string, string[]>
                    {
                        ["file"] = new[] { "Excel file is required." }
                    }
                };

                return validationResult.ToActionResult();
            }

            await using var stream = file.OpenReadStream();

            var result = await _projectMatrixService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Returns a flat report of all projects with aggregated details.
        /// </summary>
        [HttpGet("flat")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>>> GetFlatReport(
            CancellationToken ct)
        {
            var result = await _service.GetFlatReportAsync(null, ct);
            return result.ToActionResult();
        }
    }
}