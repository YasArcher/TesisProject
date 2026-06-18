using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        /// <summary>
        /// Returns projects filtered by ProjectTypeId.
        /// </summary>
        Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default);
    }
}
