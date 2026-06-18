using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services
{
    public sealed class HttpResponseWrapper<T>
    {
        public bool Success { get; }
        public T? Data { get; }
        public string? Message { get; }
        public ErrorType Error { get; }
        public string? ErrorCode { get; }
        public Dictionary<string, string[]>? ValidationErrors { get; }

        public HttpResponseWrapper(
            bool success,
            T? data,
            string? message,
            ErrorType error,
            string? errorCode,
            Dictionary<string, string[]>? validationErrors)
        {
            Success = success;
            Data = data;
            Message = message;
            Error = error;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors;
        }

        public static HttpResponseWrapper<T> Ok(T? data, string? message = null)
            => new(
                success: true,
                data: data,
                message: message,
                error: ErrorType.None,
                errorCode: null,
                validationErrors: null);

        public static HttpResponseWrapper<T> Fail(
            string? message,
            ErrorType error = ErrorType.Unexpected,
            string? errorCode = null,
            Dictionary<string, string[]>? validationErrors = null)
            => new(
                success: false,
                data: default,
                message: message,
                error: error,
                errorCode: errorCode,
                validationErrors: validationErrors);
    }
}