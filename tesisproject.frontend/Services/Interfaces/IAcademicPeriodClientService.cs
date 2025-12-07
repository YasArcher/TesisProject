using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Request;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IAcademicPeriodClientService
    {
        Task<HttpResponseWrapper<List<AcademicPeriodListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<AcademicPeriodListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<AcademicPeriodListItemDTO?>> CreateAsync(
            AcademicPeriodCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<AcademicPeriodListItemDTO?>> UpdateAsync(
            AcademicPeriodUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}