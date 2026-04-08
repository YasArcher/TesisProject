using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public ErrorType Error { get; init; }
        public string? ErrorCode { get; init; }
        public T? Data { get; init; }
        public Dictionary<string, string[]>? ValidationErrors { get; init; }

        public static ApiResponse<T> Ok(T data, string? message = null)
            => new()
            {
                Success = true,
                Message = message,
                Error = ErrorType.None,
                ErrorCode = null,
                Data = data,
                ValidationErrors = null
            };

        public static ApiResponse<T> Fail(
            string message,
            ErrorType error = ErrorType.Unexpected,
            string? errorCode = null,
            Dictionary<string, string[]>? validationErrors = null)
            => new()
            {
                Success = false,
                Message = message,
                Error = error,
                ErrorCode = errorCode,
                Data = default,
                ValidationErrors = validationErrors
            };
    }
}