using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProjectType.Request;
using tesisproject.shared.DTOs.Catalog.ProjectType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectTypesController : ControllerBase
    {
        private readonly IProjectTypeService _service;

        public ProjectTypesController(IProjectTypeService service)
            => _service = service;

        // GET: api/projecttypes?onlyActives=true
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectTypeListItemDTO>>>> GetAll(
            [FromQuery] bool onlyActives = true,
            CancellationToken ct = default)
            => (await _service.ListAsync(onlyActives, ct)).ToActionResult();

        // GET: api/projecttypes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectTypeDetailDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/projecttypes/key-values?term=x&take=10
        [HttpGet("key-values")]
        public async Task<ActionResult<ApiResponse<List<KeyValueItemDTO>>>> GetKeyValues(
            [FromQuery] string? term,
            [FromQuery] int? take,
            CancellationToken ct = default)
            => (await _service.GetKeyValuesAsync(term, take, ct)).ToActionResult();

        // POST: api/projecttypes
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectTypeDetailDTO>>> Create(
            [FromBody] AddProjectTypeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // PUT: api/projecttypes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectTypeDetailDTO>>> Update(
            int id,
            [FromBody] UpdateProjectTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // Asegurar que el id de la ruta prevalezca
            request.Id = id;
            return (await _service.UpdateAsync(request, ct)).ToActionResult();
        }
    }
}