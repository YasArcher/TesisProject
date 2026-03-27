using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Utils;
using tesisproject.shared.Auth;

namespace tesisproject.backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;
        public ProjectsController(IProjectService service) => _service = service;

        // ---------------------------------------------------------
        // GET (LECTURA) -> ReadTechArea
        // ---------------------------------------------------------

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProjectListResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectListResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("by-type/{projectTypeId:int}")]
        public async Task<ActionResult<ApiResponse<List<ProjectListResponseDTO>>>> GetByType(int projectTypeId, CancellationToken ct)
            => (await _service.GetByTypeAsync(projectTypeId, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("detail/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<ProjectDetailResponseDTO>>> GetProjectDetail(int projectId, CancellationToken ct)
            => (await _service.GetProjectDetailAsync(projectId, ct)).ToActionResult();

        // ---------------------------------------------------------
        // CREATE/UPDATE/DELETE (ESCRITURA) -> WriteTechArea
        // ---------------------------------------------------------

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectListResponseDTO>>> Create(
            AddProjectRequestDTO body, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponse<ProjectListResponseDTO>.Fail(
                    "User is not authenticated."
                ));
            }

            return (await _service.CreateAsync(body, userId.Value, ct)).ToActionResult();
        }

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPost("full")]
        public async Task<ActionResult<ApiResponse<ProjectDetailResponseDTO>>> CreateFull(
            [FromBody] AddProjectFullRequestDTO request, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponse<ProjectDetailResponseDTO>.Fail(
                    "User is not authenticated."
                ));
            }

            return (await _service.CreateFullAsync(request, userId.Value, ct)).ToActionResult();
        }

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Update(
            int id, UpdateProjectRequestDTO body, CancellationToken ct)
            => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPut("{projectId}/research-categories")]
        public async Task<ActionResult<ApiResponse<NoContent>>> UpdateResearchCategories(
            int projectId,
            [FromBody] UpdateProjectResearchCategoriesRequestDTO request,
            CancellationToken ct)
        {
            var result = await _service.UpdateResearchCategoriesAsync(
                projectId,
                request.ResearchCategoryIds,
                ct);

            return result.ToActionResult();
        }
    }
}