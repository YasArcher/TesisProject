using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/visitobjectiveactivityprogresses")]
    public class UnifiedVisitObjectiveActivityProgressesController : ControllerBase
    {
        private readonly IUnifiedVisitObjectiveActivityProgressService _service;

        public UnifiedVisitObjectiveActivityProgressesController(IUnifiedVisitObjectiveActivityProgressService service)
            => _service = service;

        // PUT: api/visitobjectiveactivityprogresses
        // Guarda/actualiza el progreso de UNA actividad en UNA visita (UPSERT unitario)
        [HttpPut]
        public async Task<ActionResult<ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>>> UpsertSingle(
            [FromBody] UpsertSingleVisitObjectiveActivityProgressRequestDTO body,
            CancellationToken ct)
            => (await _service.UpsertSingleAsync(body, ct)).ToActionResult();

        // DELETE: api/visitobjectiveactivityprogresses/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}