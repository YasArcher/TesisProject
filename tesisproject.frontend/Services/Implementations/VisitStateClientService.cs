using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class VisitStateClientService : IVisitStateClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/visitstates";

        public VisitStateClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/visitstates?onlyActives=true
            return _api.GetAsync<List<CatalogListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/visitstates/{id}
            return _api.GetAsync<CatalogDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //         KEY VALUES
        // =========================

        public Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term,
            int? take,
            CancellationToken ct = default)
        {
            // GET: api/visitstates/key-values?term=x&take=10
            string url = $"{_baseUrl}/key-values";

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(term))
                query.Add($"term={term}");
            if (take.HasValue)
                query.Add($"take={take}");

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/visitstates
            return _api.PostAsync<AddCatalogRequestDTO, CatalogDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/visitstates/{id}
            return _api.PutAsync<UpdateCatalogRequestDTO, CatalogDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            // DELETE: api/visitstates/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}