using tesisproject.shared.DTOs.Catalog.Country.Request;
using tesisproject.shared.DTOs.Catalog.Country.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface ICountryService
    {
        Task<ServiceResult<IReadOnlyList<CountryListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<CountryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<ServiceResult<CountryDetailDTO>> CreateAsync(
            AddCountryRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<CountryDetailDTO>> UpdateAsync(
            UpdateCountryRequestDTO request,
            CancellationToken ct = default);
    }
}