using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/documentrecognition")]
    public class UnifiedDocumentRecognitionController : ControllerBase
    {
        private readonly IUnifiedDocumentRecognitionService _service;

        public UnifiedDocumentRecognitionController(IUnifiedDocumentRecognitionService service)
        {
            _service = service;
        }

        /// <summary>
        /// Analyzes a resolution document and extracts structured data.
        /// Returns ResolutionInfo with resolution code, dates, budget, etc.
        /// </summary>
        [HttpPost("analyze-resolution")]
        public async Task<ActionResult<ServiceResult<ResolutionInfo>>> AnalyzeResolution(
            IFormFile file,
            CancellationToken ct)
        {
            var result = await _service.RecognizeResolutionAsync(file, ct);
            return result.ToActionResult();
        }

        /// <summary>
        /// Analyzes a DIDE project document and extracts structured data.
        /// Returns DideProjectFormInfo with project name, researchers, objectives, etc.
        /// </summary>
        [HttpPost("analyze-dide-project")]
        public async Task<ActionResult<ServiceResult<DideProjectFormInfo>>> AnalyzeDideProject(
            IFormFile file,
            CancellationToken ct)
        {
            var result = await _service.RecognizeDideProjectAsync(file, ct);
            return result.ToActionResult();
        }
    }
}