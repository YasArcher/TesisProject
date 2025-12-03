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
    }
}
