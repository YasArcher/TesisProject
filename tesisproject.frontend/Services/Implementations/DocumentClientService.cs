using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Document.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class DocumentClientService : IDocumentClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/documents";

        public DocumentClientService(IApiClient api)
        {
            _api = api;
        }

        /// <inheritdoc />
        public Task<HttpResponseWrapper<DocumentResponseDTO?>> UploadAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            // POST: api/documents/upload
            return _api.PostMultipartAsync<DocumentResponseDTO>(
                $"{_baseUrl}/upload",
                content,
                ct
            );
        }
    }
}