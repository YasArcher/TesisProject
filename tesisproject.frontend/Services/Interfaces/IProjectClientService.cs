using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectClientService
    {
        Task<HttpResponseWrapper<List<ProjectListResponseDTO>?>> GetListAsync(bool onlyActive, CancellationToken ct = default);
        Task<HttpResponseWrapper<ProjectListResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<HttpResponseWrapper<ProjectDetailResponseDTO?>> GetDetailAsync(int projectId, CancellationToken ct = default);
        Task<HttpResponseWrapper<ProjectDetailResponseDTO?>> CreateFullAsync(AddProjectFullRequestDTO request, CancellationToken ct = default);
    }
}
