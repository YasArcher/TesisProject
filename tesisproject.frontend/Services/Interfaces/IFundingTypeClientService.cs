using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IFundingTypeClientService
    {
        // GET: api/FundingTypes?onlyActives=true
        Task<HttpResponseWrapper<List<FundingTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        // GET: api/FundingTypes/{id}
        Task<HttpResponseWrapper<FundingTypeListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // POST: api/FundingTypes
        Task<HttpResponseWrapper<FundingTypeListItemDTO?>> CreateAsync(
            AddFundingTypeRequestDTO request,
            CancellationToken ct = default);

        // PUT: api/FundingTypes/{id}
        Task<HttpResponseWrapper<FundingTypeListItemDTO?>> UpdateAsync(
            UpdateFundingTypeRequestDTO request,
            CancellationToken ct = default);

        // GET: api/FundingTypes/key-values?term=..&take=..
        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}