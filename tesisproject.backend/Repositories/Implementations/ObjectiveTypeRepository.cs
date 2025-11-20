using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for ObjectiveType catalog.
    /// Delegates common behaviors to CatalogRepository<ObjectiveType>.
    /// </summary>
    public class ObjectiveTypeRepository : CatalogRepository<ObjectiveType>, IObjectiveTypeRepository
    {
        public ObjectiveTypeRepository(AppDbContext ctx) : base(ctx) { }
    }
}