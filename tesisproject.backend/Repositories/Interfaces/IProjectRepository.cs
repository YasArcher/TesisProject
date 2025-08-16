using tesisproject.shared.DTOs.Project;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        Task<IEnumerable<Project>> GetByTypeAsync(Guid projectTypeId);
        Task<IEnumerable<ProjectListItemDto>> GetProjectListAsync();
    }
}
