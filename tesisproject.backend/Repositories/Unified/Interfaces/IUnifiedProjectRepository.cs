using tesisproject.backend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedProjectRepository : IGenericRepository<Project>
    {
        /// <summary>
        /// Returns projects filtered by ProjectTypeId.
        /// </summary>
        Task<List<Project>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default);
    }
}
