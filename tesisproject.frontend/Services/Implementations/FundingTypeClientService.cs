using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class FundingTypeClientService : IFundingTypeClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/FundingTypes";

        public FundingTypeClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<FundingTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/FundingTypes?onlyActives={onlyActives}
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<FundingTypeListItemDTO>>(url, ct);
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<FundingTypeListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/FundingTypes/{id}
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<FundingTypeListItemDTO>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<FundingTypeListItemDTO?>> CreateAsync(
            AddFundingTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/FundingTypes
            return _api.PostAsync<AddFundingTypeRequestDTO, FundingTypeListItemDTO>(
                BaseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<FundingTypeListItemDTO?>> UpdateAsync(
            UpdateFundingTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/FundingTypes/{id}
            var url = $"{BaseUrl}/{request.Id}";
            return _api.PutAsync<UpdateFundingTypeRequestDTO, FundingTypeListItemDTO>(
                url,
                request,
                ct
            );
        }

        // =========================
        //       KEY-VALUE LIST
        // =========================

        public Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(term))
                query.Add($"term={Uri.EscapeDataString(term)}");

            if (take.HasValue)
                query.Add($"take={take.Value}");

            var queryString = query.Count > 0
                ? "?" + string.Join("&", query)
                : string.Empty;

            var url = $"{BaseUrl}/key-values{queryString}";

            // GET: api/FundingTypes/key-values?...
            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }
    }
}