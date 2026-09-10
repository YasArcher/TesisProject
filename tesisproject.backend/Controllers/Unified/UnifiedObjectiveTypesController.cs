using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/objectivetypes")]
    public sealed class UnifiedObjectiveTypesController : UnifiedCatalogControllerBase<ObjectiveType>
    {
        public UnifiedObjectiveTypesController(
            IUnifiedCatalogCrudService<ObjectiveType> service)
            : base(service)
        {
        }
    }
}
