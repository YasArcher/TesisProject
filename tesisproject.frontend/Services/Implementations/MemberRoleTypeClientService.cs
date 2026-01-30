using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class MemberRoleTypeClientService : IMemberRoleTypeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "memberroletypes";

        public MemberRoleTypeClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<MemberRoleTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/memberroletypes?onlyActives=true
            return _api.GetAsync<List<MemberRoleTypeListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/memberroletypes/{id}
            return _api.GetAsync<MemberRoleTypeDetailDTO>(
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
            // GET: api/memberroletypes/key-values?term=x&take=10
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

        public Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> CreateAsync(
            AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/memberroletypes
            return _api.PostAsync<AddMemberRoleTypeRequestDTO, MemberRoleTypeDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/memberroletypes/{id}
            return _api.PutAsync<UpdateMemberRoleTypeRequestDTO, MemberRoleTypeDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}