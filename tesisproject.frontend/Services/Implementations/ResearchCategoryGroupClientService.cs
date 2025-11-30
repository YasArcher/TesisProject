using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class ResearchCategoryGroupClientService : IResearchCategoryGroupClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/ResearchCategoryGroups";

        public ResearchCategoryGroupClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<ResearchCategoryGroupListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/ResearchCategoryGroups?onlyActives={onlyActives}
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<ResearchCategoryGroupListItemDTO>>(url, ct);
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/ResearchCategoryGroups/{id}
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<ResearchCategoryGroupListItemDTO>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> CreateAsync(
            AddResearchCategoryGroupDTO request,
            CancellationToken ct = default)
        {
            // POST: api/ResearchCategoryGroups
            return _api.PostAsync<AddResearchCategoryGroupDTO, ResearchCategoryGroupListItemDTO>(
                BaseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> UpdateAsync(
            UpdateResearchCategoryGroupDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/ResearchCategoryGroups/{id}
            var url = $"{BaseUrl}/{request.Id}";
            return _api.PutAsync<UpdateResearchCategoryGroupDTO, ResearchCategoryGroupListItemDTO>(
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

            // GET: api/ResearchCategoryGroups/key-values?...
            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }
    }
}