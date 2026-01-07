using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDocumentClientService
    {
        Task<HttpResponseWrapper<DocumentResponseDTO?>> CreateAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<DocumentResponseDTO?>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<DocumentResponseDTO?>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<DocumentResponseDTO?>> ReplaceFileAsync(
            int documentId,
            MultipartFormDataContent content,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int documentId,
            CancellationToken ct = default);

        string GetViewUrl(int documentId);
        string GetDownloadUrl(int documentId);
    }
}