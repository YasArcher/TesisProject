using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class VisitObjectiveActivityProgressesController : ControllerBase
    {
        private readonly IVisitObjectiveActivityProgressService _service;

        public VisitObjectiveActivityProgressesController(IVisitObjectiveActivityProgressService service)
            => _service = service;

        // PUT: api/visitobjectiveactivityprogresses
        // Guarda/actualiza el progreso de UNA actividad en UNA visita (UPSERT unitario)
        [HttpPut]
        public async Task<ActionResult<ApiResponse<VisitObjectiveActivityProgressSingleResponseDTO>>> UpsertSingle(
            [FromBody] UpsertSingleVisitObjectiveActivityProgressRequestDTO body,
            CancellationToken ct)
            => (await _service.UpsertSingleAsync(body, ct)).ToActionResult();

        // DELETE: api/visitobjectiveactivityprogresses/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

    }
}