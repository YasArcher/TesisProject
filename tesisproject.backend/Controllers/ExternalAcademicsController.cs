using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/external/[controller]")]
    [Produces("application/json")]
    public class ExternalAcademicsController
    {
        private readonly IExternalAcademicsService _service;

        public ExternalAcademicsController(IExternalAcademicsService service) => _service = service;

        // GET: api/external/ExternalAcademics/faculties
        [HttpGet("faculties")]
        public async Task<ActionResult<ServiceResult<List<ExternalFacultyDTO>>>> GetFacultiesWithPrograms(
            CancellationToken ct)
            => (await _service.GetFacultiesWithProgramsAsync(ct)).ToActionResult();
    }
}