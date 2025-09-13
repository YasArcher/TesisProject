using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectService
    {
        Task<List<ProjectListResponseDTO>> GetAllAsync(CancellationToken ct = default);
        Task<ProjectListResponseDTO?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<ProjectListResponseDTO> CreateAsync(AddProjectRequestDTO project, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateProjectRequestDTO project, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Passthrough to repository with domain semantics if needed.
        /// </summary>
        Task<List<ProjectListResponseDTO>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default);
    }
}
