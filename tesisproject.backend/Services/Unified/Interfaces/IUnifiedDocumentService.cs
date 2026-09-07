using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedDocumentService
    {
        Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> ReplaceFileAsync(
            int documentId,
            ReplaceDocumentFileRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> DeleteAsync(
            int documentId,
            CancellationToken ct = default);

        Task<ServiceResult<(Stream Stream, string ContentType, string FileName)>> GetContentAsync(
            int documentId,
            CancellationToken ct = default);
    }
}