using System.Web;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalResearcherClientService : IExternalResearcherClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/externalresearchers";

        public ExternalResearcherClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<ExternalResearcherListItemDTO>?>> ListAsync(
            string? term = null,
            int? institutionId = null,
            CancellationToken ct = default)
        {
            // Construir query string opcional: ?term=...&institutionId=...
            var url = BuildListUrl(term, institutionId);
            return _api.GetAsync<List<ExternalResearcherListItemDTO>>(url, ct);
        }

        private string BuildListUrl(string? term, int? institutionId)
        {
            var hasTerm = !string.IsNullOrWhiteSpace(term);
            var hasInstitution = institutionId.HasValue;

            if (!hasTerm && !hasInstitution)
                return _baseUrl;

            var query = HttpUtility.ParseQueryString(string.Empty);

            if (hasTerm)
                query["term"] = term!.Trim();

            if (hasInstitution)
                query["institutionId"] = institutionId!.Value.ToString();

            return $"{_baseUrl}?{query}";
        }

        // =========================
        //          DETAIL
        // =========================

        public Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/externalresearchers/{id}
            return _api.GetAsync<ExternalResearcherDetailDTO>($"{_baseUrl}/{id}", ct);
        }

        // =========================
        //        KEY VALUES
        // =========================

        public Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? institutionId = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var url = BuildKeyValuesUrl(term, institutionId, take);
            // GET: api/externalresearchers/keyvalues?term=...&institutionId=...&take=...
            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }

        private string BuildKeyValuesUrl(string? term, int? institutionId, int? take)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrWhiteSpace(term))
                query["term"] = term!.Trim();

            if (institutionId.HasValue)
                query["institutionId"] = institutionId.Value.ToString();

            if (take.HasValue && take.Value > 0)
                query["take"] = take.Value.ToString();

            var qs = query.ToString();
            return string.IsNullOrEmpty(qs)
                ? $"{_baseUrl}/keyvalues"
                : $"{_baseUrl}/keyvalues?{qs}";
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> CreateAsync(
            ExternalResearcherCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/externalresearchers
            return _api.PostAsync<ExternalResearcherCreateRequestDTO, ExternalResearcherDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ExternalResearcherDetailDTO?>> UpdateAsync(
            ExternalResearcherUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/externalresearchers/{id}
            return _api.PutAsync<ExternalResearcherUpdateRequestDTO, ExternalResearcherDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}