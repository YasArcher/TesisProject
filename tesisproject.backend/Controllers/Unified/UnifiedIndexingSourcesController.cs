using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/indexingsources")]
    public class UnifiedIndexingSourcesController : ControllerBase
    {
        private readonly IUnifiedIndexingSourceService _service;

        public UnifiedIndexingSourcesController(IUnifiedIndexingSourceService service)
        {
            _service = service;
        }

        // GET: api/indexingsources
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<IndexingSourceListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            return (await _service.ListAsync(onlyActives, ct)).ToActionResult();
        }

        // GET: api/indexingsources/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<IndexingSourceListItemDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // GET: api/indexingsources/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ServiceResult<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            return (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
        }

        // POST: api/indexingsources
        [HttpPost]
        public async Task<ActionResult<ServiceResult<IndexingSourceListItemDTO>>> Create(
            IndexingSourceCreateRequestDTO body,
            CancellationToken ct = default)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/indexingsources/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<IndexingSourceListItemDTO>>> Update(
            int id,
            IndexingSourceUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}