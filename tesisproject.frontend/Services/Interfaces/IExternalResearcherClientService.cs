using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalResearcherClientService
    {
        // LIST
        Task<HttpResponseWrapper<List<ExternalResearcherListItemDTO>?>> ListAsync(
            string? term = null,
            int? institutionId = null,
            CancellationToken ct = default);

        // DETAIL / SINGLE
        Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // KEY VALUES (para SelectInput)
        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? institutionId = null,
            int? take = null,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> CreateAsync(
            ExternalResearcherCreateRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> UpdateAsync(
            ExternalResearcherUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}