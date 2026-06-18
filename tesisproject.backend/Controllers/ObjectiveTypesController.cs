using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ObjectiveTypesController : CatalogControllerBase<ObjectiveType>
    {
        public ObjectiveTypesController(
            ICatalogCrudService<ObjectiveType> service)
            : base(service)
        {
        }
    }
}
