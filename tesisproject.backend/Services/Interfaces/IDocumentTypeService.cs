using tesisproject.shared.DTOs.Catalog.DocumentType.Request;
using tesisproject.shared.DTOs.Catalog.DocumentType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<ServiceResult<IReadOnlyList<DocumentTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentTypeDetailDTO>> CreateAsync(
            AddDocumentTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentTypeDetailDTO>> UpdateAsync(
            UpdateDocumentTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}