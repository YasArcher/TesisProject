using System.Net;

namespace tesisproject.frontend.Services.Platform.Api
{
    public sealed class HttpResponseWrapper<T>
    {
        public bool Success { get; }
        public T? Data { get; }
        public string? Message { get; }
        public HttpStatusCode StatusCode { get; }
        public string? ErrorCode { get; }
        public Dictionary<string, string[]>? ValidationErrors { get; }

        public HttpResponseWrapper(
            bool success,
            T? data,
            string? message,
            HttpStatusCode statusCode,
            string? errorCode = null,
            Dictionary<string, string[]>? validationErrors = null)
        {
            Success = success;
            Data = data;
            Message = message;
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors;
        }

        public static HttpResponseWrapper<T> Ok(
            T? data,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string? message = null)
            => new(true, data, message, statusCode);

        public static HttpResponseWrapper<T> Fail(
            string? message,
            HttpStatusCode statusCode,
            string? errorCode = null,
            Dictionary<string, string[]>? validationErrors = null)
            => new(false, default, message, statusCode, errorCode, validationErrors);
    }
}
