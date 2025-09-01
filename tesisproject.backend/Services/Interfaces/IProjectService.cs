using tesisproject.shared.DTOs.Project;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllAsync();
        Task<Project?> GetByIdAsync(Guid id);
        Task<Project> CreateAsync(Project project);
        Task<bool> UpdateAsync(string id, Project project);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<ProjectListItemDto>> GetListAsync();
    }
}
