using tesisproject.frontend.Services.Platform.Api;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string url, CancellationToken ct = default);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task DeleteAsync(string url, CancellationToken ct = default);

        Task<HttpResponseWrapper<T?>> GetResultAsync<T>(string url, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PostResultAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PutResultAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> DeleteResultAsync<TResponse>(string url, CancellationToken ct = default);
    }
}
