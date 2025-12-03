using System.Web;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class InstitutionClientService : IInstitutionClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/institutions";

        public InstitutionClientService(IApiClient api)
        {
            _api = api;
        }

        // LIST
        public Task<HttpResponseWrapper<List<InstitutionListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var url = $"{_baseUrl}?onlyActives={onlyActives.ToString().ToLower()}";
            return _api.GetAsync<List<InstitutionListItemDTO>>(url, ct);
        }

        // DETAIL
        public Task<HttpResponseWrapper<InstitutionDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            return _api.GetAsync<InstitutionDetailDTO>($"{_baseUrl}/{id}", ct);
        }

        // KEY VALUES
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

        // CREATE
        public Task<HttpResponseWrapper<InstitutionDetailDTO?>> CreateAsync(
            AddInstitutionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return _api.PostAsync<AddInstitutionRequestDTO, InstitutionDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // UPDATE
        public Task<HttpResponseWrapper<InstitutionDetailDTO?>> UpdateAsync(
            UpdateInstitutionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            return _api.PutAsync<UpdateInstitutionRequestDTO, InstitutionDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}