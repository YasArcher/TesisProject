using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/projectorigintypes")]
    public sealed class UnifiedProjectOriginTypesController : UnifiedCatalogControllerBase<ProjectOriginType>
    {
        public UnifiedProjectOriginTypesController(
            IUnifiedCatalogCrudService<ProjectOriginType> service)
            : base(service)
        {
        }
    }
}