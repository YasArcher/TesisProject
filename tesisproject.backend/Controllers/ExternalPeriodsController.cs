using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/external/[controller]")]
    [Produces("application/json")]
    public class ExternalPeriodsController
    {
        private readonly IExternalPeriodsClient _service;

        public ExternalPeriodsController(IExternalPeriodsClient service)
            => _service = service;

        // GET: api/external/ExternalPeriods
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExternalAcademicPeriodModel>>>> GetAll(
            CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();
    }
}