using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.DocumentType.Request;
using tesisproject.shared.DTOs.Catalog.DocumentType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentTypesController : ControllerBase
    {
        private readonly IDocumentTypeService _service;

        public DocumentTypesController(IDocumentTypeService service)
            => _service = service;

        // GET: api/documenttypes?onlyActives=true
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        // GET: api/documenttypes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<DocumentTypeDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/documenttypes/key-values?term=x&take=10
        [HttpGet("key-values")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();

        // POST: api/documenttypes
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DocumentTypeDetailDTO>>> Create(
            [FromBody] AddDocumentTypeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/documenttypes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<DocumentTypeDetailDTO>>> Update(
            int id,
            [FromBody] UpdateDocumentTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // Ensure route id wins over body id to avoid mismatches
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }
    }
}