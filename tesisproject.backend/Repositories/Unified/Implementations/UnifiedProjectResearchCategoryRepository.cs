using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedProjectResearchCategoryRepository
        : GenericRepository<ProjectResearchCategory>, IUnifiedProjectResearchCategoryRepository
    {
        public UnifiedProjectResearchCategoryRepository(UnifiedDideDbContext context) : base(context)
        {
        }
    }
}
