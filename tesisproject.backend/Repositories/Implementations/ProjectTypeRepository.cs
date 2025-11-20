using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectTypeRepository : CatalogRepository<ProjectType>, IProjectTypeRepository
    {
        public ProjectTypeRepository(AppDbContext ctx) : base(ctx) { }
    }
}
