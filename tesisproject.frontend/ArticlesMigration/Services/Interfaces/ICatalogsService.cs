using System.Collections.Generic;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Catalogs;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface ICatalogsService
    {
        Task<List<CatalogItemDto>> GetAcademicTermsAsync();
        Task<List<CatalogItemDto>> GetResearchLinesAsync();
        Task<List<CatalogItemDto>> GetBroadFieldsAsync();
        Task<List<CatalogItemDto>> GetSpecificFieldsAsync(int? broadFieldId = null);
        Task<List<CatalogItemDto>> GetDetailedFieldsAsync(int? specificFieldId = null);
        Task<List<CatalogItemDto>> GetPublicationStatusesAsync();
        Task<List<CatalogItemDto>> GetIndexingSourcesAsync();
        Task<List<CatalogItemDto>> GetFacultiesAsync();
        Task<List<CatalogItemDto>> GetProjectsAsync();

        Task<List<VenueCatalogItemDto>> GetVenuesAsync();
    }
}
