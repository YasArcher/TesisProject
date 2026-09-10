using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/external/externalperiods")]
    [Produces("application/json")]
    public class UnifiedExternalPeriodsController
    {
        private readonly IExternalPeriodsClient _service;

        public UnifiedExternalPeriodsController(IExternalPeriodsClient service)
            => _service = service;

        // GET: api/external/ExternalPeriods
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>>> GetAll(
            CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();
    }
}