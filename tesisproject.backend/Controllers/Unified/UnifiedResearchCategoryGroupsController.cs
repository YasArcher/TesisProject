using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/researchcategorygroups")]
    public sealed class UnifiedResearchCategoryGroupsController : UnifiedCatalogControllerBase<ResearchCategoryGroup>
    {
        public UnifiedResearchCategoryGroupsController(
            IUnifiedCatalogCrudService<ResearchCategoryGroup> service)
            : base(service)
        {
        }
    }
}
