using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductAttributeDefinitionsController : ControllerBase
    {
        private readonly IProductAttributeDefinitionService _service;

        public ProductAttributeDefinitionsController(IProductAttributeDefinitionService service)
            => _service = service;

        // GET: api/productattributedefinitions/by-product-type/1
        [HttpGet("by-product-type/{productTypeId:int}")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ProductAttributeDefinitionListItemDTO>>>> GetByProductType(
            int productTypeId,
            CancellationToken ct)
            => (await _service.ListByProductTypeAsync(productTypeId, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ProductAttributeDefinitionDetailDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ProductAttributeDefinitionDetailDTO>>> Create(
            [FromBody] AddProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct)
            => (await _service.CreateAsync(dto, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ProductAttributeDefinitionDetailDTO>>> Update(
            int id,
            [FromBody] UpdateProductAttributeDefinitionRequestDTO dto,
            CancellationToken ct)
        {
            dto.Id = id;
            return (await _service.UpdateAsync(dto, ct)).ToActionResult();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}