using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Request;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionTypesController : ControllerBase
    {
        private readonly ITransactionTypeService _service;

        public TransactionTypesController(ITransactionTypeService service)
            => _service = service;

        // ============== GET ALL ==============
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TransactionTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives,
            CancellationToken ct)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        // ============== GET BY ID ==============
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<TransactionTypeListItemDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // ============== CREATE ==============
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TransactionTypeListItemDTO>>> Create(
            [FromBody] AddTransactionTypeRequestDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // ============== UPDATE ==============
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<TransactionTypeListItemDTO>>> Update(
            int id,
            [FromBody] UpdateTransactionTypeRequestDTO body,
            CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // ============== KEY VALUES ==============
        [HttpGet("key-values")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
    }
}
