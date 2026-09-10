using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/externalresearcherprojects")]
    public class UnifiedExternalResearcherProjectsController : ControllerBase
    {
        private readonly IUnifiedExternalResearcherProjectService _service;

        public UnifiedExternalResearcherProjectsController(IUnifiedExternalResearcherProjectService service)
        {
            _service = service;
        }

        // GET: api/externalresearcherprojects/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct)
        {
            return (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();
        }

        // GET: api/externalresearcherprojects/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ExternalResearcherProjectDetailDTO>>> GetById(
            int id,
            CancellationToken ct)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // POST: api/externalresearcherprojects
        [HttpPost]
        public async Task<ActionResult<ServiceResult<ExternalResearcherProjectDetailDTO>>> Create(
            [FromBody] ExternalResearcherProjectCreateRequestDTO body,
            CancellationToken ct)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/externalresearcherprojects/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ExternalResearcherProjectDetailDTO>>> Update(
            int id,
            [FromBody] ExternalResearcherProjectUpdateRequestDTO body,
            CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}
