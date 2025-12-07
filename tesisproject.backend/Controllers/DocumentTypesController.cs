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
    public sealed class DocumentTypesController : CatalogControllerBase<DocumentType>
    {
        public DocumentTypesController(
            ICatalogCrudService<DocumentType> service)
            : base(service)
        {
        }
    }
}