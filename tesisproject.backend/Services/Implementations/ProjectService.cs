using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;
        public ProjectService(IUnitOfWork uow) => _uow = uow;

        public Task<IEnumerable<Project>> GetAllAsync()
            => _uow.Projects.GetAllAsync();

        public Task<Project?> GetByIdAsync(Guid id)
            => _uow.Projects.GetByIdAsync(id);

        public async Task<Project> CreateAsync(Project project)
        {
            await _uow.Projects.AddAsync(project);
            await _uow.SaveChangesAsync();
            return project;
        }

        public async Task<bool> UpdateAsync(string id, Project project)
        {
            var current = await _uow.Projects.GetByIdAsync(id);
            if (current is null) return false;

            // En FASE 1 mantenemos update simple:
            _uow.Projects.Update(project);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var current = await _uow.Projects.GetByIdAsync(id);
            if (current is null) return false;

            _uow.Projects.Remove(current);
            await _uow.SaveChangesAsync();
            return true;
        }

        public Task<IEnumerable<ProjectListItemDto>> GetListAsync()
            => _uow.Projects.GetProjectListAsync(); // FASE 1: OK; FASE 2 mover proyección aquí
    }
}