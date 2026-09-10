using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/projectstates")]
    public sealed class UnifiedProjectStatesController : UnifiedCatalogControllerBase<ProjectState>
    {
        public UnifiedProjectStatesController(
            IUnifiedCatalogCrudService<ProjectState> service)
            : base(service)
        {
        }
    }
}