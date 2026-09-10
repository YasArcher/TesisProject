using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/projectextensiontypes")]
    public sealed class UnifiedProjectExtensionTypesController : UnifiedCatalogControllerBase<ProjectExtensionType>
    {
        public UnifiedProjectExtensionTypesController(
            IUnifiedCatalogCrudService<ProjectExtensionType> service)
            : base(service)
        {
        }
    }
}