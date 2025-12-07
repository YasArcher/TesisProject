using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class FundingTypesController : CatalogControllerBase<FundingType>
    {
        public FundingTypesController(
            ICatalogCrudService<FundingType> service)
            : base(service)
        {
        }
    }
}
