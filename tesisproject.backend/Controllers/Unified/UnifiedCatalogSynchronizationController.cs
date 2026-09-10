using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Unified.Contracts;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/catalog-sync")]
public sealed class UnifiedCatalogSynchronizationController(
    IUnifiedFacultySynchronizationService faculties,
    IUnifiedAcademicTermSynchronizationService academicTerms) : ControllerBase
{
    [HttpPost("faculties")]
    public async Task<ActionResult<ServiceResult<CatalogSynchronizationResult>>> SynchronizeFaculties(CancellationToken ct)
        => (await faculties.SynchronizeAsync(ct)).ToActionResult();

    [HttpPost("academic-terms")]
    public async Task<ActionResult<ServiceResult<CatalogSynchronizationResult>>> SynchronizeAcademicTerms(CancellationToken ct)
        => (await academicTerms.SynchronizeAsync(ct)).ToActionResult();
}
