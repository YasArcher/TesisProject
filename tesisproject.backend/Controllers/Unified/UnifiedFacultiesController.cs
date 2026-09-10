using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Faculty;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/faculties")]
public sealed class UnifiedFacultiesController(IUnifiedFacultyQueryService service) : ControllerBase
{
    [HttpGet("hierarchy")]
    public async Task<ActionResult<ServiceResult<List<FacultyHierarchyNodeDTO>>>> GetHierarchy(CancellationToken ct)
        => (await service.GetHierarchyAsync(ct)).ToActionResult();

    [HttpGet("roots")]
    public async Task<ActionResult<ServiceResult<List<FacultyHierarchyNodeDTO>>>> GetRoots(CancellationToken ct)
        => (await service.GetRootsAsync(ct)).ToActionResult();

    [HttpGet("{parentFacultyId:int}/children")]
    public async Task<ActionResult<ServiceResult<List<FacultyHierarchyNodeDTO>>>> GetChildren(int parentFacultyId, CancellationToken ct)
        => (await service.GetChildrenAsync(parentFacultyId, ct)).ToActionResult();
}
