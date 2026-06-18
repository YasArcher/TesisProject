using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Implementations;
using tesisproject.shared.DTOs.Auth;

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
    public async Task<ActionResult<IReadOnlyCollection<IdentityUserListItemDto>>> GetUsers(CancellationToken ct)
        => Ok(await _identityAdministration.GetUsersAsync(ct));

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyCollection<IdentityRoleListItemDto>>> GetRoles(CancellationToken ct)
        => Ok(await _identityAdministration.GetRolesAsync(ct));

    [HttpPost("users")]
    public async Task<ActionResult<IdentityUserListItemDto>> CreateUser([FromBody] CreateIdentityUserRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.CreateUserAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("roles")]
    public async Task<ActionResult<IdentityRoleListItemDto>> CreateRole([FromBody] CreateIdentityRoleRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.CreateRoleAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("users/{userId}/roles")]
    public async Task<ActionResult<IdentityUserListItemDto>> UpdateUserRoles(string userId, [FromBody] UpdateIdentityUserRolesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _identityAdministration.UpdateUserRolesAsync(userId, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
