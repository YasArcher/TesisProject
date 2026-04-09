using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectService
    {
        Task<ServiceResult<List<ProjectListResponseDTO>>> GetAllAsync(CancellationToken ct = default);
        Task<ServiceResult<ProjectListResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<ProjectListResponseDTO>> CreateAsync(AddProjectRequestDTO project, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> UpdateAsync(int id, UpdateProjectRequestDTO project, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<List<ProjectListResponseDTO>>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default);
        Task<ServiceResult<ProjectDetailResponseDTO>> GetProjectDetailAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<ProjectDetailResponseDTO>> CreateFullAsync(AddProjectFullRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> UpdateResearchCategoriesAsync(int projectId, List<int> researchCategoryIds, CancellationToken ct = default);
        Task<ServiceResult<int>> ImportFromMatrixAsync(ProjectMatrixUploadSummaryDTO summary, CancellationToken ct = default);
    }
}
