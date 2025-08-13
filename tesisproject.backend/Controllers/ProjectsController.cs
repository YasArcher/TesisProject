using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    // This controller manages project entities.
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        public ProjectsController(IUnitOfWork uow) => _uow = uow;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _uow.Projects.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
            => (await _uow.Projects.GetByIdAsync(id)) is { } p ? Ok(p) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(Project body)
        {
            await _uow.Projects.AddAsync(body);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = body.ProjectId }, body);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Project body)
        {
            if (id != body.ProjectId) return BadRequest();
            _uow.Projects.Update(body);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var entity = await _uow.Projects.GetByIdAsync(id);
            if (entity is null) return NotFound();
            _uow.Projects.Remove(entity);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}
