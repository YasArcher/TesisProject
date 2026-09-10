using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/transactiontypes")]
    public sealed class UnifiedTransactionTypesController : UnifiedCatalogControllerBase<TransactionType>
    {
        public UnifiedTransactionTypesController(
            IUnifiedCatalogCrudService<TransactionType> service)
            : base(service)
        {
        }
    }
}
