using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class CatalogControllerBase<TEntity> : ControllerBase
        where TEntity : CatalogEntityBase, new()
    {
        private readonly ICatalogCrudService<TEntity> _service;

        protected CatalogControllerBase(ICatalogCrudService<TEntity> service)
        {
            _service = service;
        }

        // ====================== LIST ======================

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogListItemDTO>>>> List(
            CancellationToken ct = default)
            => (await _service.ListAsync(ct)).ToActionResult();


        // ====================== GET BY ID ======================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<CatalogDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();


        // ====================== CREATE ======================

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CatalogDetailDTO>>> Create(
            [FromBody] AddCatalogRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();


        // ====================== UPDATE ======================

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<CatalogDetailDTO>>> Update(
            int id,
            [FromBody] UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }


        // ====================== DELETE ======================

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}