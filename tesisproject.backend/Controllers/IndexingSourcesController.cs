using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class IndexingSourcesController : ControllerBase
    {
        private readonly IIndexingSourceService _service;

        public IndexingSourcesController(IIndexingSourceService service)
        {
            _service = service;
        }

        // GET: api/indexingsources
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<IndexingSourceListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            return (await _service.ListAsync(onlyActives, ct)).ToActionResult();
        }

        // GET: api/indexingsources/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<IndexingSourceListItemDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // GET: api/indexingsources/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            return (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
        }

        // POST: api/indexingsources
        [HttpPost]
        public async Task<ActionResult<ApiResponse<IndexingSourceListItemDTO>>> Create(
            IndexingSourceCreateRequestDTO body,
            CancellationToken ct = default)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/indexingsources/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<IndexingSourceListItemDTO>>> Update(
            int id,
            IndexingSourceUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}