using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _service;
        public VisitsController(IVisitService service) => _service = service;

        // GET: api/visits
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitListResponseDTO>>>> GetAll(CancellationToken ct)
            => (await _service.ListAsync(ct)).ToActionResult();

        // GET: api/visits/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<VisitListResponseDTO>>> GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/visits/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitListResponseDTO>>>> GetByProject(int projectId, CancellationToken ct)
            => (await _service.ListByProjectAsync(projectId, ct)).ToActionResult();

        // POST: api/visits
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VisitListResponseDTO>>> Create(AddVisitRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PUT: api/visits/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<VisitListResponseDTO>>> Update(int id, UpdateVisitRequestDTO body, CancellationToken ct)
        {
            // aseguramos que el id de la ruta prevalezca sobre el body
            body.VisitId = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // DELETE: api/visits/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}
