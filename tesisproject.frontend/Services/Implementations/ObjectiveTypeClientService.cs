using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Request;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class ObjectiveTypeClientService : IObjectiveTypeClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/objectivetypes";

        public ObjectiveTypeClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================

        // GET: api/objectivetypes?onlyActives=true
        public Task<HttpResponseWrapper<List<ObjectiveTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<ObjectiveTypeListItemDTO>>(url, ct);
        }

        // =========================
        //        GET BY ID
        // =========================

        // GET: api/objectivetypes/{id}
        public Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<ObjectiveTypeDetailDTO>(url, ct);
        }

        // =========================
        //        KEY VALUES
        // =========================

        // GET: api/objectivetypes/key-values?term=x&take=10
        public Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(term))
                queryParts.Add($"term={Uri.EscapeDataString(term)}");

            if (take.HasValue)
                queryParts.Add($"take={take.Value}");

            var queryString = queryParts.Count > 0
                ? "?" + string.Join("&", queryParts)
                : string.Empty;

            var url = $"{BaseUrl}/key-values{queryString}";
            return _api.GetAsync<List<KeyValueItemDTO>>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        // POST: api/objectivetypes
        public Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> CreateAsync(
            AddObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PostAsync<AddObjectiveTypeRequestDTO, ObjectiveTypeDetailDTO>(
                BaseUrl,
                request,
                ct);

        }

        // =========================
        //          UPDATE
        // =========================

        // PUT: api/objectivetypes/{id}
        public Task<HttpResponseWrapper<ObjectiveTypeDetailDTO?>> UpdateAsync(
            int id,
            UpdateObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";
            // El controller ya forza request.Id = id, así que aquí no es obligatorio setearlo,
            // pero puedes hacerlo para mantener consistencia:
            request.Id = id;

            return _api.PutAsync<UpdateObjectiveTypeRequestDTO, ObjectiveTypeDetailDTO>(
                url,
                request,
                ct);

        }
    }
}