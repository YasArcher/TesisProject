using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Product.Request;
using tesisproject.shared.DTOs.Product.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        public ProductsController(IProductService service) => _service = service;

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.ListAsync(ct)).ToActionResult();

        // GET: api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProductDetailResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/products/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemResponseDTO>>>> GetByProject(int projectId, CancellationToken ct)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductDetailResponseDTO>>> Create(ProductCreateRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PUT: api/products/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProductDetailResponseDTO>>> Update(int id, ProductUpdateRequestDTO body, CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}