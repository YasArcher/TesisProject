using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Request;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicPeriodsController : ControllerBase
    {
        private readonly IAcademicPeriodService _service;

        public AcademicPeriodsController(IAcademicPeriodService service)
        {
            _service = service;
        }

        // GET: api/academicperiods
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AcademicPeriodListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            return (await _service.ListAsync(onlyActives, ct)).ToActionResult();
        }

        // GET: api/academicperiods/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // GET: api/academicperiods/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            return (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
        }

        // POST: api/academicperiods
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> Create(
            AcademicPeriodCreateRequestDTO body,
            CancellationToken ct = default)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/academicperiods/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> Update(
            int id,
            AcademicPeriodUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}
