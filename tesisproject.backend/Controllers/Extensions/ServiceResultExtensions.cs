using Microsoft.AspNetCore.Mvc;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Extensions
{
    public static class ServiceResultExtensions
    {
        public static ActionResult<ApiResponse<T>> ToActionResult<T>(this ServiceResult<T> result)
        {
            if (result.Success)
                return new OkObjectResult(ApiResponse<T>.Ok(result.Data!, result.Message));

            // Mapea error → HTTP status
            return result.Error switch
            {
                ErrorType.NotFound => new NotFoundObjectResult(ApiResponse<T>.Fail(result.Message ?? "Not found")),
                ErrorType.Validation => new BadRequestObjectResult(ApiResponse<T>.Fail(result.Message ?? "Validation error")),
                ErrorType.Conflict => new ConflictObjectResult(ApiResponse<T>.Fail(result.Message ?? "Conflict")),
                ErrorType.Forbidden => new ObjectResult(ApiResponse<T>.Fail(result.Message ?? "Forbidden")) { StatusCode = 403 },
                ErrorType.Unauthorized => new UnauthorizedObjectResult(ApiResponse<T>.Fail(result.Message ?? "Unauthorized")),
                ErrorType.None => new BadRequestObjectResult(ApiResponse<T>.Fail(result.Message ?? "Invalid service result")),
                _ => new ObjectResult(ApiResponse<T>.Fail(result.Message ?? "Unexpected error")) { StatusCode = 500 },
            };
        }
    }
}
