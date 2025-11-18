using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class VisitIssuesController : ControllerBase
    {
        private readonly IVisitIssueService _service;
        public VisitIssuesController(IVisitIssueService service) => _service = service;

        // GET: api/visit-issues/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<VisitIssueResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/visit-issues/by-visit/{visitId}
        [HttpGet("by-visit/{visitId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitIssueResponseDTO>>>> ListByVisit(int visitId, CancellationToken ct)
            => (await _service.ListByVisitAsync(visitId, ct)).ToActionResult();

        // POST: api/visit-issues
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VisitIssueResponseDTO>>> Create(VisitIssueCreateRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PATCH: api/visit-issues/{id}
        [HttpPatch("{id:int}")]
        public async Task<ActionResult<ApiResponse<VisitIssueResponseDTO>>> Update(int id, VisitIssueUpdateRequestDTO body, CancellationToken ct)
            => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

        // DELETE: api/visit-issues/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}