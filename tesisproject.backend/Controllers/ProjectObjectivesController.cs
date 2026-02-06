using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectObjectivesController : ControllerBase
    {
        private readonly IProjectObjectiveService _service;

        public ProjectObjectivesController(IProjectObjectiveService service)
            => _service = service;

        // GET: api/projectobjectives/by-project/{projectId}
        // 👉 Listado "ligero" (sin actividades)
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectObjectiveListItemDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct = default)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        // GET: api/projectobjectives/by-project/{projectId}/with-activities
        // 👉 Listado general (con actividades) - SIN visita
        [HttpGet("by-project/{projectId:int}/with-activities")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>>> GetByProjectWithActivities(
            int projectId,
            CancellationToken ct = default)
            => (await _service.GetByProjectWithActivitiesAsync(projectId, ct)).ToActionResult();
        // o: (await _service.ListByProjectWithActivitiesAsync(projectId, ct)) según el nombre real

        // GET: api/projectobjectives/by-visit/{projectId}/{visitId}/with-activities
        // 👉 Listado con actividades en contexto de visita (si lo sigues usando)
        [HttpGet("by-visit/{projectId:int}/{visitId:int}/with-activities")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>>> GetByProjectWithActivitiesByVisit(
            int projectId,
            int visitId,
            CancellationToken ct = default)
            => (await _service.ListByProjectWithActivitiesAsync(projectId, visitId, ct)).ToActionResult();

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