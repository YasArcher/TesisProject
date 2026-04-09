using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
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