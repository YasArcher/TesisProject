using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Responses
{
    public enum ErrorType
    {
        None = 0,
        NotFound,
        Validation,
        Conflict,
        Forbidden,
        Unauthorized,
        Unexpected
    }

    public class ServiceResult<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public ErrorType Error { get; init; }
        public string? ErrorCode { get; init; }
        public T? Data { get; init; }
        public Dictionary<string, string[]>? ValidationErrors { get; init; }

        public static ServiceResult<T> Ok(T data, string? message = null)
            => new()
            {
                Success = true,
                Data = data,
                Message = message,
                Error = ErrorType.None,
                ErrorCode = null,
                ValidationErrors = null
            };

        public static ServiceResult<T> Fail(
            string message,
            ErrorType error = ErrorType.Unexpected,
            string? errorCode = null,
            Dictionary<string, string[]>? validation = null)
            => new()
            {
                Success = false,
                Message = message,
                Error = error,
                ErrorCode = errorCode,
                Data = default,
                ValidationErrors = validation
            };
    }
}