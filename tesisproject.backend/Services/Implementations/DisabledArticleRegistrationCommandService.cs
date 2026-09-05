using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations;

public sealed class DisabledArticleRegistrationCommandService : IArticleRegistrationCommandService
{
    public Task<ServiceResult<RegisterArticleAggregateResponse>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default)
        => Task.FromResult(ServiceResult<RegisterArticleAggregateResponse>.Fail(
            "El modulo de articulos todavia no esta habilitado en este entorno.",
            ErrorType.Unexpected,
            "ARTICLES_MODULE_DISABLED"));
}
