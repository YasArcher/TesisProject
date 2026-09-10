using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.Country.Request;
using tesisproject.shared.DTOs.Catalog.Country.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/countries")]
    public class UnifiedCountriesController : ControllerBase
    {
        private readonly IUnifiedCountryService _service;

        public UnifiedCountriesController(IUnifiedCountryService service)
        {
            _service = service;
        }

        // GET: api/countries
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<CountryListItemDTO>>>> List(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
        {
            return (await _service.ListAsync(onlyActives, ct)).ToActionResult();
        }

        // GET: api/countries/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<CountryDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
        {
            return (await _service.GetByIdAsync(id, ct)).ToActionResult();
        }

        // GET: api/countries/keyvalues
        [HttpGet("keyvalues")]
        public async Task<ActionResult<ServiceResult<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
        {
            return (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();
        }

        // POST: api/countries
        [HttpPost]
        public async Task<ActionResult<ServiceResult<CountryDetailDTO>>> Create(
            AddCountryRequestDTO body,
            CancellationToken ct = default)
        {
            return (await _service.CreateAsync(body, ct)).ToActionResult();
        }

        // PUT: api/countries/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<CountryDetailDTO>>> Update(
            int id,
            UpdateCountryRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }
    }
}