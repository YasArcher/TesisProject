using System.Buffers.Text;
using System.Net.Http.Headers;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Document.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class DocumentClientService : IDocumentClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "documents";

        public DocumentClientService(IApiClient api)
        {
            _api = api;
        }

        public Task<HttpResponseWrapper<DocumentResponseDTO?>> CreateAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            // POST: api/documents
            return _api.PostMultipartAsync<DocumentResponseDTO>(BaseUrl, content, ct);
        }

        public Task<HttpResponseWrapper<DocumentResponseDTO?>> GetByIdAsync(
            int documentId,
            CancellationToken ct = default)
        {
            // GET: api/documents/{id}
            return _api.GetAsync<DocumentResponseDTO>($"{BaseUrl}/{documentId}", ct);
        }

        public Task<HttpResponseWrapper<DocumentResponseDTO?>> UpdateAsync(
            int documentId,
            UpdateDocumentRequestDTO request,
            CancellationToken ct = default)
        {
            // PUT: api/documents/{id}
            return _api.PutAsync<UpdateDocumentRequestDTO, DocumentResponseDTO>(
                $"{BaseUrl}/{documentId}",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<DocumentResponseDTO?>> ReplaceFileAsync(
            int documentId,
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {

            return _api.PostMultipartAsync<DocumentResponseDTO>(
                $"{BaseUrl}/{documentId}/file",
                content,
                ct
            );
        }

        public async Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int documentId,
            CancellationToken ct = default)
        {
            // DELETE: api/documents/{id}
            return await _api.DeleteAsync($"{BaseUrl}/{documentId}", ct);
        }

        public Task<HttpResponseWrapper<FilePayloadDTO?>> GetContentAsync(int documentId, CancellationToken ct = default)
    => _api.GetFileAsync($"{BaseUrl}/{documentId}/content", ct);

        public Task<HttpResponseWrapper<FilePayloadDTO?>> GetDownloadAsync(int documentId, CancellationToken ct = default)
            => _api.GetFileAsync($"{BaseUrl}/{documentId}/download", ct);
    }
}