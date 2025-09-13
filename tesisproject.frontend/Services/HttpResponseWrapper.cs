namespace tesisproject.frontend.Services
{
    public sealed class HttpResponseWrapper<T>
    {
        public bool Success { get; }
        public T? Response { get; }
        public string? Error { get; }
        public HttpResponseMessage HttpResponse { get; }

        public HttpResponseWrapper(bool success, T? response, string? error, HttpResponseMessage httpResponse)
        {
            Success = success;
            Response = response;
            Error = error;
            HttpResponse = httpResponse;
        }
    }
}