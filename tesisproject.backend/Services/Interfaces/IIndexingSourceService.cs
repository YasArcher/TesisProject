using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IIndexingSourceService
    {
        Task<ServiceResult<IReadOnlyList<IndexingSourceListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<IndexingSourceListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<ServiceResult<IndexingSourceListItemDTO>> CreateAsync(
            IndexingSourceCreateRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<IndexingSourceListItemDTO>> UpdateAsync(
            IndexingSourceUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}