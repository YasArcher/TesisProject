using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectResearchCategoryRepository : IGenericRepository<ProjectResearchCategory>
    {
        // Por ahora no necesitas métodos extra.
        // Si luego quieres "GetByProjectId", etc., lo agregas aquí.
    }
}