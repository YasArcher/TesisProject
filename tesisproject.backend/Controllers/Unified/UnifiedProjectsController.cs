using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize]
    [ApiController]
    [Route("api/projects")]
    public class UnifiedProjectsController : ControllerBase
    {
        private readonly IUnifiedProjectService _service;

        public UnifiedProjectsController(IUnifiedProjectService service) => _service = service;

        // ---------------------------------------------------------
        // GET (LECTURA) -> ReadTechArea
        // ---------------------------------------------------------

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet]
        public async Task<ActionResult<ServiceResult<List<ProjectListResponseDTO>>>> GetAll(
            CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ProjectListResponseDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("by-type/{projectTypeId:int}")]
        public async Task<ActionResult<ServiceResult<List<ProjectListResponseDTO>>>> GetByType(
            int projectTypeId,
            CancellationToken ct)
            => (await _service.GetByTypeAsync(projectTypeId, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.ReadTechArea)]
        [HttpGet("detail/{projectId:int}")]
        public async Task<ActionResult<ServiceResult<ProjectDetailResponseDTO>>> GetProjectDetail(
            int projectId,
            CancellationToken ct)
            => (await _service.GetProjectDetailAsync(projectId, ct)).ToActionResult();

        // ---------------------------------------------------------
        // CREATE/UPDATE/DELETE (ESCRITURA) -> WriteTechArea
        // ---------------------------------------------------------

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPost]
        public async Task<ActionResult<ServiceResult<ProjectListResponseDTO>>> Create(
            [FromBody] AddProjectRequestDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(new UnifiedAddProjectRequestDTO
            {
                ProjectName = body.ProjectName,
                ProjectTypeId = body.ProjectTypeId,
                ProjectGroupId = body.ProjectGroupId,
                ResearchCategoryIds = body.ResearchCategoryIds,
                ApprovalDate = body.ApprovalDate,
                StartDate = body.StartDate,
                ProjectCode = body.ProjectCode,
                ProjectStateId = body.ProjectStateId,
                DurationInMonths = body.DurationInMonths,
                ExternalFacultyId = body.FacultyId,
                ConvocationId = body.ConvocationId,
                ProjectOriginTypeId = body.ProjectOriginTypeId
            }, ct)).ForHttpField("externalFacultyId", nameof(body.FacultyId)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPost("full")]
        public async Task<ActionResult<ServiceResult<ProjectDetailResponseDTO>>> CreateFull(
            [FromBody] AddProjectFullRequestDTO request,
            CancellationToken ct)
            => (await _service.CreateFullAsync(request, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Update(
            int id,
            [FromBody] UpdateProjectRequestDTO body,
            CancellationToken ct)
            => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        [Authorize(Roles = AppRoles.WriteTechArea)]
        [HttpPut("{projectId:int}/research-categories")]
        public async Task<ActionResult<ServiceResult<NoContent>>> UpdateResearchCategories(
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
