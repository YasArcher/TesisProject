using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResearchCategoryGroupsController : ControllerBase
    {
        private readonly IResearchCategoryGroupService _service;

        public ResearchCategoryGroupsController(IResearchCategoryGroupService service)
            => _service = service;

        // ================= READS =================

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ResearchCategoryGroupListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives,
            CancellationToken ct)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ResearchCategoryGroupListItemDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // ================ WRITES ================

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ResearchCategoryGroupListItemDTO>>> Create(
            [FromBody] AddResearchCategoryGroupDTO body,
            CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ResearchCategoryGroupListItemDTO>>> Update(
            int id,
            [FromBody] UpdateResearchCategoryGroupDTO body,
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