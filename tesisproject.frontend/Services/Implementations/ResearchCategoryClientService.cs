using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ResearchCategoryClientService : IResearchCategoryClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/researchcategories";

        public ResearchCategoryClientService(IApiClient api) => _api = api;

        // =========================
        //           READS
        // =========================

        public Task<HttpResponseWrapper<IReadOnlyList<ResearchCategoryListItemDTO>?>> GetAllAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/researchcategories?onlyActives=true
            return _api.GetAsync<IReadOnlyList<ResearchCategoryListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        public Task<HttpResponseWrapper<List<ResearchCategoryTreeItemDTO>?>> GetTreeAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // GET: api/researchcategories/tree?onlyActives=true
            return _api.GetAsync<List<ResearchCategoryTreeItemDTO>>(
                $"{_baseUrl}/tree?onlyActives={onlyActives}",
                ct
            );
        }

        public Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            // GET: api/researchcategories/{id}
            return _api.GetAsync<ResearchCategoryDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //           CREATE
        // =========================

        public Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> CreateAsync(
            AddResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/researchcategories
            return _api.PostAsync<AddResearchCategoryRequestDTO, ResearchCategoryDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //           UPDATE
        // =========================

        public Task<HttpResponseWrapper<ResearchCategoryDetailDTO?>> UpdateAsync(
            UpdateResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            // PUT: api/researchcategories/{id}
            return _api.PutAsync<UpdateResearchCategoryRequestDTO, ResearchCategoryDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct
            );
        }

        // =========================
        //           DELETE
        // =========================

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            // DELETE: api/researchcategories/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}