using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Request;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class TransactionTypeClientService : ITransactionTypeClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/TransactionTypes";

        public TransactionTypeClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================
        public Task<HttpResponseWrapper<List<TransactionTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<TransactionTypeListItemDTO>>(url, ct);
        }

        // =========================
        //          SINGLE
        // =========================
        public Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<TransactionTypeListItemDTO>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================
        public Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> CreateAsync(
            AddTransactionTypeRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PostAsync<AddTransactionTypeRequestDTO, TransactionTypeListItemDTO>(
                BaseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================
        public Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> UpdateAsync(
            UpdateTransactionTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            var url = $"{BaseUrl}/{request.Id}";
            return _api.PutAsync<UpdateTransactionTypeRequestDTO, TransactionTypeListItemDTO>(
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

            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }
    }
}