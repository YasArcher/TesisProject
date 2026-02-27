using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IApiClient
    {
        Task<HttpResponseWrapper<T?>> GetAsync<T>(string url, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PatchAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(string url, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content, CancellationToken ct = default);
        Task<HttpResponseWrapper<FilePayloadDTO?>> GetFileAsync(string url, CancellationToken ct = default);
        Task<HttpResponseWrapper<FilePayloadDTO?>> PostFileAsync<TRequest>(string url, TRequest body, CancellationToken ct = default);

    }
}