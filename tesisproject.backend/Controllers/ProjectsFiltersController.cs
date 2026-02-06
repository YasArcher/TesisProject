using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/projects/filters")]
    [Produces("application/json")]
    public sealed class ProjectsFiltersController : ControllerBase
    {
        private readonly IProjectsFiltersService _svc;
        public ProjectsFiltersController(IProjectsFiltersService svc) => _svc = svc;

        /// <summary>
        /// Bootstrap de filtros para Projects.
        /// Usa ?include=states,types,extensionTypes para cargar secciones específicas.
        /// </summary>
        /// <param name="include">Secciones a incluir (coma separadas)</param>
        /// <param name="ct">CancellationToken</param>
        [HttpGet("bootstrap")]
        public async Task<ActionResult<ApiResponse<ProjectsFilterBootstrapDTO>>> Bootstrap(
            CancellationToken ct = default)
            => (await _svc.GetBootstrapAsync(ct)).ToActionResult();
    }
}
