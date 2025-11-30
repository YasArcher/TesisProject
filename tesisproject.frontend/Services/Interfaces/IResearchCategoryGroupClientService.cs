using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IResearchCategoryGroupClientService
    {
        // =========================
        //           LIST
        // =========================

        Task<HttpResponseWrapper<List<ResearchCategoryGroupListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        // =========================
        //          SINGLE
        // =========================

        Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // =========================
        //          CREATE
        // =========================

        Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> CreateAsync(
            AddResearchCategoryGroupDTO request,
            CancellationToken ct = default);

        // =========================
        //          UPDATE
        // =========================

        Task<HttpResponseWrapper<ResearchCategoryGroupListItemDTO?>> UpdateAsync(
            UpdateResearchCategoryGroupDTO request,
            CancellationToken ct = default);

        // =========================
        //       KEY-VALUE LIST
        // =========================

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}