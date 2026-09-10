using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/producttypes")]
    public sealed class UnifiedProductTypesController : UnifiedCatalogControllerBase<ProductType>
    {
        public UnifiedProductTypesController(
            IUnifiedCatalogCrudService<ProductType> service)
            : base(service)
        {
        }
    }
}
