using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectClientService
    {
        Task<HttpResponseWrapper<List<ProjectListResponseDTO>?>> GetListAsync(bool onlyActive, CancellationToken ct = default);
        Task<HttpResponseWrapper<ProjectListResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);

    }
}
