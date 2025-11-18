using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductType.Request;
using tesisproject.shared.DTOs.Catalog.ProductType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTypesController : ControllerBase
    {
        private readonly IProductTypeService _service;
        public ProductTypesController(IProductTypeService service) => _service = service;

        // ============================
        //        BASIC CRUD
        // ============================

        // GET: api/producttypes
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductTypeListItemResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.ListAsync(ct)).ToActionResult();

        // GET: api/producttypes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProductTypeListItemResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // POST: api/producttypes
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductTypeListItemResponseDTO>>> Create(ProductTypeCreateRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PUT: api/producttypes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProductTypeListItemResponseDTO>>> Update(int id, ProductTypeUpdateRequestDTO body, CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // DELETE: api/producttypes/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        // ============================
        //     WITH DEFINITIONS
        // ============================

        // GET: api/producttypes/{id}/definitions
        [HttpGet("{id:int}/definitions")]
        public async Task<ActionResult<ApiResponse<ProductTypeWithDefinitionsResponseDTO>>> GetByIdWithDefinitions(int id, CancellationToken ct)
            => (await _service.GetByIdWithDefinitionsAsync(id, ct)).ToActionResult();

        // POST: api/producttypes/with-definitions
        [HttpPost("with-definitions")]
        public async Task<ActionResult<ApiResponse<ProductTypeWithDefinitionsResponseDTO>>> CreateWithDefinitions(ProductTypeWithDefinitionsCreateRequestDTO body, CancellationToken ct)
            => (await _service.CreateWithDefinitionsAsync(body, ct)).ToActionResult();

        // PUT: api/producttypes/{id}/definitions
        [HttpPut("{id:int}/definitions")]
        public async Task<ActionResult<ApiResponse<ProductTypeWithDefinitionsResponseDTO>>> UpdateWithDefinitions(int id, ProductTypeWithDefinitionsUpdateRequestDTO body, CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateWithDefinitionsAsync(body, ct)).ToActionResult();
        }
    }
}