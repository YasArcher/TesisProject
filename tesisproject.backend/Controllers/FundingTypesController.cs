using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FundingTypesController : ControllerBase
    {
        private readonly IFundingTypeService _service;

        public FundingTypesController(IFundingTypeService service)
            => _service = service;

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FundingTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives,
            CancellationToken ct)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<FundingTypeListItemDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ApiResponse<FundingTypeListItemDTO>>> Create(
            [FromBody] AddFundingTypeRequestDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<FundingTypeListItemDTO>>> Update(
            int id,
            [FromBody] UpdateFundingTypeRequestDTO body,
            CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        [HttpGet("key-values")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
    }
}