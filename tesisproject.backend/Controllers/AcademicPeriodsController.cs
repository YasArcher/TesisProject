using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Request;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicPeriodsController : ControllerBase
    {
        private readonly IExternalPeriodsClient _service;

        public AcademicPeriodsController(IExternalPeriodsClient service)
        {
            _service = service;
        }

        // GET: api/academicperiods
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AcademicPeriodListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            var api = await _service.GetAllAsync(ct);
            if (!api.Success || api.Data is null)
                return ServiceResult<IReadOnlyList<AcademicPeriodListItemDTO>>
                    .Fail(api.Message ?? "Cannot retrieve academic periods.", api.Error)
                    .ToActionResult();

            var list = api.Data
                .Select(MapToListItem)
                .ToList();

            return ServiceResult<IReadOnlyList<AcademicPeriodListItemDTO>>
                .Ok(list, "Academic periods retrieved")
                .ToActionResult();
        }

        // GET: api/academicperiods/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            var api = await _service.GetByIdAsync(id, ct);
            if (!api.Success || api.Data is null)
                return ServiceResult<AcademicPeriodListItemDTO>
                    .Fail(api.Message ?? "Academic period not found.", api.Error)
                    .ToActionResult();

            return ServiceResult<AcademicPeriodListItemDTO>
                .Ok(MapToListItem(api.Data), "Academic period retrieved")
                .ToActionResult();
        }

        // GET: api/academicperiods/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            var api = await _service.GetAllAsync(ct);
            if (!api.Success || api.Data is null)
                return ServiceResult<List<KeyValueItemDTO>>
                    .Fail(api.Message ?? "Cannot retrieve academic periods.", api.Error)
                    .ToActionResult();

            var q = api.Data.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(p => (p.Name ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase));
            }

            q = q.OrderByDescending(p => p.StartDate);

            if (take.HasValue && take.Value > 0)
                q = q.Take(take.Value);

            var list = q.Select(p => new KeyValueItemDTO
            {
                Id = p.PeriodId,
                Name = p.Name
            }).ToList();

            return ServiceResult<List<KeyValueItemDTO>>
                .Ok(list, "Academic periods key-values retrieved")
                .ToActionResult();
        }

        // POST: api/academicperiods
        [HttpPost]
        public Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> Create(
            AcademicPeriodCreateRequestDTO body,
            CancellationToken ct = default)
        {
            return Task.FromResult<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>>(
                StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<AcademicPeriodListItemDTO>
                {
                    Success = false,
                    Message = "AcademicPeriods now comes from external source; create is not supported.",
                    Data = null
                }));
        }

        // PUT: api/academicperiods/{id}
        [HttpPut("{id:int}")]
        public Task<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>> Update(
            int id,
            AcademicPeriodUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            return Task.FromResult<ActionResult<ApiResponse<AcademicPeriodListItemDTO>>>(
                StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<AcademicPeriodListItemDTO>
                {
                    Success = false,
                    Message = "AcademicPeriods now comes from external source; update is not supported.",
                    Data = null
                }));
        }

        private static AcademicPeriodListItemDTO MapToListItem(ExternalAcademicPeriodModel p) => new()
        {
            Id = p.PeriodId,
            Name = p.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate
        };
    }
}
