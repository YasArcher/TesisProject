using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/fundingtypes")]
    public sealed class UnifiedFundingTypesController : UnifiedCatalogControllerBase<FundingType>
    {
        public UnifiedFundingTypesController(
            IUnifiedCatalogCrudService<FundingType> service)
            : base(service)
        {
        }
    }
}
