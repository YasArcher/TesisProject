using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectResearchCategory.Request;
using tesisproject.shared.DTOs.ProjectResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectResearchCategoryController : ControllerBase
    {
        private readonly IProjectResearchCategoryService _service;

        public ProjectResearchCategoryController(IProjectResearchCategoryService service)
            => _service = service;

        // ================= READS =================

        /// <summary>
        /// Lista las categorías de investigación asociadas a un proyecto.
        /// </summary>
        [HttpGet("project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<List<ProjectResearchCategoryListItemDTO>>>>
            GetByProject(int projectId, CancellationToken ct)
            => (await _service.ListAsync(projectId, ct)).ToActionResult();

        /// <summary>
        /// Obtiene el detalle de una categoría de investigación vinculada a un proyecto.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectResearchCategoryDetailDTO>>>
            GetById(int id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();


        // ================= WRITES =================

        /// <summary>
        /// Crea una relación entre proyecto y categoría de investigación.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectResearchCategoryDetailDTO>>>
            Create(AddProjectResearchCategoryRequestDTO body, CancellationToken ct)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        /// <summary>
        /// Actualiza la relación entre proyecto y categoría de investigación.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProjectResearchCategoryDetailDTO>>>
            Update(int id, UpdateProjectResearchCategoryRequestDTO body, CancellationToken ct)
        {
            body.Id = id;
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        /// <summary>
        /// Elimina la relación entre proyecto y categoría de investigación.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>>
            Delete(int id, CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();
    }
}