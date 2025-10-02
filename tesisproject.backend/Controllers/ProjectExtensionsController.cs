using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectExtensionsController : ControllerBase
    {
        private readonly IProjectExtensionService _service;
        public ProjectExtensionsController(IProjectExtensionService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectExtensionListResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.ListAsync(ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectExtensionListResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectExtensionListResponseDTO>>>> GetByProject(int projectId, CancellationToken ct)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectExtensionListResponseDTO>>> Create(AddProjectExtensionRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectExtensionListResponseDTO>>> Update(int id, UpdateProjectExtensionRequestDTO body, CancellationToken ct)
        {
            body.ProjectExtensionId = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}