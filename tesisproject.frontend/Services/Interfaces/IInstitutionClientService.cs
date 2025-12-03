using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IInstitutionClientService
    {
        Task<HttpResponseWrapper<List<InstitutionListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<InstitutionDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<InstitutionDetailDTO?>> CreateAsync(
            AddInstitutionRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<InstitutionDetailDTO?>> UpdateAsync(
            UpdateInstitutionRequestDTO request,
            CancellationToken ct = default);
    }
}
