using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Request;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectiveTypesController : ControllerBase
    {
        private readonly IObjectiveTypeService _service;

        public ObjectiveTypesController(IObjectiveTypeService service)
            => _service = service;

        // GET: api/objectivetypes?onlyActives=true
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectiveTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        // GET: api/objectivetypes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ObjectiveTypeDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/objectivetypes/key-values?term=x&take=10
        [HttpGet("key-values")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();

        // POST: api/objectivetypes
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ObjectiveTypeDetailDTO>>> Create(
            [FromBody] AddObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/objectivetypes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ObjectiveTypeDetailDTO>>> Update(
            int id,
            [FromBody] UpdateObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // Ensure route id wins over body id to avoid mismatches
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }
    }
}