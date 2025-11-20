using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProjectType.Request;
using tesisproject.shared.DTOs.Catalog.ProjectType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectTypeClientService : IProjectTypeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/projecttypes";

        public ProjectTypeClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<ProjectTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/projecttypes?onlyActives=true
            return _api.GetAsync<List<ProjectTypeListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/projecttypes/{id}
            return _api.GetAsync<ProjectTypeDetailDTO>(
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
            // GET: api/projecttypes/key-values?term=x&take=10
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

        public Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> CreateAsync(
            AddProjectTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/projecttypes
            return _api.PostAsync<AddProjectTypeRequestDTO, ProjectTypeDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ProjectTypeDetailDTO?>> UpdateAsync(
            UpdateProjectTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/projecttypes/{id}
            return _api.PutAsync<UpdateProjectTypeRequestDTO, ProjectTypeDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}