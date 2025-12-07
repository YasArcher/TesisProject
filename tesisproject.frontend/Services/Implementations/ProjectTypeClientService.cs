using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

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

        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/projecttypes?onlyActives=true
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
            // GET: api/projecttypes/{id}
            return _api.GetAsync<CatalogDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/projecttypes
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

            // PUT: api/projecttypes/{id}
            return _api.PutAsync<UpdateCatalogRequestDTO, CatalogDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
    int id,
    CancellationToken ct = default)
        {
            // DELETE: api/documenttypes/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}