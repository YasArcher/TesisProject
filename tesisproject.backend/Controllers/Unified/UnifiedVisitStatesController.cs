using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/visitstates")]
    public sealed class UnifiedVisitStatesController : UnifiedCatalogControllerBase<VisitState>
    {
        public UnifiedVisitStatesController(
            IUnifiedCatalogCrudService<VisitState> service)
            : base(service)
        {
        }
    }
}