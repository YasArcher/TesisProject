using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public abstract class UnifiedCatalogControllerBase<TEntity> : ControllerBase
        where TEntity : CatalogEntityBase, new()
    {
        private readonly IUnifiedCatalogCrudService<TEntity> _service;

        protected UnifiedCatalogControllerBase(IUnifiedCatalogCrudService<TEntity> service)
        {
            _service = service;
        }

        // ====================== LIST ======================

        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<CatalogListItemDTO>>>> List(
            CancellationToken ct = default)
            => (await _service.ListAsync(ct)).ToActionResult();

        // ====================== GET BY ID ======================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<CatalogDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // ====================== CREATE ======================

        [HttpPost]
        public async Task<ActionResult<ServiceResult<CatalogDetailDTO>>> Create(
            [FromBody] AddCatalogRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // ====================== UPDATE ======================

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<CatalogDetailDTO>>> Update(
            int id,
            [FromBody] UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }

        // ====================== DELETE ======================

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct = default)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}