using tesisproject.shared.DTOs.Catalog.Country.Request;
using tesisproject.shared.DTOs.Catalog.Country.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface ICountryClientService
    {
        Task<HttpResponseWrapper<List<CountryListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<CountryDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<CountryDetailDTO?>> CreateAsync(
            AddCountryRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<CountryDetailDTO?>> UpdateAsync(
            UpdateCountryRequestDTO request,
            CancellationToken ct = default);
    }
}