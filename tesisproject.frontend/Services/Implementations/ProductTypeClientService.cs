using System.Buffers.Text;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProductTypeClientService : IProductTypeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "producttypes";

        public ProductTypeClientService(IApiClient api)
        {
            _api = api;
        }

        // ============================
        //          LIST
        // ============================

        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            CancellationToken ct = default)
        {
            // GET: api/producttypes
            return _api.GetAsync<List<CatalogListItemDTO>>(_baseUrl, ct);
        }

        // ============================
        //        GET BY ID
        // ============================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/producttypes/{id}
            var url = $"{_baseUrl}/{id}";
            return _api.GetAsync<CatalogDetailDTO>(url, ct);
        }

        // ============================
        //          CREATE
        // ============================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/producttypes
            return _api.PostAsync<AddCatalogRequestDTO, CatalogDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // ============================
        //          UPDATE
        // ============================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            var url = $"{_baseUrl}";
            return _api.PutAsync<UpdateCatalogRequestDTO, CatalogDetailDTO>(
                url,
                request,
                ct);

        }

        // ============================
        //          DELETE
        // ============================

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // DELETE: api/producttypes/{id}
            var url = $"{_baseUrl}/{id}";
            return _api.DeleteAsync(url, ct);
        }
    }
}