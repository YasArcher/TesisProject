using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedResearchCategoryRepository
        : GenericRepository<ResearchCategory>, IUnifiedResearchCategoryRepository
    {
        public UnifiedResearchCategoryRepository(UnifiedDideDbContext ctx) : base(ctx)
        {
        }
    }
}