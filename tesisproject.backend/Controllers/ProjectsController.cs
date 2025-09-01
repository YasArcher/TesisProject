using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project;
using tesisproject.shared.Entities.Core;

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
        public async Task<ActionResult<IEnumerable<Project>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Project>> GetById(Guid id)
        {
            var p = await _service.GetByIdAsync(id);
            return p is null ? NotFound() : Ok(p);
        }

        [HttpPost]
        public async Task<ActionResult<Project>> Create(Project body)
        {
            var created = await _service.CreateAsync(body);
            return CreatedAtAction(nameof(GetById), new { id = created.ProjectId }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(String id, Project body)
        {
            if (id != body.ProjectId) return BadRequest("Route id and body id must match.");
            var ok = await _service.UpdateAsync(id, body);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<ProjectListItemDto>>> GetList()
            => Ok(await _service.GetListAsync());
    }
}
