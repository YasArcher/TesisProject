using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/external/[controller]")]
    [Produces("application/json")]
    public class ExternalAcademicsController
    {
        private readonly IExternalAcademicsService _service;
        public ExternalAcademicsController(IExternalAcademicsService service) => _service = service;
        // GET: api/ExternalAcademics/faculties
        [HttpGet("faculties")]
        public async Task<ActionResult<ApiResponse<List<ExternalFacultyDTO>>>> GetFacultiesWithPrograms(CancellationToken ct)
            => (await _service.GetFacultiesWithProgramsAsync(ct)).ToActionResult();

    }
}
