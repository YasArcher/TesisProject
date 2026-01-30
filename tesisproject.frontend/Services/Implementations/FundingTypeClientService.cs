using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class FundingTypeClientService : IFundingTypeClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "fundingtypes";

        public FundingTypeClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/FundingTypes?onlyActives={onlyActives}
            var url = $"{BaseUrl}?onlyActives={onlyActives}";
            return _api.GetAsync<List<CatalogListItemDTO>>(url, ct);
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/FundingTypes/{id}
            var url = $"{BaseUrl}/{id}";
            return _api.GetAsync<CatalogDetailDTO>(url, ct);
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/FundingTypes
            return _api.PostAsync<AddCatalogRequestDTO, CatalogDetailDTO>(
                BaseUrl,
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

            // PUT: api/FundingTypes/{id}
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