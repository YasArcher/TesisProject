using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;
        public ProjectsController(IProjectService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectListResponseDTO>>> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectListResponseDTO>> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectListResponseDTO>> Create(AddProjectRequestDTO body, CancellationToken ct)
        {
            var created = await _service.CreateAsync(body, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.ProjectId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateProjectRequestDTO body, CancellationToken ct)
        {
            var ok = await _service.UpdateAsync(id, body, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("by-type/{projectTypeId:int}")]
        public async Task<ActionResult<IEnumerable<ProjectListResponseDTO>>> GetByType(int projectTypeId, CancellationToken ct)
            => Ok(await _service.GetByTypeAsync(projectTypeId, ct));
    }
}
