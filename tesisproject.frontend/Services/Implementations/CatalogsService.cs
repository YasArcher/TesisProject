using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalogs;

namespace tesisproject.frontend.Services.Implementations
{
    public class CatalogsService : ICatalogsService
    {
        private readonly IApiClient _api;

        public CatalogsService(IApiClient api)
        {
            _api = api;
        }

        public Task<List<CatalogItemDto>> GetAcademicTermsAsync()
            => GetListAsync("api/catalogs/academic-terms");

        public Task<List<CatalogItemDto>> GetResearchLinesAsync()
            => GetListAsync("api/catalogs/research-lines");

        public Task<List<CatalogItemDto>> GetBroadFieldsAsync()
            => GetListAsync("api/catalogs/broad-fields");

        public Task<List<CatalogItemDto>> GetSpecificFieldsAsync(int? broadFieldId = null)
        {
            var url = "api/catalogs/specific-fields";
            if (broadFieldId.HasValue)
                url += $"?broadFieldId={broadFieldId.Value}";

            return GetListAsync(url);
        }

        public Task<List<CatalogItemDto>> GetDetailedFieldsAsync(int? specificFieldId = null)
        {
            var url = "api/catalogs/detailed-fields";
            if (specificFieldId.HasValue)
                url += $"?specificFieldId={specificFieldId.Value}";

            return GetListAsync(url);
        }

        public Task<List<CatalogItemDto>> GetPublicationStatusesAsync()
            => GetListAsync("api/catalogs/publication-statuses");

        public Task<List<CatalogItemDto>> GetIndexingSourcesAsync()
            => GetListAsync("api/catalogs/indexing-sources");

        public Task<List<CatalogItemDto>> GetProjectsAsync()
            => GetListAsync("api/catalogs/projects");

        public async Task<List<VenueCatalogItemDto>> GetVenuesAsync()
        {
            var data = await _api.GetAsync<List<VenueCatalogItemDto>>("api/catalogs/venues");
            return data ?? new List<VenueCatalogItemDto>();
        }

        // Helper genérico CatalogItemDto
        private async Task<List<CatalogItemDto>> GetListAsync(string url, CancellationToken ct = default)
        {
            var data = await _api.GetAsync<List<CatalogItemDto>>(url, ct);
            return data ?? new List<CatalogItemDto>();
        }
    }
}
