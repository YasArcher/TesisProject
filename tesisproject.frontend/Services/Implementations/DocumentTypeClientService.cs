using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.DocumentType.Request;
using tesisproject.shared.DTOs.Catalog.DocumentType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class DocumentTypeClientService : IDocumentTypeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/documenttypes";

        public DocumentTypeClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<DocumentTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/documenttypes?onlyActives=true
            return _api.GetAsync<List<DocumentTypeListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/documenttypes/{id}
            return _api.GetAsync<DocumentTypeDetailDTO>(
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
            // GET: api/documenttypes/key-values?term=x&take=10
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

        public Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> CreateAsync(
            AddDocumentTypeRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/documenttypes
            return _api.PostAsync<AddDocumentTypeRequestDTO, DocumentTypeDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<DocumentTypeDetailDTO?>> UpdateAsync(
            UpdateDocumentTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/documenttypes/{id}
            return _api.PutAsync<UpdateDocumentTypeRequestDTO, DocumentTypeDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }
    }
}