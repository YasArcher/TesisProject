using tesisproject.shared.DTOs.Catalog.ProjectType.Request;
using tesisproject.shared.DTOs.Catalog.ProjectType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectTypeClientService
    {
        // LIST
        Task<HttpResponseWrapper<List<ProjectTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        // SINGLE
        Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // KEY VALUES (search + take)
        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term,
            int? take,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> CreateAsync(
            AddProjectTypeRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> UpdateAsync(
            UpdateProjectTypeRequestDTO request,
            CancellationToken ct = default);
    }
}