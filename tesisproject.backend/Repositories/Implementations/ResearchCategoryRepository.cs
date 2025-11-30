using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ResearchCategoryRepository
        : GenericRepository<ResearchCategory>, IResearchCategoryRepository
    {
        public ResearchCategoryRepository(AppDbContext ctx) : base(ctx)
        {
        }
    }
}