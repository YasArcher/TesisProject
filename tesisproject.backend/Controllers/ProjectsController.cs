using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;
using tesisproject.backend.Controllers.Extensions;

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
        public async Task<ActionResult<ApiResponse<List<ProjectListResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectListResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectListResponseDTO>>> Create(AddProjectRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Update(int id, UpdateProjectRequestDTO body, CancellationToken ct)
            => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        [HttpGet("by-type/{projectTypeId:int}")]
        public async Task<ActionResult<ApiResponse<List<ProjectListResponseDTO>>>> GetByType(int projectTypeId, CancellationToken ct)
            => (await _service.GetByTypeAsync(projectTypeId, ct)).ToActionResult();
    }
}
