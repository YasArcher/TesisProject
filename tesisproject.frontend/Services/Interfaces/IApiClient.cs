namespace tesisproject.frontend.Services.Interfaces
{
    public interface IApiClient
    {
        Task<HttpResponseWrapper<T?>> GetAsync<T>(string url, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<TResponse?>> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);
        Task<HttpResponseWrapper<NoContent>> DeleteAsync(string url, CancellationToken ct = default);
    }
}