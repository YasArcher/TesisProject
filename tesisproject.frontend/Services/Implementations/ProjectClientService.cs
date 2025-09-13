using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.frontend.Services.Implementations;

public class ProjectClientService : IProjectClientService
{
    private readonly IApiClient _api;

    public ProjectClientService(IApiClient api) => _api = api;

    public Task<HttpResponseWrapper<List<ProjectListResponseDTO>?>> GetListAsync(bool onlyActive, CancellationToken ct = default)
    {
        return _api.GetAsync<List<ProjectListResponseDTO>>($"api/projects?onlyActive={onlyActive}", ct);
    }

    public Task<HttpResponseWrapper<ProjectListResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _api.GetAsync<ProjectListResponseDTO>($"api/projects/{id}", ct);
    }
}