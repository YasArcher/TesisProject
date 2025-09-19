using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/external/[controller]")]
    [Produces("application/json")]
    public class ExternalUsersController : Controller
    {
        private readonly IExternalUsersService _service;
        public ExternalUsersController(IExternalUsersService service) => _service = service;

        // GET: api/ExternalUsers
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ExternalUserDTO>>>> GetAll(CancellationToken ct)
            => (await _service.GetAllAsync(ct)).ToActionResult();

        /*   // GET: api/ExternalUsers/{id}
         [HttpGet("{id:int}")]
         public async Task<ActionResult<ApiResponse<ExternalUserDTO>>> GetById(int id, CancellationToken ct)
             => (await _service.GetByIdAsync(id, ct)).ToActionResult();*/

    }
}
