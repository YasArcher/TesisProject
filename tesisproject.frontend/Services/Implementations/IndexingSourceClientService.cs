using System.Web;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class IndexingSourceClientService : IIndexingSourceClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/indexingsources";

        public IndexingSourceClientService(IApiClient api)
        {
            _api = api;
        }

        public Task<HttpResponseWrapper<List<IndexingSourceListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var url = $"{_baseUrl}?onlyActives={onlyActives.ToString().ToLower()}";
            return _api.GetAsync<List<IndexingSourceListItemDTO>>(url, ct);
        }

        public Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be positive.", nameof(id));

            return _api.GetAsync<IndexingSourceListItemDTO>($"{_baseUrl}/{id}", ct);
        }

        public Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var qs = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrWhiteSpace(term))
                qs["term"] = term.Trim();

            if (take.HasValue && take.Value > 0)
                qs["take"] = take.Value.ToString();

            var url = $"{_baseUrl}/keyvalues";
            var queryString = qs.ToString();

            if (!string.IsNullOrEmpty(queryString))
                url += $"?{queryString}";

            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }

        public Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> CreateAsync(
            IndexingSourceCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return _api.PostAsync<IndexingSourceCreateRequestDTO, IndexingSourceListItemDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> UpdateAsync(
            IndexingSourceUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be positive.", nameof(request.Id));

            return _api.PutAsync<IndexingSourceUpdateRequestDTO, IndexingSourceListItemDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}