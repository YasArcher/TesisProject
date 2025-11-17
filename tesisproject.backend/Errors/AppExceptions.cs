using System.Net;

namespace tesisproject.backend.Errors;

public abstract class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    protected AppException(string message, HttpStatusCode statusCode) : base(message) => StatusCode = statusCode;
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, HttpStatusCode.NotFound) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message, HttpStatusCode.Conflict) { }
}
