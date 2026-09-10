using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/external/externalacademics")]
    [Produces("application/json")]
    public class UnifiedExternalAcademicsController
    {
        private readonly IExternalAcademicsService _service;

        public UnifiedExternalAcademicsController(IExternalAcademicsService service) => _service = service;

        // GET: api/external/ExternalAcademics/faculties
        [HttpGet("faculties")]
        public async Task<ActionResult<ServiceResult<List<ExternalFacultyDTO>>>> GetFacultiesWithPrograms(
            CancellationToken ct)
            => (await _service.GetFacultiesWithProgramsAsync(ct)).ToActionResult();
    }
}