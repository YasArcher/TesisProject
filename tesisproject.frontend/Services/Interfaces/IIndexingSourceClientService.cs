using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IIndexingSourceClientService
    {
        Task<HttpResponseWrapper<List<IndexingSourceListItemDTO>?>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> CreateAsync(
            IndexingSourceCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<IndexingSourceListItemDTO?>> UpdateAsync(
            IndexingSourceUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}