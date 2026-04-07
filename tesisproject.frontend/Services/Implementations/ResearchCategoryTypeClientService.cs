using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ResearchCategoryTypeClientService : IResearchCategoryTypeClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "catalog/research-category-types";

        public ResearchCategoryTypeClientService(IApiClient api) => _api = api;

        // =========================
        //           LIST
        // =========================
        public Task<HttpResponseWrapper<IReadOnlyList<ResearchCategoryTypeListItemDTO>?>>
            ListAsync(bool onlyActives = true, CancellationToken ct = default)
        {
            // GET: api/catalog/research-category-types?onlyActives=true
            return _api.GetAsync<IReadOnlyList<ResearchCategoryTypeListItemDTO>>(
                $"{_baseUrl}?onlyActives={onlyActives}",
                ct
            );
        }

        // =========================
        //         GET BY ID
        // =========================
        public Task<HttpResponseWrapper<ResearchCategoryTypeDetailDTO?>>
            GetByIdAsync(int id, CancellationToken ct = default)
        {
            // GET: api/catalog/research-category-types/{id}
            return _api.GetAsync<ResearchCategoryTypeDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================
        public Task<HttpResponseWrapper<int>>
            CreateAsync(ResearchCategoryTypeCreateRequestDTO dto, CancellationToken ct = default)
        {
            // POST: api/catalog/research-category-types
            // Backend devuelve algo que se deserializa como "int"
            return _api.PostAsync<ResearchCategoryTypeCreateRequestDTO, int>(
                _baseUrl,
                dto,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================
        public Task<HttpResponseWrapper<bool>>
            UpdateAsync(int id, ResearchCategoryTypeUpdateRequestDTO dto, CancellationToken ct = default)
        {
            // PUT: api/catalog/research-category-types/{id}
            return _api.PutAsync<ResearchCategoryTypeUpdateRequestDTO, bool>(
                $"{_baseUrl}/{id}",
                dto,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================
        public Task<HttpResponseWrapper<bool?>>
            DeleteAsync(int id, CancellationToken ct = default)
        {
            return _api.DeleteAsync<bool?>(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}