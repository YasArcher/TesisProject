using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectiveActivitiesController : ControllerBase
    {
        private readonly IObjectiveActivityService _service;

        public ObjectiveActivitiesController(IObjectiveActivityService service)
            => _service = service;

        // GET: api/objectiveactivities/by-objective/{objectiveId}
        [HttpGet("by-objective/{objectiveId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectiveActivityListItemDTO>>>> GetByObjective(
            int objectiveId,
            CancellationToken ct = default)
            => (await _service.ListByObjectiveAsync(objectiveId, ct)).ToActionResult();

        // GET: api/objectiveactivities/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ObjectiveActivityDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // POST: api/objectiveactivities
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ObjectiveActivityDetailDTO>>> Create(
            [FromBody] AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/objectiveactivities/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ObjectiveActivityDetailDTO>>> Update(
            int id,
            [FromBody] UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            request.ObjectiveActivityId = id; // route id has priority
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }

        // DELETE: api/objectiveactivities/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        // PUT: api/objectiveactivities/{id}/completed?value=true
        [HttpPut("{id:int}/completed")]
        public async Task<ActionResult<ApiResponse<bool>>> SetCompleted(
            int id,
            [FromQuery] bool value,
            CancellationToken ct = default)
            => (await _service.SetCompletedAsync(id, value, ct)).ToActionResult();
    }
}