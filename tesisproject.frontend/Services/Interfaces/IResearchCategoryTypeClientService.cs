using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IResearchCategoryTypeClientService
    {
        // LIST
        Task<HttpResponseWrapper<IReadOnlyList<ResearchCategoryTypeListItemDTO>?>>
            ListAsync(bool onlyActives = true, CancellationToken ct = default);

        // GET BY ID
        Task<HttpResponseWrapper<ResearchCategoryTypeDetailDTO?>>
            GetByIdAsync(int id, CancellationToken ct = default);

        // CREATE  -> devuelve el Id creado
        Task<HttpResponseWrapper<int>>
            CreateAsync(ResearchCategoryTypeCreateRequestDTO dto, CancellationToken ct = default);

        // UPDATE  -> devuelve true/false
        Task<HttpResponseWrapper<bool>>
            UpdateAsync(int id, ResearchCategoryTypeUpdateRequestDTO dto, CancellationToken ct = default);

        // DELETE  -> NoContent (ya coincide con IApiClient.DeleteAsync)
        Task<HttpResponseWrapper<NoContent?>>
            DeleteAsync(int id, CancellationToken ct = default);
    }
}