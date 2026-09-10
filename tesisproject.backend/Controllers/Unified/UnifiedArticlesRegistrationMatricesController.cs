using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

[ApiController]
[Route("api/articles/registration-matrices")]
public sealed class UnifiedArticlesRegistrationMatricesController : ControllerBase
{
    private readonly IUnifiedRegistrationMatrixService _service;
    private readonly ArticlesModuleOptions _options;
    private readonly IUnifiedArticleUserContext _user;

    public UnifiedArticlesRegistrationMatricesController(IUnifiedRegistrationMatrixService service, IOptions<ArticlesModuleOptions> options, IUnifiedArticleUserContext user)
    {
        _service = service;
        _user = user;
        _options = options.Value;
    }

    [HttpGet]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<List<RegistrationMatrixSummaryDto>>>> GetMatrices([FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<List<RegistrationMatrixSummaryDto>>();
        return (await _service.GetMatricesAsync(take, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    [HttpGet("{matrixId:int}")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> GetMatrix(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.GetMatrixAsync(matrixId, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> CreateMatrix([FromBody] CreateRegistrationMatrixRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.CreateMatrixAsync(request, _user.OwnerReference, ct)).ToActionResult();
    }

    [HttpPost("{matrixId:int}/rows")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> AddRow(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.AddRowAsync(matrixId, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    [HttpPut("{matrixId:int}/rows/{rowId:int}/cells")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> UpdateCell(int matrixId, int rowId, [FromBody] UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.UpdateCellAsync(matrixId, rowId, request, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    [HttpDelete("{matrixId:int}")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDeleteResultDto>>> DeleteMatrix(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDeleteResultDto>();
        return (await _service.DeleteMatrixAsync(matrixId, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    [HttpPost("{matrixId:int}/submit")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixSubmissionResultDto>>> SubmitToStaging(int matrixId, [FromBody] SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixSubmissionResultDto>();
        return (await _service.SubmitToStagingAsync(matrixId, request, _user.OwnerReference, includeAll: true, ct)).ToActionResult();
    }

    private ActionResult<ServiceResult<T>> ModuleUnavailable<T>()
        => StatusCode(StatusCodes.Status503ServiceUnavailable, ServiceResult<T>.Fail("El modulo de articulos todavia no esta habilitado en este entorno.", ErrorType.Unexpected, "ARTICLES_MODULE_DISABLED"));
}
