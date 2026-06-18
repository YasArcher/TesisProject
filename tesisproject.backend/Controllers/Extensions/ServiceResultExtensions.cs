using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Extensions
{
    public static class ServiceResultExtensions
    {
        public static ActionResult<ServiceResult<T>> ToActionResult<T>(this ServiceResult<T> result)
        {
            if (result.Success)
                return new OkObjectResult(result);

            return result.Error switch
            {
                ErrorType.NotFound => new NotFoundObjectResult(result),
                ErrorType.Validation => new BadRequestObjectResult(result),
                ErrorType.Conflict => new ConflictObjectResult(result),
                ErrorType.Forbidden => new ObjectResult(result)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },
                ErrorType.Unauthorized => new UnauthorizedObjectResult(result),
                ErrorType.None => new BadRequestObjectResult(result),
                _ => new ObjectResult(result)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };
        }
    }
}