using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductTypeDesignController : ControllerBase
    {
        private readonly IProductTypeDesignService _service;

        public ProductTypeDesignController(IProductTypeDesignService service)
            => _service = service;

        // ======================================================
        // GET: api/producttypedesign
        // GET: api/producttypedesign/5
        //
        // - Sin id  => devuelve template para crear un nuevo tipo
        // - Con id  => devuelve el diseño del tipo existente
        // ======================================================

        [HttpGet]
        [HttpGet("{productTypeId:int}")]
        public async Task<ActionResult<ServiceResult<ProductTypeDesignDetailDTO>>> Get(
            int? productTypeId,
            CancellationToken ct)
        {
            var result = await _service.GetDesignAsync(productTypeId, ct);
            return result.ToActionResult();
        }

        // ======================================================
        // POST: api/producttypedesign
        //
        // Upsert:
        //   - ProductTypeUpsertDTO.Id == 0  => crea nuevo tipo
        //   - ProductTypeUpsertDTO.Id > 0   => actualiza tipo existente
        //
        // También crea/actualiza atributos y definiciones.
        // ======================================================

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ProductTypeDesignDetailDTO>>> Save(
            [FromBody] SaveProductTypeDesignRequestDTO request,
            CancellationToken ct)
        {
            var result = await _service.SaveDesignAsync(request, ct);
            return result.ToActionResult();
        }
    }
}