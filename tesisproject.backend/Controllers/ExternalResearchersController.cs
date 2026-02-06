using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalResearchersController : ControllerBase
    {
        private readonly IExternalResearcherService _service;

        public ExternalResearchersController(IExternalResearcherService service)
        {
            _service = service;
        }

        // ================================================================
        // GET: api/externalresearchers
        // Optional filters: term, institutionId
        // ================================================================
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExternalResearcherListItemDTO>>>> List(
            [FromQuery] string? term,
            [FromQuery] int? institutionId,
            CancellationToken ct)
        {
            return (await _service.ListAsync(term, institutionId, ct)).ToActionResult();
        }

        // ================================================================
        // GET: api/externalresearchers/{id}
        // ================================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExternalResearcherDetailDTO>>> GetById(
            int id,
            CancellationToken ct)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // ================================================================
        // GET: api/externalresearchers/keyvalues
        // For SelectInput components: id + name
        // ================================================================
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? institutionId,
            [FromQuery] int? take,
            CancellationToken ct)
        {
            return (await _service.GetKeyValuesAsync(term, institutionId, take, ct)).ToActionResult();
        }

        // ================================================================
        // POST: api/externalresearchers
        // ================================================================
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ExternalResearcherDetailDTO>>> Create(
            ExternalResearcherCreateRequestDTO body,
            CancellationToken ct)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // ================================================================
        // PUT: api/externalresearchers/{id}
        // ================================================================
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ExternalResearcherDetailDTO>>> Update(
            int id,
            ExternalResearcherUpdateRequestDTO body,
            CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}