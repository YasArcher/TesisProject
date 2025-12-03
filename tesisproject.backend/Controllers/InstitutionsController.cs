using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class InstitutionsController : ControllerBase
    {
        private readonly IInstitutionService _service;

        public InstitutionsController(IInstitutionService service)
        {
            _service = service;
        }

        // GET: api/institutions
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<InstitutionListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            return (await _service.ListAsync(onlyActives, ct)).ToActionResult();
        }

        // GET: api/institutions/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<InstitutionDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // GET: api/institutions/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            return (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
        }

        // POST: api/institutions
        [HttpPost]
        public async Task<ActionResult<ApiResponse<InstitutionDetailDTO>>> Create(
            AddInstitutionRequestDTO body,
            CancellationToken ct = default)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/institutions/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<InstitutionDetailDTO>>> Update(
            int id,
            UpdateInstitutionRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}
