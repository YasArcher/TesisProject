using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ProjectResearchCategoryRepository
        : GenericRepository<ProjectResearchCategory>, IProjectResearchCategoryRepository
    {
        public ProjectResearchCategoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}
