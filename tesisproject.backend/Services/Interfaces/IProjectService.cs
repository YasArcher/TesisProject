using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllAsync(CancellationToken ct = default);
        Task<Project?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<Project> CreateAsync(Project project, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, Project project, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Lightweight list for UI consumption (projection happens in Service).
        /// </summary>
        Task<List<ProjectListResponseDTO>> GetListAsync(CancellationToken ct = default);

        /// <summary>
        /// Passthrough to repository with domain semantics if needed.
        /// </summary>
        Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default);
    }
}
