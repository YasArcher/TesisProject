using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;

        public ProjectService(IUnitOfWork uow) => _uow = uow;

        public async Task<List<Project>> GetAllAsync(CancellationToken ct = default)
            => await _uow.Projects.GetAllAsync(null, ct);

        public async Task<Project?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _uow.Projects.GetByIdAsync(new object[] { id }, ct);

        public async Task<Project> CreateAsync(Project project, CancellationToken ct = default)
        {
            await _uow.Projects.AddAsync(project, ct);
            await _uow.SaveChangesAsync(ct);
            return project;
        }

        public async Task<bool> UpdateAsync(int id, Project project, CancellationToken ct = default)
        {
            var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
            if (current is null) return false;

            // FASE 1: update simple (enforce key + full update)
            project.ProjectId = id;            // Garantiza que el ID coincida
            _uow.Projects.Update(project);     // Update por estado completo de la entidad
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
            if (current is null) return false;

            _uow.Projects.Remove(current);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ProjectListResponseDTO>> GetListAsync(CancellationToken ct = default)
        {
            // Proyección en Service (no en Repo)
            var q = _uow
                .Projects
                .Query() // AsNoTracking por defecto en el genérico
                .Select(p => new ProjectListResponseDTO(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectState.Name,
                    p.ProjectType.Name,
                    p.ProjectGroup.Name,
                    p.StartDate,
                    p.TentativeEndDate,
                    p.ExecutionPercentage
                ));

            return await q.ToListAsync(ct);
        }

        public Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default)
            => _uow.Projects.GetByTypeAsync(projectTypeId, ct);
    }
}
