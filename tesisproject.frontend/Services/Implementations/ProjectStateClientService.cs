using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectStateClientService : IProjectStateClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "projectstates";

        public ProjectStateClientService(IApiClient api)
            => _api = api;

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/projectstates?onlyActives=true
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
            // GET: api/projectstates/{id}
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
            // POST: api/projectstates
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

            // PUT: api/projectstates/{id}
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
            // DELETE: api/projectstates/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}