using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<ServiceResult<DocumentResponseDTO>> UploadAsync(
            UploadDocumentRequestDTO request,
            int currentUserId,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            int currentUserId,
            CancellationToken ct = default);

        Task<ServiceResult<DocumentResponseDTO>> ReplaceFileAsync(
            int documentId,
            ReplaceDocumentFileRequestDTO request,
            int currentUserId,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> DeleteAsync(
            int documentId,
            CancellationToken ct = default);

        Task<ServiceResult<(Stream Stream, string ContentType, string FileName)>> GetContentAsync(
            int documentId,
            CancellationToken ct = default);
    }
}