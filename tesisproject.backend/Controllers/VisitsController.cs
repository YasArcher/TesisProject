using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _service;

        public VisitsController(IVisitService service) => _service = service;

        // GET: api/visits
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<VisitListResponseDTO>>>> GetAll(
            CancellationToken ct)
            => (await _service.ListAsync(ct)).ToActionResult();

        // GET: api/visits/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<VisitListResponseDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/visits/{id}/detail
        [HttpGet("{id:int}/detail")]
        public async Task<ActionResult<ServiceResult<VisitDetailResponseDTO>>> GetDetail(
            int id,
            CancellationToken ct)
            => (await _service.GetVisitDetailAsync(id, ct)).ToActionResult();

        // GET: api/visits/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<VisitListResponseDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        // GET: api/visits/by-state/{visitStateId}
        [HttpGet("by-state/{visitStateId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>>> GetByState(
            int visitStateId,
            CancellationToken ct)
            => (await _service.ListByStateAsync(visitStateId, ct)).ToActionResult();

        // POST: api/visits
        [HttpPost]
        public async Task<ActionResult<ServiceResult<VisitListResponseDTO>>> Create(
            AddVisitRequestDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PUT: api/visits/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<VisitListResponseDTO>>> Update(
            int id,
            UpdateVisitRequestDTO body,
            CancellationToken ct)
        {
            body.VisitId = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // DELETE: api/visits/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        // PUT: api/visits/{id}/finalize
        [HttpPut("{id:int}/finalize")]
        public async Task<ActionResult<ServiceResult<VisitListResponseDTO>>> Finalize(
            int id,
            [FromBody] FinalizeVisitRequestDTO body,
            CancellationToken ct)
        {
            body.VisitId = id;
            return (await _service.FinalizeAsync(body, ct)).ToActionResult();
        }

        // PUT: api/visits/bulk/schedule
        [HttpPut("bulk/schedule")]
        public async Task<ActionResult<ServiceResult<NoContent>>> BulkSchedule(
            [FromBody] BulkScheduleVisitsRequestDTO request,
            CancellationToken ct)
            => (await _service.BulkScheduleAsync(request, ct)).ToActionResult();

        // GET: api/visits/planned-for-execution?executionDate=2026-02-03
        [HttpGet("planned-for-execution")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>>> GetPlannedForExecution(
            [FromQuery] DateOnly? executionDate,
            CancellationToken ct)
            => (await _service.ListPlannedForExecutionAsync(executionDate, ct)).ToActionResult();
    }
}