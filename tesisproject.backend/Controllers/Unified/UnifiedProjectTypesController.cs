using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/projecttypes")]
    public sealed class UnifiedProjectTypesController : UnifiedCatalogControllerBase<ProjectType>
    {
        public UnifiedProjectTypesController(
            IUnifiedCatalogCrudService<ProjectType> service)
            : base(service)
        {
        }
    }
}
