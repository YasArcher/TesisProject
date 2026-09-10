using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/catalog/research-category-types")]
    public class UnifiedResearchCategoryTypeController : ControllerBase
    {
        private readonly IUnifiedResearchCategoryTypeService _service;

        public UnifiedResearchCategoryTypeController(IUnifiedResearchCategoryTypeService service)
        {
            _service = service;
        }

        // ============================
        //           LIST
        // ============================
        // GET: api/catalog/research-category-types?onlyActives=true
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ResearchCategoryTypeListItemDTO>>>> ListAsync(
            [FromQuery] bool onlyActives = true)
        {
            var result = await _service.ListAsync(onlyActives, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        // ============================
        //         GET BY ID
        // ============================
        // GET: api/catalog/research-category-types/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ResearchCategoryTypeDetailDTO>>> GetByIdAsync(int id)
        {
            var result = await _service.GetByIdAsync(id, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        // ============================
        //           CREATE
        // ============================
        // POST: api/catalog/research-category-types
        [HttpPost]
        public async Task<ActionResult<ServiceResult<int>>> CreateAsync(
            [FromBody] ResearchCategoryTypeCreateRequestDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var validationResult = new ServiceResult<int>
                {
                    Success = false,
                    Message = "Validation error",
                    Error = ErrorType.Validation,
                    ValidationErrors = errors
                };

                return validationResult.ToActionResult();
            }

            var result = await _service.CreateAsync(dto, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        // ============================
        //           UPDATE
        // ============================
        // PUT: api/catalog/research-category-types/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> UpdateAsync(
            int id,
            [FromBody] ResearchCategoryTypeUpdateRequestDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var validationResult = new ServiceResult<bool>
                {
                    Success = false,
                    Message = "Validation error",
                    Error = ErrorType.Validation,
                    ValidationErrors = errors
                };

                return validationResult.ToActionResult();
            }

            var result = await _service.UpdateAsync(id, dto, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        // ============================
        //           DELETE
        // ============================
        // DELETE: api/catalog/research-category-types/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> DeleteAsync(int id)
        {
            var result = await _service.DeleteAsync(id, HttpContext.RequestAborted);
            return result.ToActionResult();
        }
    }
}