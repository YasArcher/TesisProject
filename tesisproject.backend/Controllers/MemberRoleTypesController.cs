using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class MemberRoleTypesController : ControllerBase
    {
        private readonly IMemberRoleTypeService _service;

        public MemberRoleTypesController(IMemberRoleTypeService service)
            => _service = service;

        // GET: api/memberroletypes?onlyActives=true
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        // GET: api/memberroletypes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<MemberRoleTypeDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/memberroletypes/key-values?term=x&take=10
        [HttpGet("key-values")]
        public async Task<ActionResult<ServiceResult<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();

        // POST: api/memberroletypes
        [HttpPost]
        public async Task<ActionResult<ServiceResult<MemberRoleTypeDetailDTO>>> Create(
            [FromBody] AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/memberroletypes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<MemberRoleTypeDetailDTO>>> Update(
            int id,
            [FromBody] UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // Ensure route id wins over body id to avoid mismatches
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }
    }
}