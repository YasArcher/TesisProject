using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers;

[Authorize(Roles = "superadmin")]
[ApiController]
[Route("api/etl/articles")]
public sealed class ArticlesDwEtlController(IArticlesDwEtlService etlService) : ControllerBase
{
    [HttpPost("full-load")]
    public async Task<ActionResult<ServiceResult<NoContent>>> RunFullLoad(CancellationToken ct)
        => (await etlService.RunFullLoadAsync(ct)).ToActionResult();
}
