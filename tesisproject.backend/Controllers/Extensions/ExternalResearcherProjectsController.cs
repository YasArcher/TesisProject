using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Extensions
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalResearcherProjectsController : ControllerBase
    {
        private readonly IExternalResearcherProjectService _service;

        public ExternalResearcherProjectsController(IExternalResearcherProjectService service)
        {
            _service = service;
        }

        // GET: api/externalresearcherprojects/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExternalResearcherProjectListItemDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct)
        {
            return (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();
        }

        // GET: api/externalresearcherprojects/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExternalResearcherProjectDetailDTO>>> GetById(
            int id,
            CancellationToken ct)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // POST: api/externalresearcherprojects
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ExternalResearcherProjectDetailDTO>>> Create(
            ExternalResearcherProjectCreateRequestDTO body,
            CancellationToken ct)
        {
            // 1) Obtener el ID del usuario desde la cookie/JWT
            var userId = User.GetUserId(); // <-- extensión que ya usas

            if (userId is null)
                return Unauthorized(ApiResponse<ExternalResearcherProjectDetailDTO>
                    .Fail("User not authenticated."));

            // 3) Llamar al servicio ya con el ID correcto
            return (await _service.CreateAsync(body, userId.Value, ct)).ToActionResult();
        }


        // PUT: api/externalresearcherprojects/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExternalResearcherProjectDetailDTO>>> Update(
            int id,
            ExternalResearcherProjectUpdateRequestDTO body,
            CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}