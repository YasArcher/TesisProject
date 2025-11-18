using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Request;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectiveActivityUsersController : ControllerBase
    {
        private readonly IObjectiveActivityUserService _service;

        public ObjectiveActivityUsersController(IObjectiveActivityUserService service)
            => _service = service;

        // GET: api/objectiveactivityusers/by-activity/{objectiveActivityId}
        [HttpGet("by-activity/{objectiveActivityId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectiveActivityUserDTO>>>> GetByActivity(
            int objectiveActivityId,
            CancellationToken ct = default)
            => (await _service.ListByActivityAsync(objectiveActivityId, ct)).ToActionResult();

        // POST: api/objectiveactivityusers/assign
        [HttpPost("assign")]
        public async Task<ActionResult<ApiResponse<ObjectiveActivityUserDTO>>> Assign(
            [FromBody] AssignObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default)
            => (await _service.AssignAsync(request, ct)).ToActionResult();

        // PUT: api/objectiveactivityusers/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ObjectiveActivityUserDTO>>> Update(
            int id,
            [FromBody] UpdateObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default)
        {
            request.Id = id; // route id has priority
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }

        // DELETE: api/objectiveactivityusers/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> Unassign(
            int id,
            CancellationToken ct = default)
            => (await _service.UnassignAsync(id, ct)).ToActionResult();
    }
}