using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers;

[ApiController]
[Route("api/articles/registration-matrices")]
public sealed class ArticlesRegistrationMatricesController : ControllerBase
{
    private readonly IRegistrationMatrixService _service;
    private readonly ArticlesModuleOptions _options;

    public ArticlesRegistrationMatricesController(IRegistrationMatrixService service, IOptions<ArticlesModuleOptions> options)
    {
        _service = service;
        _options = options.Value;
    }

    [HttpGet]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<List<RegistrationMatrixSummaryDto>>>> GetMatrices([FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<List<RegistrationMatrixSummaryDto>>();
        return (await _service.GetMatricesAsync(take, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    [HttpGet("{matrixId:int}")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> GetMatrix(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.GetMatrixAsync(matrixId, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> CreateMatrix([FromBody] CreateRegistrationMatrixRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.CreateMatrixAsync(request, CurrentUserReference(), ct)).ToActionResult();
    }

    [HttpPost("{matrixId:int}/rows")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> AddRow(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.AddRowAsync(matrixId, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    [HttpPut("{matrixId:int}/rows/{rowId:int}/cells")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDetailDto>>> UpdateCell(int matrixId, int rowId, [FromBody] UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDetailDto>();
        return (await _service.UpdateCellAsync(matrixId, rowId, request, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    [HttpDelete("{matrixId:int}")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixDeleteResultDto>>> DeleteMatrix(int matrixId, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixDeleteResultDto>();
        return (await _service.DeleteMatrixAsync(matrixId, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    [HttpPost("{matrixId:int}/submit")]
    [Authorize(Policy = ArticlePolicyNames.AuthorSubmission)]
    public async Task<ActionResult<ServiceResult<RegistrationMatrixSubmissionResultDto>>> SubmitToStaging(int matrixId, [FromBody] SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled) return ModuleUnavailable<RegistrationMatrixSubmissionResultDto>();
        return (await _service.SubmitToStagingAsync(matrixId, request, CurrentUserReference(), CanManageAll(), ct)).ToActionResult();
    }

    private bool CanManageAll()
        => User.IsInRole(AppRoles.Admin)
           || User.IsInRole(AppRoles.SuperAdmin)
           || User.IsInRole(ArticleRoles.Analyst)
           || User.HasClaim(ArticlePermissions.ClaimType, ArticlePermissions.ManageConfiguration);

    private string? CurrentUserReference()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;

    private ActionResult<ServiceResult<T>> ModuleUnavailable<T>()
        => StatusCode(StatusCodes.Status503ServiceUnavailable, ServiceResult<T>.Fail("El modulo de articulos todavia no esta habilitado en este entorno.", ErrorType.Unexpected, "ARTICLES_MODULE_DISABLED"));
}
