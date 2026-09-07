using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProjectResearchCategoryRepository : IGenericRepository<ProjectResearchCategory>
    {
        // Por ahora no necesitas métodos extra.
        // Si luego quieres "GetByProjectId", etc., lo agregas aquí.
    }
}