using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/catalog/research-category-types")]
    public class ResearchCategoryTypeController : ControllerBase
    {
        private readonly IResearchCategoryTypeService _service;

        public ResearchCategoryTypeController(IResearchCategoryTypeService service)
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
            var result = await _service.ListAsync(onlyActives);
            return Ok(result);
        }

        // ============================
        //         GET BY ID
        // ============================
        // GET: api/catalog/research-category-types/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ResearchCategoryTypeDetailDTO>>> GetByIdAsync(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
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

                return Ok(ServiceResult<int>.Fail("Validation error", ErrorType.Validation, errors));
            }

            var result = await _service.CreateAsync(dto);
            return Ok(result);
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

                return Ok(ServiceResult<bool>.Fail("Validation error", ErrorType.Validation, errors));
            }

            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        // ============================
        //           DELETE
        // ============================
        // DELETE: api/catalog/research-category-types/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> DeleteAsync(int id)
        {
            var result = await _service.DeleteAsync(id);
            return Ok(result);
        }
    }
}