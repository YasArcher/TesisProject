using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/researchcategories")]
    public class UnifiedResearchCategoriesController : ControllerBase
    {
        private readonly IUnifiedResearchCategoryService _service;

        public UnifiedResearchCategoriesController(IUnifiedResearchCategoryService service)
            => _service = service;

        // ================ READS ================

        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ResearchCategoryListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        [HttpGet("tree")]
        public async Task<ActionResult<ServiceResult<List<ResearchCategoryTreeItemDTO>>>> GetTree(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.GetTreeAsync(onlyActives, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ResearchCategoryDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // ================ WRITES ================

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ResearchCategoryDetailDTO>>> Create(
            [FromBody] AddResearchCategoryRequestDTO body,
            CancellationToken ct = default)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ResearchCategoryDetailDTO>>> Update(
            int id,
            [FromBody] UpdateResearchCategoryRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}