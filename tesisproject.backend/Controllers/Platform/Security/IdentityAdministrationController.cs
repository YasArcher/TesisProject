using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Implementations;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Wrappers;

namespace tesisproject.backend.Controllers.Platform.Security;

[ApiController]
[Route("api/admin/security")]
[Authorize(Policy = AppPolicies.SecurityAdministration)]
public class IdentityAdministrationController : ControllerBase
{
    private readonly IIdentityAdministrationService _identityAdministration;

    public IdentityAdministrationController(IIdentityAdministrationService identityAdministration)
    {
        _identityAdministration = identityAdministration;
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResult<IReadOnlyCollection<IdentityUserListItemDto>>>> GetUsers(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyCollection<IdentityUserListItemDto>>.Success(await _identityAdministration.GetUsersAsync(ct)));

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResult<IReadOnlyCollection<IdentityRoleListItemDto>>>> GetRoles(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyCollection<IdentityRoleListItemDto>>.Success(await _identityAdministration.GetRolesAsync(ct)));

    [HttpPost("users")]
    public async Task<ActionResult<ApiResult<IdentityUserListItemDto>>> CreateUser([FromBody] CreateIdentityUserRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.CreateUserAsync(request, ct);
            return Ok(ApiResult<IdentityUserListItemDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<IdentityUserListItemDto>.Fail(ex.Message));
        }
    }

    [HttpPost("roles")]
    public async Task<ActionResult<ApiResult<IdentityRoleListItemDto>>> CreateRole([FromBody] CreateIdentityRoleRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.CreateRoleAsync(request, ct);
            return Ok(ApiResult<IdentityRoleListItemDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<IdentityRoleListItemDto>.Fail(ex.Message));
        }
    }

    [HttpPut("users/{userId}/roles")]
    public async Task<ActionResult<ApiResult<IdentityUserListItemDto>>> UpdateUserRoles(string userId, [FromBody] UpdateIdentityUserRolesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.UpdateUserRolesAsync(userId, request, ct);
            return Ok(ApiResult<IdentityUserListItemDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<IdentityUserListItemDto>.Fail(ex.Message));
        }
    }
}
