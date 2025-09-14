using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Project;

namespace tesisproject.shared.Abstractions.Project;

public interface IProjectsService
{
    Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateProjectRequest req, CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateProjectRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

