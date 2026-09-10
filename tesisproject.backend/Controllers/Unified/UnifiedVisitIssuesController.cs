using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/visitissues")]
    public class UnifiedVisitIssuesController : ControllerBase
    {
        private readonly IUnifiedVisitIssueService _service;

        public UnifiedVisitIssuesController(IUnifiedVisitIssueService service) => _service = service;

        // GET: api/visit-issues/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<VisitIssueResponseDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/visit-issues/by-visit/{visitId}
        [HttpGet("by-visit/{visitId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>>> ListByVisit(
            int visitId,
            CancellationToken ct)
            => (await _service.ListByVisitAsync(visitId, ct)).ToActionResult();

        // POST: api/visit-issues
        [HttpPost]
        public async Task<ActionResult<ServiceResult<VisitIssueResponseDTO>>> Create(
            [FromBody] VisitIssueCreateRequestDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PATCH: api/visit-issues/{id}
        [HttpPatch("{id:int}")]
        public async Task<ActionResult<ServiceResult<VisitIssueResponseDTO>>> Update(
            int id,
            [FromBody] VisitIssueUpdateRequestDTO body,
            CancellationToken ct)
            => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

        // DELETE: api/visit-issues/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}