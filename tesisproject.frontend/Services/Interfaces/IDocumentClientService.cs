using tesisproject.shared.DTOs.Document.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDocumentClientService
    {
        /// <summary>
        /// Uploads a document using multipart/form-data and returns the created document data.
        /// </summary>
        Task<HttpResponseWrapper<DocumentResponseDTO?>> UploadAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default);
    }
}
