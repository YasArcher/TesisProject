using System.Net.Http.Json;
using tesisproject.shared.Abstractions;
using tesisproject.shared.DTOs.Project;

namespace tesisproject.frontend.Services;

public class ProjectsClient
{
    private readonly HttpClient _http;
    public ProjectsClient(HttpClient http) => _http = http;

    public Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync()
        => _http.GetFromJsonAsync<Result<IReadOnlyList<ProjectDto>>>("/api/projects")!;

    public Task<Result<ProjectDto>> GetByIdAsync(int id)
        => _http.GetFromJsonAsync<Result<ProjectDto>>($"/api/projects/{id}")!;

    public async Task<Result<int>> CreateAsync(CreateProjectRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/api/projects", req);
        return await resp.Content.ReadFromJsonAsync<Result<int>>() ?? Result<int>.Fail("No response");
    }

    public async Task<Result> UpdateAsync(UpdateProjectRequest req)
    {
        var resp = await _http.PutAsJsonAsync("/api/projects", req);
        return await resp.Content.ReadFromJsonAsync<Result>() ?? Result.Fail("No response");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var resp = await _http.DeleteAsync($"/api/projects/{id}");
        return await resp.Content.ReadFromJsonAsync<Result>() ?? Result.Fail("No response");
    }
}
