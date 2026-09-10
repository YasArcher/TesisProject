using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/objectiveactivities")]
    public class UnifiedObjectiveActivitiesController : ControllerBase
    {
        private readonly IUnifiedObjectiveActivityService _service;

        public UnifiedObjectiveActivitiesController(IUnifiedObjectiveActivityService service)
            => _service = service;

        [HttpGet("by-objective/{objectiveId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>>> GetByObjective(
            int objectiveId,
            CancellationToken ct = default)
            => (await _service.ListByObjectiveAsync(objectiveId, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ObjectiveActivityDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ObjectiveActivityDetailDTO>>> Create(
            [FromBody] AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ObjectiveActivityDetailDTO>>> Update(
            int id,
            [FromBody] UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            request.ObjectiveActivityId = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        // PUT: api/objectiveactivities/{id}/progress?visitId=123&value=50
        [HttpPut("{id:int}/progress")]
        public async Task<ActionResult<ServiceResult<bool>>> SetProgress(
            int id,
            [FromQuery] int visitId,
            [FromQuery] int value,
            CancellationToken ct = default)
            => (await _service.SetProgressAsync(visitId, id, value, ct)).ToActionResult();
    }
}