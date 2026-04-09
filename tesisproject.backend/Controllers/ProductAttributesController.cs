using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductAttributesController : ControllerBase
    {
        private readonly IProductAttributeService _service;

        public ProductAttributesController(IProductAttributeService service)
            => _service = service;

        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ProductAttributeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives,
            CancellationToken ct)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ProductAttributeDetailDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ProductAttributeDetailDTO>>> Create(
            [FromBody] AddProductAttributeRequestDTO dto,
            CancellationToken ct)
            => (await _service.CreateAsync(dto, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ProductAttributeDetailDTO>>> Update(
            int id,
            [FromBody] UpdateProductAttributeRequestDTO dto,
            CancellationToken ct)
        {
            dto.Id = id;
            return (await _service.UpdateAsync(dto, ct)).ToActionResult();
        }

        // ================= DELETE =================

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}