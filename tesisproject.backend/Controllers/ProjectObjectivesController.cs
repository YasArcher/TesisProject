using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectObjectivesController : ControllerBase
    {
        private readonly IProjectObjectiveService _service;

        public ProjectObjectivesController(IProjectObjectiveService service)
            => _service = service;

        // GET: api/projectobjectives/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectObjectiveListItemDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct = default)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        // GET: api/projectobjectives/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectObjectiveDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // POST: api/projectobjectives
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectObjectiveDetailDTO>>> Create(
            [FromBody] AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/projectobjectives/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectObjectiveDetailDTO>>> Update(
            int id,
            [FromBody] UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            request.Id = id; // route id has priority
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }

        // DELETE: api/projectobjectives/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}