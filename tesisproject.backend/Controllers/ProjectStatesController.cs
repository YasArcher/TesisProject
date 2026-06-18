using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProjectStatesController : CatalogControllerBase<ProjectState>
    {
        public ProjectStatesController(
            ICatalogCrudService<ProjectState> service)
            : base(service)
        {
        }
    }
}