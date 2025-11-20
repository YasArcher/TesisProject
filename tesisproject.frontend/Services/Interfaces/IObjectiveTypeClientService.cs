using tesisproject.shared.DTOs.Catalog.ObjectiveType.Request;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IObjectiveTypeClientService
    {
        Task<HttpResponseWrapper<List<ObjectiveTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> CreateAsync(
            AddObjectiveTypeRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> UpdateAsync(
            int id,
            UpdateObjectiveTypeRequestDTO request,
            CancellationToken ct = default);
    }
}