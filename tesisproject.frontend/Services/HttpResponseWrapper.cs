using System.Net.Http;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services
{
    public sealed class HttpResponseWrapper<T>
    {
        public bool Success { get; }
        public T? Response { get; }
        public string? Message { get; }
        public ErrorType ErrorType { get; }
        public string? ErrorCode { get; }
        public Dictionary<string, string[]>? ValidationErrors { get; }
        public HttpResponseMessage HttpResponse { get; }

        public HttpResponseWrapper(
            bool success,
            T? response,
            string? message,
            ErrorType errorType,
            string? errorCode,
            Dictionary<string, string[]>? validationErrors,
            HttpResponseMessage httpResponse)
        {
            Success = success;
            Response = response;
            Message = message;
            ErrorType = errorType;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors;
            HttpResponse = httpResponse;
        }
    }
}