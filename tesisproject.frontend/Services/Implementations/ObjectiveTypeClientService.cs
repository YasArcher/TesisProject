using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

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
        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<CatalogListItemDTO>>(url, ct);
        }

        // =========================
        //        GET BY ID
        // =========================

        // GET: api/objectivetypes/{id}
        public Task<HttpResponseWrapper<CatalogDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<CatalogDetailDTO>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        // POST: api/objectivetypes
        public Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PostAsync<AddCatalogRequestDTO, CatalogDetailDTO>(
                BaseUrl,
                request,
                ct);

        }

        // =========================
        //          UPDATE
        // =========================

        // PUT: api/objectivetypes/{id}
        public Task<HttpResponseWrapper<CatalogDetailDTO?>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            var url = $"{BaseUrl}/{request.Id}";
            return _api.PutAsync<UpdateCatalogRequestDTO, CatalogDetailDTO>(
                url,
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
            // DELETE: api/documenttypes/{id}
            return _api.DeleteAsync(
                $"{BaseUrl}/{id}",
                ct
            );
        }
    }
}